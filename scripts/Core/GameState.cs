using System;
using System.Collections.Generic;
using System.Globalization;

namespace Pfand.Core;

public enum BottleType { Bier, Wasser, Dose, Plastik, Retro, Gold }

public enum DayPhase { Tag, Abend, Nacht }

/// <summary>Schritte der Einführung beim allerersten Spielen.</summary>
public enum TutorialStep { Intro, WatchGuest, ClickBottle, Feed, Outro, Done }

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
    public Dictionary<string, int> Skills = new();     // Skill -> Stufe
    public Dictionary<string, double> Cooldowns = new(); // Event -> Restzeit in Sekunden
    public Dictionary<string, int> EventCount = new();  // Event -> wie oft schon veranstaltet
    public string ActiveEvent;                          // null = kein Event
    public double EventTimeLeft;
    public TutorialStep Tutorial = TutorialStep.Done;   // neue Spiele beginnen mit Intro
    public bool TutorialActive => Tutorial != TutorialStep.Done;
    public HashSet<string> SeenHints = new();           // Kalles Tipps, die schon kamen
    public bool LaterneHint => SeenHints.Contains("laterne");
    public bool ClockPaused;                            // Kalle redet gerade: die Uhr steht

    // Statistik
    public double PlayTime;                             // Sekunden
    public Dictionary<int, long> FedByType = new();     // BottleType -> Anzahl eingeworfen
    public long CollectedByHand;
    public long CollectedByCollectors;
    public int EventsHeld;
    public int KaestenAbgegeben;
    public double BestBon;

    public event Action Changed;
    /// <summary>Ein Kronkorken wurde gefunden (Motiv, neu fürs Album?).</summary>
    public event Action<int, bool> CapFound;
    public void DebugCapFound(int cap) => CapFound?.Invoke(cap, true);
    public bool CrateLeftBehind;                        // eine Gruppe hat ihren Bierkasten stehen lassen (für Kalle)

    public void Notify() => Changed?.Invoke();

    public int Level(string id) => Upgrades.TryGetValue(id, out var l) ? l : 0;
    public int SkillLevel(string id) => id == SkillDefs.Root ? 1 : Skills.TryGetValue(id, out var l) ? l : 0;
    public bool HasSkill(string id) => SkillLevel(id) > 0;

    public int BagCapacity => Balance.BagBase + Level("tasche") * Balance.BagPerLevel;
    public int MaxLevel(UpgradeDef d) => d.Max + (d.MaxBonus?.Invoke(this) ?? 0);
    public bool BagFull => Bag.Count >= BagCapacity;

    public DayPhase Phase =>
        TimeOfDay < Balance.EveningStart ? DayPhase.Tag :
        TimeOfDay < Balance.NightStart ? DayPhase.Abend : DayPhase.Nacht;

    // --- Skill-Wirkungen ---
    public double ValueMultiplier => (1 + 0.2 * SkillLevel("wert")) * (HasSkill("wert_koenig") ? 2 : 1);

    public double TypeMultiplier(BottleType t) => t switch
    {
        BottleType.Bier or BottleType.Wasser or BottleType.Retro => 1 + 0.5 * SkillLevel("wert_glas"),
        BottleType.Dose or BottleType.Plastik => 1 + 0.25 * SkillLevel("wert_dose"),
        _ => 1,
    };

    /// <summary>Album und Skilltree tauchen auf, sobald man den ersten Kronkorken hat.</summary>
    public bool SkillsVisible => Kronkorken > 0 || Caps.Count > 0 || Skills.Count > 0;
    public double CapChance => Balance.CapDropChance * (1 + 0.5 * SkillLevel("glueck"));
    public double CooldownFactor => 1 - 0.15 * SkillLevel("ev_cooldown");
    public double FeedInterval => Balance.FeedInterval / (1 + 0.5 * SkillLevel("abgabe"));
    public double RetroChance => Balance.RetroChancePerLevel * SkillLevel("retro");
    public double GoldChance => Balance.GoldChancePerLevel * SkillLevel("gold");
    /// <summary>Anteil der leeren Flaschen, die Besucher liegen lassen (der Rest wird mitgenommen).</summary>
    public double LeaveChance => Math.Min(1, Balance.LeaveChanceBase + Balance.LeaveChancePerLevel * SkillLevel("liegen"));
    public double DrinkFactor => 1 - 0.1 * SkillLevel("durst");
    public double VisitorBoost => 0.15 * SkillLevel("stammgaeste");
    public int ExtraVisitorSlots => 2 * SkillLevel("stammgaeste");
    public double GroupCrateChance => Level("gruppen") > 0
        ? Balance.GroupCrateChanceBase + Balance.GroupCrateChancePerLevel * SkillLevel("kastenrunde") : 0;
    public float ClickRadius => Balance.ClickRadiusBase + Balance.ClickRadiusPerLevel * SkillLevel("aoe");
    public double CollectorSpeed => Balance.CollectorSpeed * (1 + 0.25 * SkillLevel("flink"));
    public int CollectorCarry => Balance.CollectorCarry + 2 * SkillLevel("flink");

    public double Value(BottleType t) => BottleInfo.BaseValue(t) * ValueMultiplier * TypeMultiplier(t);

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
        FedByType[(int)t] = (FedByType.TryGetValue((int)t, out var n) ? n : 0) + 1;
        int cap = -1;
        double chance = CapChance * (t == BottleType.Retro || t == BottleType.Gold ? 5 : 1);
        // Der erste Kronkorken kommt spätestens mit der zwölften Flasche, damit man den Skilltree früh entdeckt
        bool firstGuaranteed = Caps.Count == 0 && Kronkorken == 0 && Skills.Count == 0 && BottlesFed >= Balance.FirstCapGuaranteedAt;
        if (BottleInfo.HasCap(t) && (firstGuaranteed || rng.NextDouble() < chance))
        {
            cap = rng.Next(CapData.Count);
            // Albumjäger: oft ein Motiv, das noch fehlt
            if (HasSkill("albumjaeger") && Caps.ContainsKey(cap) && rng.NextDouble() < 0.6)
            {
                var missing = new List<int>();
                for (int i = 0; i < CapData.Count; i++) if (!Caps.ContainsKey(i)) missing.Add(i);
                if (missing.Count > 0) cap = missing[rng.Next(missing.Count)];
            }
            Caps[cap] = (Caps.TryGetValue(cap, out var c) ? c : 0) + 1;
            Kronkorken++;
            CapFound?.Invoke(cap, Caps[cap] == 1);
        }
        return (v, cap);
    }

    // --- Upgrades ---
    public bool CanBuy(UpgradeDef d)
    {
        int l = Level(d.Id);
        return UpgradeUnlocked(d) && l < MaxLevel(d) && Money + 1e-9 >= d.Price(l);
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

    /// <summary>Im Skilltree freigeschaltet und die Zusatzbedingung erfüllt (z. B. Grill erst mit Imbiss).</summary>
    public bool UpgradeUnlocked(UpgradeDef d) => (d.Skill == null || HasSkill(d.Skill)) && d.Unlocked(this);

    // --- Ausbau: gilt über den Skilltree für alle Objekte einer Sorte ---
    public int AusbauLevel(string kind, int index = 0) => kind switch
    {
        "bank" => HasSkill("bank_doppel") ? 2 : HasSkill("bank_tisch") ? 1 : 0,
        "muelleimer" => HasSkill("eimer_ring") ? 2 : HasSkill("eimer_gross") ? 1 : 0,
        "imbiss" => HasSkill("imbiss_kuehl") ? 2 : HasSkill("imbiss_theke") ? 1 : 0,
        "tischtennis" => HasSkill("tt_turnier") ? 1 : 0,
        "grill" => HasSkill("grill_kuehl") ? 2 : HasSkill("grill_rost") ? 1 : 0,
        _ => 0,
    };

    public int BinCapacity(int bin) => AusbauLevel("muelleimer", bin) switch { 0 => Balance.BinCapacity, 1 => 25, _ => 40 };
    public int BenchSeats(int bench) => AusbauLevel("bank", bench) >= 2 ? 4 : 2;

    // --- Skills ---
    /// <summary>Lernbar, sobald der Elternknoten gelernt ist.</summary>
    public bool SkillAvailable(SkillDef s) => s.Id != SkillDefs.Root && HasSkill(s.Parent ?? SkillDefs.Root);
    public bool SkillMaxed(SkillDef s) => SkillLevel(s.Id) >= s.MaxLevel;
    public int NextSkillCost(SkillDef s) => SkillMaxed(s) ? 0 : SkillDefs.Cost(s, SkillLevel(s.Id));
    public bool CanBuySkill(SkillDef s) => SkillAvailable(s) && !SkillMaxed(s) && Kronkorken >= NextSkillCost(s);

    public bool TryBuySkill(SkillDef s)
    {
        if (!CanBuySkill(s)) return false;
        Kronkorken -= NextSkillCost(s);
        Skills[s.Id] = SkillLevel(s.Id) + 1;
        Notify();
        return true;
    }

    // --- Events ---
    public double CooldownLeft(EventDef e) => Cooldowns.TryGetValue(e.Id, out var c) ? Math.Max(0, c) : 0;
    public bool EventUnlocked(EventDef e) => HasSkill(e.Skill);
    public bool AnyEventUnlocked => EventDefs.All.Exists(EventUnlocked);
    public int TimesHeld(EventDef e) => EventCount.TryGetValue(e.Id, out var n) ? n : 0;
    /// <summary>
    /// Events kosten Geld. Der Preis wächst mit dem Pfandwert und mit jedem Mal, das man es schon veranstaltet hat.
    /// Sponsoren machen es billiger.
    /// </summary>
    public double EventCost(EventDef e) => Math.Round(e.Cost * ValueMultiplier * (1 + Balance.EventCostGrowth * TimesHeld(e))
        * (1 - 0.15 * SkillLevel("ev_sponsor")), 2);
    public bool CanStartEvent(EventDef e) => EventUnlocked(e) && ActiveEvent == null && CooldownLeft(e) <= 0 && Money + 1e-9 >= EventCost(e);

    public bool TryStartEvent(EventDef e, bool free = false)
    {
        if (!free && !CanStartEvent(e)) return false;
        if (!free) Money -= EventCost(e);
        ActiveEvent = e.Id;
        EventTimeLeft = e.Duration;
        EventsHeld++;
        EventCount[e.Id] = TimesHeld(e) + 1;
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
