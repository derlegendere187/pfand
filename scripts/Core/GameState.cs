using System;
using System.Collections.Generic;
using System.Globalization;

namespace Pfand.Core;

public enum BottleType { Bier, Wasser, Dose, Plastik, Retro, Gold }

public enum DayPhase { Tag, Abend, Nacht }

public static class BottleInfo
{
    /// <summary>Grundwert ohne Skill-Bonus.</summary>
    public static double BaseValue(BottleType t) => t switch
    {
        BottleType.Bier => 0.08,
        BottleType.Wasser => 0.15,
        BottleType.Retro => 0.75,
        BottleType.Gold => 5.00,
        _ => 0.25,
    };

    public static string Name(BottleType t) => t switch
    {
        BottleType.Bier => "Bierflasche",
        BottleType.Wasser => "Glasflasche",
        BottleType.Dose => "Dose",
        BottleType.Retro => "Retro-Flasche",
        BottleType.Gold => "Goldflasche",
        _ => "Plastikflasche",
    };

    public static bool HasCap(BottleType t) => t != BottleType.Dose;
}

/// <summary>
/// Der komplette Spielstand. Die einzige Wahrheit – die UI liest nur von hier
/// und hört auf <see cref="Changed"/>.
/// </summary>
public class GameState
{
    public static readonly GameState I = new();

    public double Money;
    public double TotalEarned;
    public long BottlesFed;
    public int PeakVisitors;       // Rekord: so viele Leute waren gleichzeitig im Park
    public int Kronkorken;         // Währung für den Skilltree
    public double TimeOfDay = Balance.StartTimeOfDay;
    public List<BottleType> Bag = new();
    public Dictionary<string, int> Upgrades = new();
    public Dictionary<int, int> Caps = new();          // Album: Motiv -> wie oft gefunden
    public HashSet<string> Skills = new();
    public Dictionary<string, double> Cooldowns = new(); // Event -> Restzeit in Sekunden
    public string ActiveEvent;                          // null = kein Event
    public double EventTimeLeft;

    public event Action Changed;

    public void Notify() => Changed?.Invoke();

    public int Level(string id) => Upgrades.TryGetValue(id, out var l) ? l : 0;
    public bool HasSkill(string id) => Skills.Contains(id);

    public int BagCapacity => Balance.BagBase + Level("tasche") * Balance.BagPerLevel;
    public bool BagFull => Bag.Count >= BagCapacity;

    public DayPhase Phase =>
        TimeOfDay < Balance.EveningStart ? DayPhase.Tag :
        TimeOfDay < Balance.NightStart ? DayPhase.Abend : DayPhase.Nacht;

    // --- Skill-Wirkungen ---
    public double ValueMultiplier =>
        1 + (HasSkill("wert1") ? 0.25 : 0) + (HasSkill("wert2") ? 0.25 : 0) + (HasSkill("wert3") ? 0.5 : 0);

    public double CapChance => Balance.CapDropChance * (HasSkill("glueck") ? 2 : 1);
    public double CooldownFactor => HasSkill("ev_cooldown") ? 0.7 : 1.0;

    public double Value(BottleType t) => BottleInfo.BaseValue(t) * ValueMultiplier;

    public bool TryAddToBag(BottleType t)
    {
        if (BagFull) return false;
        Bag.Add(t);
        return true;
    }

    /// <summary>Wirft die erste Flasche aus der Tasche ein. cap = -1, wenn kein Kronkorken gefunden wurde.</summary>
    public (BottleType type, double value, int cap) FeedOne(Random rng)
    {
        var t = Bag[0];
        Bag.RemoveAt(0);
        var (v, cap) = FeedDirect(t, rng);
        return (t, v, cap);
    }

