using System;
using System.Collections.Generic;
using System.Globalization;

namespace Pfand.Core;

public enum BottleType { Bier, Wasser, Dose, Plastik }

public enum DayPhase { Tag, Abend, Nacht }

public static class BottleInfo
{
    public static double Value(BottleType t) => t switch
    {
        BottleType.Bier => 0.08,
        BottleType.Wasser => 0.15,
        _ => 0.25,
    };

    public static string Name(BottleType t) => t switch
    {
        BottleType.Bier => "Bierflasche",
        BottleType.Wasser => "Glasflasche",
        BottleType.Dose => "Dose",
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
    public double TimeOfDay = Balance.StartTimeOfDay;
    public List<BottleType> Bag = new();
    public Dictionary<string, int> Upgrades = new();
    public Dictionary<int, int> Caps = new();

    public event Action Changed;

    public void Notify() => Changed?.Invoke();

    public int Level(string id) => Upgrades.TryGetValue(id, out var l) ? l : 0;

    public int BagCapacity => Balance.BagBase + Level("tasche") * Balance.BagPerLevel;
    public bool BagFull => Bag.Count >= BagCapacity;

    public DayPhase Phase =>
        TimeOfDay < Balance.EveningStart ? DayPhase.Tag :
        TimeOfDay < Balance.NightStart ? DayPhase.Abend : DayPhase.Nacht;

    public bool TryAddToBag(BottleType t)
    {
        if (BagFull) return false;
        Bag.Add(t);
        return true;
    }

    /// <summary>Wirft die erste Flasche aus der Tasche ein. cap = -1, wenn kein Deckel gefunden wurde.</summary>
    public (BottleType type, double value, int cap) FeedOne(Random rng)
    {
        var t = Bag[0];
        Bag.RemoveAt(0);
        var v = BottleInfo.Value(t);
        Money += v;
        TotalEarned += v;
        BottlesFed++;
        int cap = -1;
        if (BottleInfo.HasCap(t) && rng.NextDouble() < Balance.CapDropChance)
        {
            cap = rng.Next(CapData.Count);
            Caps[cap] = (Caps.TryGetValue(cap, out var c) ? c : 0) + 1;
        }
        return (t, v, cap);
    }

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
}
