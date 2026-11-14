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
    public double EventPauseLeft;                       // gemeinsame Pause aller großen Events
    public Dictionary<string, double> ActiveBoosts = new(); // laufende Aktionen -> Restzeit
    public Dictionary<string, int> EventCount = new();  // Event -> wie oft schon veranstaltet
    public string ActiveEvent;                          // null = kein Event
    public double EventTimeLeft;
    public TutorialStep Tutorial = TutorialStep.Done;   // neue Spiele beginnen mit Intro
    public bool TutorialActive => Tutorial != TutorialStep.Done;
    public HashSet<string> SeenHints = new();           // Kalles Tipps, die schon kamen
    public bool LaterneHint => SeenHints.Contains("laterne");
    public bool ClockPaused;                            // Kalle redet gerade: die Uhr steht
    public bool Feeding;                                // der Automat zählt: solange kommt nichts Neues in die Tasche
    public bool FlyerVisible;                           // gerade liegt ein Aktions-Flyer im Park (für Kalle)
    public bool BottleTaken;                            // jemand anderes hat schon mal eine Flasche mitgenommen (für Kalle)
    public Godot.Vector2 TakenAt;                             // wo das zum ersten Mal passiert ist (Kalle zeigt hin)
    public bool KomboSeen;                              // erster Bon mit Kombo-Bonus (für Kalle)
    public bool CapIntroDone;                           // der erste Kronkorken wurde groß gezeigt, jetzt darf Kalle erklären

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
    public double ValueMultiplier => (1 + 0.05 * SkillLevel("wert")) * (HasSkill("wert_koenig") ? 2 : 1);

    public double TypeMultiplier(BottleType t) => t switch
    {
        BottleType.Bier => 1 + 0.1 * SkillLevel("bier_wert"),
        BottleType.Wasser => 1 + 0.1 * SkillLevel("glas_wert"),
        BottleType.Plastik => 1 + 0.1 * SkillLevel("plastik_wert"),
        BottleType.Dose => 1 + 0.1 * SkillLevel("dose_wert"),
        _ => 1,
    };

    /// <summary>Am Anfang gibt es nur Bier. Die anderen Sorten schaltet der Skilltree frei.</summary>
    public bool TypeUnlocked(BottleType t) => t switch
    {
        BottleType.Wasser => HasSkill("s_glas"),
        BottleType.Plastik => HasSkill("s_plastik"),
        BottleType.Dose => HasSkill("s_dose"),
        BottleType.Retro => HasSkill("retro"),
        BottleType.Gold => HasSkill("gold"),
        _ => true,
    };

    /// <summary>Wie oft eine Sorte beim Auswürfeln drankommt (0 = noch nicht freigeschaltet).</summary>
    public double SpawnWeight(BottleType t)
    {
        if (!TypeUnlocked(t)) return 0;
        return t switch
        {
            BottleType.Bier => Balance.WeightBier * (1 + Balance.WeightBierPerLevel * SkillLevel("bier_oft")),
            BottleType.Wasser => Balance.WeightGlas * (1 + Balance.WeightOtherPerLevel * SkillLevel("glas_oft")),
            BottleType.Plastik => Balance.WeightPlastik * (1 + Balance.WeightOtherPerLevel * SkillLevel("plastik_oft")),
            BottleType.Dose => Balance.WeightDose * (1 + Balance.WeightOtherPerLevel * SkillLevel("dose_oft")),
            _ => 0,
        };
    }

    /// <summary>Ein Event will eine Sorte, die es noch nicht gibt: dann eben Bier.</summary>
    public BottleType OrBier(BottleType t) => TypeUnlocked(t) ? t : BottleType.Bier;

    /// <summary>Album und Skilltree tauchen auf, sobald man den ersten Kronkorken hat.</summary>
    public bool SkillsVisible => Kronkorken > 0 || Caps.Count > 0 || Skills.Count > 0;
    public double CapChance => Math.Min(1, (Balance.CapDropChance + Balance.CapChancePerLevel * SkillLevel("glueck")) * (BoostActive("gluecksstraehne") ? 4 : 1));
    public double MultiCapChance => Balance.MultiCapPerLevel * SkillLevel("mehrfach");
    /// <summary>Sammler geben erst mit "Ehrliche Sammler" Kronkorken ab.</summary>
    public double CollectorCapFactor => 0.1 * SkillLevel("ehrlich");

    /// <summary>Laufendes Event und Pfandaktion machen eingeworfene Flaschen mehr wert.</summary>
    public double EventValueBonus
    {
        get
        {
            double b = ActiveEvent != null && EventDefs.Get(ActiveEvent) is { } e ? EventBonus(e) : 1;
            foreach (var id in ActiveBoosts.Keys) b *= EventDefs.Get(id)?.ValueBonus ?? 1;
            return b;
        }
    }
    public bool BoostActive(string id) => ActiveBoosts.TryGetValue(id, out var t) && t > 0;
    public double FeedInterval => Balance.FeedInterval / (1 + 0.5 * SkillLevel("abgabe"));
    public double RetroChance => Balance.RetroChancePerLevel * SkillLevel("retro");
    public double GoldChance => Balance.GoldChancePerLevel * SkillLevel("gold");
    /// <summary>Anteil der leeren Flaschen, die Besucher liegen lassen (der Rest wird mitgenommen).</summary>
    public double LeaveChance => Math.Min(1, Balance.LeaveChanceBase + Balance.LeaveChancePerLevel * SkillLevel("liegen"));
    public double DrinkFactor => (1 - 0.03 * SkillLevel("durst"))
        * (BoostActive("happyhour") ? 0.4 : 1) * (ActiveEvent == "oktoberfest" ? 0.7 : 1);
    public double VisitorBoost => 0.05 * SkillLevel("stammgaeste");
    public int ExtraVisitorSlots => SkillLevel("platz");
    public double GroupCrateChance => HasSkill("u_gruppen")
        ? Balance.GroupCrateChanceBase + Balance.GroupCrateChancePerLevel * SkillLevel("kastenrunde") : 0;
    public float ClickRadius => Balance.ClickRadiusBase * (1 + Balance.ClickRadiusPerLevel * SkillLevel("aoe"));
    public double CollectorSpeed => Balance.CollectorSpeed * (1 + 0.08 * SkillLevel("flink"));
    public int CollectorCarry => Balance.CollectorCarry + SkillLevel("tragkraft");
    public double BottleLifetime => Balance.BottleLifetime + Balance.BottleLifetimePerLevel * SkillLevel("revier");
    public double KomboMax => Balance.KomboMax + Balance.KomboMaxPerLevel * SkillLevel("kombo");
    /// <summary>Kombo-Bonus für einen Einwurf mit n Flaschen (0.12 = +12 %).</summary>
    public double Kombo(int n) => HasSkill("kombo_an") ? Math.Min(KomboMax, Math.Max(0, n - Balance.KomboStart + 1) * Balance.KomboPerBottle) : 0;
    /// <summary>Wie oft pro Sekunde ein Aktions-Flyer auftaucht.</summary>
    public double FlyerRate => (1 + 0.1 * SkillLevel("flyer")) / Balance.FlyerMeanInterval;

    // Ausbau der Park-Objekte
    public double BankStayFactor => 1 + 0.15 * SkillLevel("bank_tisch");
    public double BankDrinkFactor => 1 - 0.06 * SkillLevel("tischrunde");
    public double TableStayFactor => 1 + 0.15 * SkillLevel("tt_turnier");
    public double TableDrinkBonus => 1 - 0.08 * SkillLevel("tt_sieger");
    public int ImbissExtraSlots => SkillLevel("imbiss_theke");
    public double ImbissDrinkBonus => 1 - 0.05 * SkillLevel("imbiss_kuehl");
    public double ImbissVisitBonus => 0.08 * SkillLevel("imbiss_schild");
    public double GrillStayBonus => 1 + 0.15 * SkillLevel("grill_rost");
    public double GrillDrinkBonus => 1 - 0.08 * SkillLevel("grill_kuehl");
    public double BinRingChance => Balance.BinRingChancePerLevel * SkillLevel("eimer_ring");

    public double Value(BottleType t) => BottleInfo.BaseValue(t) * ValueMultiplier * TypeMultiplier(t);

    /// <summary>Solange der Automat zählt, passt nichts in die Tasche – außer mit "Zweite Hand".</summary>
    public bool CanCollectWhileFeeding => !Feeding || HasSkill("weitersammeln");

    public bool TryAddToBag(BottleType t)
    {
        if (BagFull || !CanCollectWhileFeeding) return false;
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

    /// <summary>Eine Flasche direkt einwerfen (auch durch Sammler und Lastenrad).</summary>
    public (double value, int cap) FeedDirect(BottleType t, Random rng, bool byCollector = false)
    {
        var v = Value(t) * EventValueBonus * (byCollector ? 1 + Balance.CollectorValuePerLevel * SkillLevel("sammler_lohn") : 1);
        Money += v;
        TotalEarned += v;
        BottlesFed++;
        FedByType[(int)t] = (FedByType.TryGetValue((int)t, out var n) ? n : 0) + 1;
        int cap = -1;
        double chance = CapChance * (t == BottleType.Retro || t == BottleType.Gold ? 5 : 1) * (byCollector ? CollectorCapFactor : 1);
        // Der erste Kronkorken kommt spätestens mit der zwölften Flasche, damit man den Skilltree früh entdeckt
        bool firstGuaranteed = !byCollector && Caps.Count == 0 && Kronkorken == 0 && Skills.Count == 0 && BottlesFed >= Balance.FirstCapGuaranteedAt;
        if (BottleInfo.HasCap(t) && (firstGuaranteed || rng.NextDouble() < chance))
        {
            cap = RollCap(rng);
            Caps[cap] = (Caps.TryGetValue(cap, out var c) ? c : 0) + 1;
            Kronkorken++;
            CapFound?.Invoke(cap, Caps[cap] == 1);
            // Doppelfund: mit etwas Glück steckt gleich noch einer drin, und noch einer …
            for (int extra = 0; extra < 9 && rng.NextDouble() < MultiCapChance; extra++)
            {
                Caps[cap]++;
                Kronkorken++;
                CapFound?.Invoke(cap, false);
            }
        }
        return (v, cap);
    }

    /// <summary>Welches Motiv? Seltene Motive und spätere Sets kommen seltener (dafür gibt es keine Skills).</summary>
    public int RollCap(Random rng)
    {
        double total = 0;
        for (int i = 0; i < CapData.Count; i++) total += CapWeight(i);
        double r = rng.NextDouble() * total;
        for (int i = 0; i < CapData.Count; i++)
        {
            r -= CapWeight(i);
            if (r <= 0) return i;
        }
        return CapData.Count - 1;
    }

    static double CapWeight(int i) => CapData.SetWeight(CapData.All[i].Set) * Balance.CapTierWeight[CapData.Tier(i)];

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
    /// <summary>Wie ein Objekt aussieht (Tisch an der Bank, Pfandtonne am Eimer …). Die Wirkung steht oben bei den Skill-Wirkungen.</summary>
    public int AusbauLevel(string kind, int index = 0) => kind switch
    {
        "bank" => HasSkill("bank_doppel") ? 2 : HasSkill("bank_tisch") ? 1 : 0,
        "muelleimer" => HasSkill("eimer_ring") ? 2 : HasSkill("eimer_gross") ? 1 : 0,
        _ => 0,
    };

    public int BinCapacity(int bin) => Balance.BinCapacity + Balance.BinCapacityPerLevel * SkillLevel("eimer_gross");
    public int BenchSeats(int bench) => AusbauLevel("bank", bench) >= 2 ? 4 : 2;

    // --- Skills ---
    /// <summary>Lernbar, sobald der Elternknoten gelernt ist.</summary>
    public bool SkillAvailable(SkillDef s) => s.Id != SkillDefs.Root && HasSkill(s.Parent ?? SkillDefs.Root) && GateOpen(s);

    /// <summary>Äste an der Wurzel öffnen sich nach und nach, je mehr Kronkorken man im Skilltree ausgegeben hat.</summary>
    public bool GateOpen(SkillDef s) => SpentOnSkills >= SkillDefs.GateOf(s);

    /// <summary>So viele Kronkorken stecken insgesamt in den gelernten Skills.</summary>
    public int SpentOnSkills
    {
        get
        {
            int sum = 0;
            foreach (var kv in Skills)
                if (SkillDefs.Get(kv.Key) is { } d)
                    for (int l = 0; l < kv.Value && l < d.MaxLevel; l++) sum += SkillDefs.Cost(d, l);
            return sum;
        }
    }

    /// <summary>Der nächste noch geschlossene Ast an der Wurzel (null = alle offen).</summary>
    public SkillDef NextGatedBranch
    {
        get
        {
            int spent = SpentOnSkills;
            SkillDef next = null;
            foreach (var d in SkillDefs.All)
            {
                int g = SkillDefs.GateOf(d);
                if (d.Parent == null && d.Id != SkillDefs.Root && g > spent && (next == null || g < SkillDefs.GateOf(next))) next = d;
            }
            return next;
        }
    }
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
    public bool EventUnlocked(EventDef e) => HasSkill(e.Skill);
    public bool AnyEventUnlocked => EventDefs.All.Exists(e => !e.Short && EventUnlocked(e));
    public int TimesHeld(EventDef e) => EventCount.TryGetValue(e.Id, out var n) ? n : 0;
    /// <summary>Ausbau des Events im Skilltree (EVENT+).</summary>
    public int EventPlus(EventDef e) => SkillLevel("evp_" + e.Id);
    public double EventDuration(EventDef e) => e.Duration * (1 + 0.1 * EventPlus(e));
    public double EventBonus(EventDef e) => e.ValueBonus + 0.05 * EventPlus(e);
    public double EventPauseLength => Balance.EventPause * (1 - Balance.EventPauseCutPerLevel * SkillLevel("ev_cooldown"));
    /// <summary>
    /// Events kosten Kronkorken. Der Preis steigt, je öfter man es veranstaltet (höchstens bis zum doppelten Grundpreis).
    /// Sponsoren machen es billiger.
    /// </summary>
    public int EventCost(EventDef e) => (int)Math.Ceiling(e.Cost * (1 + Balance.EventCostGrowth * Math.Min(TimesHeld(e), Balance.EventCostGrowthMax))
        * (1 - 0.05 * SkillLevel("ev_sponsor")));
    public bool EventRunning(EventDef e) => e.Short ? BoostActive(e.Id) : ActiveEvent == e.Id;
    public bool CanStartEvent(EventDef e) => !e.Short && EventUnlocked(e) && ActiveEvent == null
        && EventPauseLeft <= 0 && Kronkorken >= EventCost(e);

    /// <summary>Großes Event starten (kostet Kronkorken) oder Aktion starten (gratis, kommt per Flyer).</summary>
    public bool TryStartEvent(EventDef e, bool free = false)
    {
        if (e.Short)
        {
            ActiveBoosts[e.Id] = e.Duration;
            Notify();
            return true;
        }
        if (!free && !CanStartEvent(e)) return false;
        if (!free) Kronkorken -= EventCost(e);
        ActiveEvent = e.Id;
        EventTimeLeft = EventDuration(e);
        EventsHeld++;
        EventCount[e.Id] = TimesHeld(e) + 1;
        Notify();
        return true;
    }

    public void UpdateEvents(double dt)
    {
        if (EventPauseLeft > 0) EventPauseLeft = Math.Max(0, EventPauseLeft - dt);
        if (ActiveBoosts.Count > 0)
        {
            foreach (var k in new List<string>(ActiveBoosts.Keys))
            {
                ActiveBoosts[k] -= dt;
                if (ActiveBoosts[k] <= 0) { ActiveBoosts.Remove(k); Notify(); }
            }
        }
        if (ActiveEvent != null)
        {
            EventTimeLeft -= dt;
            if (EventTimeLeft <= 0)
            {
                ActiveEvent = null;
                EventTimeLeft = 0;
                EventPauseLeft = EventPauseLength;
                Notify();
            }
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