    /// <summary>Eine Flasche direkt einwerfen (z. B. durch Sammler).</summary>
    public (double value, int cap) FeedDirect(BottleType t, Random rng)
    {
        var v = Value(t);
        Money += v;
        TotalEarned += v;
        BottlesFed++;
        int cap = -1;
        double chance = CapChance * (t == BottleType.Retro || t == BottleType.Gold ? 5 : 1);
        if (BottleInfo.HasCap(t) && rng.NextDouble() < chance)
        {
            cap = rng.Next(CapData.Count);
            Caps[cap] = (Caps.TryGetValue(cap, out var c) ? c : 0) + 1;
            Kronkorken++;
        }
        return (v, cap);
    }

    // --- Upgrades ---
    public bool CanBuy(UpgradeDef d)
    {
        int l = Level(d.Id);
        return d.Unlocked(this) && l < d.Max && Money + 1e-9 >= d.Price(l);
    }

    public bool TryBuy(UpgradeDef d)
    {
        if (!CanBuy(d)) return false;
        int l = Level(d.Id);
        Money -= d.Price(l);
        Upgrades[d.Id] = l + 1;
        Notify();
        return true;
    }

    // --- Skills ---
    public bool SkillAvailable(SkillDef s) => !HasSkill(s.Id) && (s.Requires == null || HasSkill(s.Requires));
    public bool CanBuySkill(SkillDef s) => SkillAvailable(s) && Kronkorken >= s.Cost;

    public bool TryBuySkill(SkillDef s)
    {
        if (!CanBuySkill(s)) return false;
        Kronkorken -= s.Cost;
        Skills.Add(s.Id);
        Notify();
        return true;
    }

    // --- Events ---
    public double CooldownLeft(EventDef e) => Cooldowns.TryGetValue(e.Id, out var c) ? Math.Max(0, c) : 0;
    public bool EventUnlocked(EventDef e) => HasSkill(e.Skill);
    public bool CanStartEvent(EventDef e) => EventUnlocked(e) && ActiveEvent == null && CooldownLeft(e) <= 0;

    public bool TryStartEvent(EventDef e)
    {
        if (!CanStartEvent(e)) return false;
        ActiveEvent = e.Id;
        EventTimeLeft = e.Duration;
        Cooldowns[e.Id] = e.Duration + e.Cooldown * CooldownFactor;
        Notify();
        return true;
    }

    public void UpdateEvents(double dt)
    {
        var keys = new List<string>(Cooldowns.Keys);
        foreach (var k in keys) Cooldowns[k] = Math.Max(0, Cooldowns[k] - dt);
        if (ActiveEvent != null)
        {
            EventTimeLeft -= dt;
            if (EventTimeLeft <= 0) { ActiveEvent = null; EventTimeLeft = 0; Notify(); }
        }
    }

    public string ClockText()
    {
        // Tag 08:00–19:00, Abend 19:00–22:00, Nacht 22:00–08:00
        double h;
        if (TimeOfDay < Balance.EveningStart) h = 8 + TimeOfDay / Balance.EveningStart * 11;
        else if (TimeOfDay < Balance.NightStart) h = 19 + (TimeOfDay - Balance.EveningStart) / (Balance.NightStart - Balance.EveningStart) * 3;
        else h = 22 + (TimeOfDay - Balance.NightStart) / (1 - Balance.NightStart) * 10;
        h %= 24;
        int hh = (int)h, mm = (int)((h - hh) * 60) / 10 * 10;
        return $"{hh:00}:{mm:00}";
    }

    /// <summary>Deutsches Geldformat: € 1.284,50</summary>
    public static string Euro(double v)
    {
        var s = v.ToString("N2", CultureInfo.InvariantCulture); // 1,284.50
        s = s.Replace(",", "#").Replace(".", ",").Replace("#", ".");
        return "€ " + s;
    }

    public static string Seconds(double s)
    {
        int t = (int)Math.Ceiling(s);
        return t >= 60 ? $"{t / 60}:{t % 60:00}" : $"{t}s";
    }
}
