using System;
using System.Collections.Generic;
using Godot;

namespace Pfand.Core;

/// <summary>Ein Ast des Skilltrees (Farbe für Rahmen und Linien).</summary>
public record SkillBranch(string Name, Color Color);

/// <summary>
/// Ein Knoten im Skilltree. Er sitzt auf einem Rasterplatz (Pos), hängt an einem Elternknoten
/// und kann mehrere Stufen haben. Stufe n kostet Base · Growth^n, mal einem Aufschlag je Abstand zur Wurzel.
/// </summary>
public class SkillDef
{
    public string Id;
    public SkillBranch Branch;
    public string Name;
    public Func<int, string> Effect;   // Wirkung bei Stufe n (n >= 1)
    public double Base, Growth = 1;
    public int Max;
    public string Parent;              // null = hängt an der Wurzel
    public Vector2I Pos;
    public string[] Icon;              // 8×8 Pixel: '#' = Astfarbe, 'o' = hell, '.' = leer

    public int MaxLevel => Max;
}

public static class SkillDefs
{
    public const string Root = "wurzel";

    static Color H(string hex) => Color.FromHtml(hex);

    public static readonly SkillBranch Wert = new("PFANDWERT", H("#c9962e"));
    public static readonly SkillBranch Sorten = new("FLASCHENSORTEN", H("#4f7fb0"));
    public static readonly SkillBranch Glueck = new("GLÜCK", H("#3f9a8f"));
    public static readonly SkillBranch Events = new("EVENTS", H("#c4553f"));
    public static readonly SkillBranch Sammeln = new("SAMMELN", H("#4f8a5a"));
    public static readonly SkillBranch Gaeste = new("GÄSTE", H("#8a5aa8"));
    public static readonly SkillBranch Park = new("PARK", H("#8a5a34"));
    public static readonly SkillBranch Start = new("START", H("#4a3424"));

    static string Pct(double v) => $"{Math.Round(v * 100)} %";

    /// <summary>Text für Knoten, die ein Upgrade freischalten: was es macht und wo man es kauft.</summary>
    static string Unlock(string upgradeId)
    {
        var u = UpgradeDefs.Get(upgradeId);
        return $"Upgrade {u.Name}: {u.Effect}";
    }

    static string EventUnlock(string eventId)
    {
        var e = EventDefs.Get(eventId);
        return $"Event: {e.Desc}";
    }

    static string AktionUnlock(string eventId)
    {
        var e = EventDefs.Get(eventId);
        return $"Flyer im Park: {e.Desc} ({GameState.Seconds(e.Duration)})";
    }

    /// <summary>Wie viele Knoten zwischen Wurzel und d liegen (direkt an der Wurzel = 1).</summary>
    public static int Depth(SkillDef d)
    {
        int n = 0;
        for (var x = d; x != null && x.Id != Root; x = Get(x.Parent ?? Root)) n++;
        return n;
    }

    /// <summary>Preis einer Stufe: wächst mit jeder Stufe und je weiter weg von der Wurzel.</summary>
    public static int Cost(SkillDef d, int level)
        => (int)Math.Ceiling(d.Base * Math.Pow(d.Growth, level) * Math.Pow(Balance.SkillDepthCost, Math.Max(0, Depth(d) - 1)));

    // --- Icons ---
    static readonly string[] IBier = { "...##...", "...##...", "..####..", "..#oo#..", "..#oo#..", "..####..", "..####..", "..####.." };
    static readonly string[] IGlas = { "...##...", "...##...", "..#oo#..", "..#oo#..", "..####..", "..####..", "..#oo#..", "..####.." };
    static readonly string[] IPlastik = { "...oo...", "..####..", ".#oooo#.", ".######.", ".#oooo#.", ".######.", ".#oooo#.", ".######." };
    static readonly string[] IDose = { "........", "..####..", ".#oooo#.", ".######.", ".#o##o#.", ".######.", ".#oooo#.", "..####.." };
    static readonly string[] ICoin = { "..####..", ".######.", "##o##o##", "##oooo##", "##o##o##", "##oooo##", ".######.", "..####.." };
    static readonly string[] IPlus = { "........", "...##...", "...##...", ".######.", ".######.", "...##...", "...##...", "........" };
    static readonly string[] IClock = { "..####..", ".#oooo#.", "#oo#ooo#", "#oo#ooo#", "#oo###o#", "#oooooo#", ".#oooo#.", "..####.." };
    static readonly string[] IStar = { "...##...", "...##...", "########", ".######.", "..####..", ".##..##.", "##....##", "........" };
    static readonly string[] ICap = { "..####..", ".#oooo#.", "#o#oo#o#", "#oo##oo#", "#oo##oo#", "#o#oo#o#", ".#oooo#.", "..####.." };
    static readonly string[] IFlyer = { ".######.", ".#oooo#.", ".#o##o#.", ".#oooo#.", ".#o##o#.", ".#oooo#.", ".######.", "........" };
    static readonly string[] IPeople = { "........", ".##..##.", "#oo##oo#", ".##..##.", "####.###", "####.###", "####.###", "........" };
    static readonly string[] ICollector = { "...##...", "..####..", "...##...", ".######.", "#.####.#", "..####.o", "..#..#oo", "..#..#oo" };
    static readonly string[] IBag = { "..####..", ".#....#.", "########", "#oooooo#", "#o####o#", "#oooooo#", "#oooooo#", "########" };
    static readonly string[] IEye = { "........", "..####..", ".#oooo#.", "#oo##oo#", "#oo##oo#", ".#oooo#.", "..####..", "........" };
    static readonly string[] ITrophy = { "#.####.#", "#.####.#", ".######.", "..####..", "...##...", "...##...", "..####..", ".######." };
    static readonly string[] ITable = { "........", "########", "........", "########", "#oooooo#", "########", ".#....#.", ".#....#." };
    static readonly string[] IFridge = { "..####..", ".#oooo#.", ".#o##o#.", ".#oooo#.", ".######.", ".#oooo#.", ".#o##o#.", ".######." };

    static SkillDef S(string id, SkillBranch b, string name, string parent, int x, int y, double cost, double growth, int max,
        Func<int, string> effect, string[] icon)
        => new() { Id = id, Branch = b, Name = name, Parent = parent, Pos = new(x, y), Base = cost, Growth = growth, Max = max, Effect = effect, Icon = icon };

    /// <summary>Ausbau eines Events: länger und mehr wert.</summary>
    static SkillDef EventPlus(string eventId, string parent, int x, int y, double cost)
    {
        var e = EventDefs.Get(eventId);
        return S("evp_" + eventId, Events, e.Name + "+", parent, x, y, cost, 1.5, 5,
            n => $"{e.Name} dauert {n * 10} % länger, Flaschen +{n * 5} % mehr wert", IStar);
    }

    public static readonly List<SkillDef> All = new()
    {
        new SkillDef { Id = Root, Branch = Start, Name = "PFANDSAMMLER", Max = 0, Pos = new(0, 0),
            Effect = _ => "",
            Icon = new[] { "..####..", "..#oo#..", "...##...", "..####..", ".#oooo#.", ".#o##o#.", ".#oooo#.", "..####.." } },

        // ---- Pfandwert (rechts) ----
        S("wert", Wert, "PFANDWERT", null, 1, 0, 1, 1.28, 20, n => $"Alle Flaschen +{n * 5} % wert",
            new[] { "........", ".#....#.", "..#..#..", "...##...", "..####..", "...##...", "..####..", "...##..." }),
        S("abgabe", Wert, "SCHNELLE ABGABE", "wert", 2, 0, 2, 1.25, 15, n => $"Automat zählt {n * 10} % schneller",
            new[] { "........", "#...#...", "##..##..", "###.###.", "###.###.", "##..##..", "#...#...", "........" }),
        S("kombo", Wert, "LANGER BON", "auto", 4, 0, 6, 1.6, 5,
            n => $"Kombo bis {Pct(Balance.KomboMax + n * Balance.KomboMaxPerLevel)} statt {Pct(Balance.KomboMax)}",
            new[] { ".######.", ".#oooo#.", ".######.", ".#oooo#.", ".######.", ".#oooo#.", ".#o#o#o.", "........" }),
        S("wert_koenig", Wert, "PFANDKÖNIG", "kombo", 5, 0, 150, 1, 1, _ => "Alle Flaschen doppelt so viel wert",
            new[] { "........", "#..#..#.", "##.#.##.", "#######.", "#o#o#o#.", "#######.", "........", "........" }),
        S("auto", Wert, "AUTO-EINWURF", "abgabe", 3, 0, 4, 1, 1, _ => "Volle Tasche wird automatisch eingeworfen",
            new[] { "..####..", ".#oooo#.", ".#o##o#.", ".#oooo#.", ".######.", "...##...", ".######.", "..####.." }),
        S("auto_pause", Wert, "AUTO-PAUSE", "auto", 3, -1, 6, 1, 1,
            _ => "Auch halbvolle Tasche nach 4 s ohne Sammeln", IClock),

        // ---- Flaschensorten (oben): am Anfang gibt es nur Bier. Glasflaschen sind der allererste Knoten ----
        S("bier_wert", Sorten, "BIERKENNER", null, 1, -1, 1, 1.3, 10, n => $"Bierflaschen +{n * 10} % wert", IBier),
        S("bier_oft", Sorten, "MEHR BIER", "bier_wert", 2, -1, 2, 1.4, 5, n => $"Bier wird {n * 10} % öfter getrunken", IPlus),
        S("s_glas", Sorten, "GLASFLASCHEN", null, 0, -1, 1, 1, 1,
            _ => $"Glasflaschen: {GameState.Euro(BottleInfo.BaseValue(BottleType.Wasser))}, mit Kronkorken", IGlas),
        S("glas_wert", Sorten, "GLASKENNER", "s_glas", 1, -2, 2, 1.3, 10, n => $"Glasflaschen +{n * 10} % wert", ICoin),
        S("glas_oft", Sorten, "MEHR GLAS", "glas_wert", 2, -2, 3, 1.4, 5, n => $"Glasflaschen {n * 15} % öfter", IPlus),
        S("s_plastik", Sorten, "PLASTIKFLASCHEN", "s_glas", 0, -2, 8, 1, 1,
            _ => $"Plastikflaschen: {GameState.Euro(BottleInfo.BaseValue(BottleType.Plastik))}", IPlastik),
        S("plastik_wert", Sorten, "PLASTIKPROFI", "s_plastik", 1, -3, 2, 1.3, 10, n => $"Plastikflaschen +{n * 10} % wert", ICoin),
        S("plastik_oft", Sorten, "MEHR PLASTIK", "plastik_wert", 2, -3, 3, 1.4, 5, n => $"Plastikflaschen {n * 15} % öfter", IPlus),
        S("s_dose", Sorten, "DOSEN", "s_plastik", 0, -3, 10, 1, 1,
            _ => $"Dosen: {GameState.Euro(BottleInfo.BaseValue(BottleType.Dose))}, ohne Kronkorken", IDose),
        S("dose_wert", Sorten, "DOSENPROFI", "s_dose", 1, -4, 2, 1.3, 10, n => $"Dosen +{n * 10} % wert", ICoin),
        S("dose_oft", Sorten, "MEHR DOSEN", "dose_wert", 2, -4, 3, 1.4, 5, n => $"Dosen {n * 15} % öfter", IPlus),
        S("retro", Sorten, "RETRO-FLASCHEN", "s_dose", 0, -4, 8, 1.4, 6,
            n => $"{(n * Balance.RetroChancePerLevel * 100).ToString("0.0")} % der Flaschen sind Retro ({GameState.Euro(BottleInfo.BaseValue(BottleType.Retro))})",
            new[] { "...oo...", "...##...", "...##...", "..####..", ".#o##o#.", ".######.", ".#o##o#.", ".######." }),
        S("gold", Sorten, "GOLDENE FLASCHE", "retro", 0, -5, 40, 1.8, 3,
            n => $"{(n * Balance.GoldChancePerLevel * 100).ToString("0.0")} % Goldflaschen ({GameState.Euro(BottleInfo.BaseValue(BottleType.Gold))})",
            new[] { "...o....", "...##...", "..#oo#..", "..#oo#..", ".#oooo#.", ".#o##o#.", ".#oooo#.", ".######." }),

        // ---- Glück (unten rechts): Kronkorken und Aktionen ----
        S("glueck", Glueck, "GLÜCKSDECKEL", null, 1, 1, 2, 1.35, 10, n => $"Chance auf Kronkorken +{n * 10} %", ICap),
        S("kenner", Glueck, "KENNERBLICK", "glueck", 2, 1, 4, 1.5, 5, n => $"Seltene Motive kommen {n * 15} % öfter",
            new[] { "...##...", "..#oo#..", ".#oooo#.", "#oooooo#", ".#oooo#.", "..#oo#..", "...##...", "........" }),
        S("albumjaeger", Glueck, "ALBUMJÄGER", "kenner", 3, 1, 8, 1.5, 6, n => $"{n * 10} % der Kronkorken sind neue Motive",
            new[] { "########", "#oo#oo##", "#oo#oo##", "########", "#oo#..##", "#oo#..##", "########", "........" }),
        S("ak_pfandaktion", Glueck, "PFANDAKTION", "glueck", 1, 2, 5, 1, 1, _ => AktionUnlock("pfandaktion"),
            new[] { "........", ".######.", "#oo##oo#", "#o#o#oo#", "#oo#o#o#", "#oo##oo#", ".######.", "........" }),
        S("flyer", Glueck, "FLYER", "ak_pfandaktion", 2, 2, 4, 1.3, 10, n => $"Flyer {n * 10} % öfter", IFlyer),
        S("ak_happyhour", Glueck, "HAPPY HOUR", "ak_pfandaktion", 1, 3, 6, 1, 1, _ => AktionUnlock("happyhour"),
            new[] { "#......#", ".#....#.", "..####..", "..#oo#..", "..#oo#..", "...##...", "...##...", "..####.." }),
        S("ak_gluecksstraehne", Glueck, "GLÜCKSSTRÄHNE", "ak_happyhour", 1, 4, 8, 1, 1, _ => AktionUnlock("gluecksstraehne"),
            new[] { "...##...", "..#oo#..", "########", ".#oooo#.", "..#oo#..", ".#o##o#.", "#o#..#o#", "........" }),

        // ---- Events (unten links): eine Reihe, darunter je ein Ausbau ----
        S("ev_geburtstag", Events, "GEBURTSTAG", null, -1, 1, 3, 1, 1, _ => EventUnlock("geburtstag"),
            new[] { "...o....", "...#....", "..####..", ".#oooo#.", ".######.", ".#o#o##.", ".######.", "........" }),
        S("ev_bierball", Events, "BIERBALL", "ev_geburtstag", -2, 1, 5, 1, 1, _ => EventUnlock("bierball"),
            new[] { "........", "#......#", "#..##..#", "#..##..#", "#.####.#", "#.####.#", "#......#", "........" }),
        S("ev_konzert", Events, "KONZERT", "ev_bierball", -3, 1, 8, 1, 1, _ => EventUnlock("konzert"),
            new[] { "....###.", "....#.#.", "....#.#.", "....#.#.", "..###.#.", ".####.#.", ".###.##.", "....###." }),
        S("ev_maennertag", Events, "MÄNNERTAG", "ev_konzert", -4, 1, 12, 1, 1, _ => EventUnlock("maennertag"),
            new[] { "........", ".#o#o#..", ".#####..", "########", "#......#", "########", ".##..##.", ".##..##." }),
        S("ev_feiertag", Events, "FEIERTAG", "ev_maennertag", -5, 1, 18, 1, 1, _ => EventUnlock("feiertag"),
            new[] { "#......#", "##....##", "#o#..#o#", "#oo##oo#", "########", "........", ".#.#.#..", "........" }),
        S("ev_oktoberfest", Events, "OKTOBERFEST", "ev_feiertag", -6, 1, 70, 1, 1, _ => EventUnlock("oktoberfest"),
            new[] { "...##...", "..#oo#..", ".#o##o#.", "#o#oo#o#", "########", "#oooooo#", "#oo##oo#", "#oo##oo#" }),
        EventPlus("geburtstag", "ev_geburtstag", -1, 2, 2),
        EventPlus("bierball", "ev_bierball", -2, 2, 2),
        EventPlus("konzert", "ev_konzert", -3, 2, 3),
        EventPlus("maennertag", "ev_maennertag", -4, 2, 3),
        EventPlus("feiertag", "ev_feiertag", -5, 2, 4),
        EventPlus("oktoberfest", "ev_oktoberfest", -6, 2, 6),
        S("ev_cooldown", Events, "GUTE KONTAKTE", "evp_bierball", -2, 3, 4, 1.3, 10,
            n => $"Pause zwischen Events {n * 6} % kürzer ({GameState.Seconds(Balance.EventPause * (1 - n * Balance.EventPauseCutPerLevel))} statt {GameState.Seconds(Balance.EventPause)})", IClock),
        S("ev_sponsor", Events, "SPONSOREN", "evp_konzert", -3, 3, 5, 1.3, 10, n => $"Events kosten {n * 5} % weniger Kronkorken",
            new[] { "...##...", ".######.", "##.##...", ".#####..", "...##.#.", "...##.##", ".######.", "...##..." }),

        // ---- Gäste (links) ----
        S("liegen", Gaeste, "SORGLOSE GÄSTE", null, -1, 0, 1, 1.3, 10,
            n => $"Leute lassen {Pct(Balance.LeaveChanceBase + n * Balance.LeaveChancePerLevel)} ihrer Flaschen liegen",
            new[] { "...##...", "..####..", "...##...", "..####..", "..####..", "........", "....##..", "...####." }),
        S("durst", Gaeste, "DURSTIGE GÄSTE", "liegen", -2, 0, 2, 1.3, 10, n => $"Leute trinken {n * 3} % schneller",
            new[] { "..o..o..", "...##...", "..#oo#..", "..####..", "..#oo#..", "..####..", "..#oo#..", "..####.." }),
        S("stammgaeste", Gaeste, "STAMMGÄSTE", "durst", -3, 0, 3, 1.3, 10, n => $"+{n * 5} % Besucher", IPeople),
        S("platz", Gaeste, "GROSSER PARK", "stammgaeste", -4, 0, 4, 1.3, 10, n => $"+{n} Plätze im Park",
            new[] { "........", "########", "#oo#ooo#", "#oo#ooo#", "####o###", "#ooo#oo#", "########", "........" }),
        S("revier", Gaeste, "RUHIGES REVIER", "platz", -5, 0, 5, 1.4, 5,
            n => $"Flaschen bleiben {Balance.BottleLifetime + n * Balance.BottleLifetimePerLevel} statt {Balance.BottleLifetime} s liegen", IEye),

        // ---- Sammeln (unten) ----
        S("aoe", Sammeln, "GREIFZANGE", null, 0, 1, 1, 1.25, 20, n => $"Sammelradius der Maus +{n * 15} %",
            new[] { "..####..", ".#....#.", "#..##..#", "#.#oo#.#", "#.#oo#.#", "#..##..#", ".#....#.", "..####.." }),
        S("u_tasche", Sammeln, "TASCHE+", "aoe", 0, 2, 2, 1, 1, _ => Unlock("tasche"), IBag),
        S("rucksack", Sammeln, "WANDERRUCKSACK", "u_tasche", 0, 3, 3, 1.3, 10, n => $"TASCHE+ kann {n}× öfter gekauft werden",
            new[] { "..####..", ".#....#.", "########", "#oooooo#", "#o####o#", "#oooooo#", "#oooooo#", "########" }),
        S("lastenrad", Sammeln, "LASTENRAD", "rucksack", 0, 4, 10, 1, 1,
            _ => $"Leere Kästen bringen {GameState.Euro(Balance.KastenPfand)} Kastenpfand",
            new[] { "........", "..####..", "..#oo#..", "#######.", "#....#..", ".##..##.", "#oo##oo#", ".##..##." }),

        // ---- Park (oben links): Upgrades freischalten und Objekte ausbauen ----
        // Stamm nach oben: jedes Upgrade für den Park. Seitenäste nach links: Ausbau aller Objekte dieser Sorte.
        S("u_picknick", Park, "PICKNICKDECKE", null, -1, -1, 1, 1, 1, _ => Unlock("picknick"),
            new[] { "........", "#o#o#o#o", "o#o#o#o#", "#o#o#o#o", "o#o#o#o#", "#o#o#o#o", "........", "........" }),
        S("u_gruppen", Park, "FREUNDESKREIS", "u_picknick", -2, -1, 3, 1, 1, _ => Unlock("gruppen"), IPeople),
        S("kastenrunde", Park, "BIERKASTEN", "u_gruppen", -3, -1, 3, 1.3, 10,
            n => $"{Pct(Balance.GroupCrateChanceBase + n * Balance.GroupCrateChancePerLevel)} der Gruppen bringen einen Bierkasten",
            new[] { "........", "########", "#o#o#o##", "########", "#o#o#o##", "########", "#o#o#o##", "########" }),
        S("u_muelleimer", Park, "MÜLLEIMER", "u_picknick", -1, -2, 2, 1, 1, _ => Unlock("muelleimer"),
            new[] { "..####..", "########", ".#o#o#o.", ".#o#o#o.", ".#o#o#o.", ".#o#o#o.", ".######.", "........" }),
        S("eimer_gross", Park, "GROSSE EIMER", "u_muelleimer", -2, -2, 2, 1.3, 8,
            n => $"Alle Mülleimer fassen {Balance.BinCapacity + n * Balance.BinCapacityPerLevel} statt {Balance.BinCapacity} Flaschen",
            new[] { "########", "########", "#o#o#o##", "#o#o#o##", "#o#o#o##", "#o#o#o##", "#o#o#o##", "########" }),
        S("eimer_ring", Park, "PFANDTONNE", "eimer_gross", -3, -2, 3, 1.4, 5, n => $"Pro Eimer landen {n * 3} % mehr Flaschen darin",
            new[] { "########", "#..##..#", "########", "#oo##oo#", "#oo##oo#", "########", "#oo##oo#", "########" }),
        S("u_bank", Park, "BANK", "u_muelleimer", -1, -3, 3, 1, 1, _ => Unlock("bank"),
            new[] { "........", "########", "#oooooo#", "########", "........", "########", ".#....#.", ".#....#." }),
        S("bank_tisch", Park, "TISCHE", "u_bank", -2, -3, 3, 1.4, 5, n => $"Leute auf Bänken bleiben {n * 10} % länger", ITable),
        S("bank_doppel", Park, "SITZGRUPPEN", "bank_tisch", -3, -3, 8, 1, 1, _ => "4 statt 2 Plätze pro Bank",
            new[] { "########", "........", "########", "#oooooo#", "########", "........", "########", ".#....#." }),
        S("tischrunde", Park, "TISCHRUNDE", "bank_doppel", -4, -3, 4, 1.4, 5, n => $"Leute auf Bänken trinken {n * 3} % schneller", ITable),
        S("bau", Park, "BAUGENEHMIGUNG", "tischrunde", -5, -3, 6, 1.6, 5, n => $"Mülleimer, Bänke und Laternen: Maximum +{n}",
            new[] { "########", "#oooooo#", "#o####o#", "#oooooo#", "#o###oo#", "#oooooo#", "#ooo#o##", "########" }),
        S("u_sammler", Park, "SAMMLER", "u_bank", -1, -4, 4, 1, 1, _ => Unlock("sammler"), ICollector),
        S("personal", Park, "PERSONALCHEF", "u_sammler", -2, -4, 4, 1.4, 6, n => $"SAMMLER kann {n}× öfter gekauft werden",
            new[] { "...##...", "..####..", "...##...", "..o##o..", ".######.", ".#o##o#.", "..#..#..", "..#..#.." }),
        S("flink", Park, "FLINKE SAMMLER", "personal", -3, -4, 3, 1.3, 10, n => $"Sammler laufen {n * 8} % schneller",
            new[] { "...##...", "..####..", "...##...", ".######.", "#.####.#", "..#..#..", ".#....#.", "#......#" }),
        S("tragkraft", Park, "STARKE SAMMLER", "flink", -4, -4, 4, 1.4, 6, n => $"Sammler tragen {n} Flaschen mehr", IBag),
        S("ehrlich", Park, "EHRLICHE SAMMLER", "tragkraft", -5, -4, 3, 1.3, 10,
            n => $"Sammler bringen Kronkorken ({n * 10} % so oft wie du)", ICap),
        S("u_tischtennis", Park, "TISCHTENNIS", "u_sammler", -1, -5, 5, 1, 1, _ => Unlock("tischtennis"),
            new[] { "########", "#ooo#oo#", "#ooo#oo#", "########", "#ooo#oo#", "#ooo#oo#", "########", ".#....#." }),
        S("tt_turnier", Park, "TURNIERPLATTEN", "u_tischtennis", -2, -5, 3, 1.4, 5, n => $"Tischtennis-Spieler bleiben {n * 10} % länger",
            new[] { "#..##..#", "########", "#ooo#oo#", "########", "#ooo#oo#", "########", ".#....#.", ".#....#." }),
        S("tt_sieger", Park, "SIEGERBIER", "tt_turnier", -3, -5, 4, 1.4, 5, n => $"Tischtennis-Spieler trinken {n * 5} % schneller", ITrophy),
        S("u_imbiss", Park, "IMBISSBUDE", "u_tischtennis", -1, -6, 8, 1, 1, _ => Unlock("imbiss"),
            new[] { "#o#o#o#o", "########", "#......#", "#.####.#", "#.#oo#.#", "#......#", "########", "#......#" }),
        S("imbiss_theke", Park, "GRÖSSERE THEKE", "u_imbiss", -2, -6, 3, 1.4, 6, n => $"+{n} Plätze im Park mit Imbiss",
            new[] { "#o#o#o#o", "########", "#......#", "########", "#oooooo#", "########", "#......#", "#......#" }),
        S("imbiss_kuehl", Park, "GETRÄNKEKÜHLSCHRANK", "imbiss_theke", -3, -6, 4, 1.4, 5, n => $"Alle trinken {n * 3} % schneller mit Imbiss", IFridge),
        S("imbiss_schild", Park, "WERBESCHILD", "imbiss_kuehl", -4, -6, 4, 1.5, 4, n => $"{n * 5} % mehr Leute holen sich erst was am Imbiss",
            new[] { "########", "#oooooo#", "#o#oo#o#", "#oooooo#", "########", "...##...", "...##...", "..####.." }),
        S("u_grill", Park, "GRILLPLATZ", "u_imbiss", -1, -7, 12, 1, 1, _ => Unlock("grill"),
            new[] { "..o.o...", "...o.o..", "########", "#o#o#o##", ".######.", "...##...", "..#..#..", ".#....#." }),
        S("grill_rost", Park, "GROSSER ROST", "u_grill", -2, -7, 4, 1.4, 5, n => $"Grillrunden bleiben {n * 10} % länger",
            new[] { "........", "########", "#o#o#o##", "########", "#o#o#o##", "########", "..#..#..", ".#....#." }),
        S("grill_kuehl", Park, "KÜHLBOX", "grill_rost", -3, -7, 5, 1.4, 5, n => $"Grillrunden trinken {n * 6} % schneller",
            new[] { "........", ".######.", "#oooooo#", "########", "#oooooo#", "#oooooo#", "########", "........" }),
    };

    public static SkillDef Get(string id) => All.Find(s => s.Id == id);

    /// <summary>
    /// Die Äste an der Wurzel wachsen nacheinander: ein Ast öffnet sich erst, wenn man insgesamt so viele
    /// Kronkorken im Skilltree ausgegeben hat. Am Anfang gibt es nur die Glasflaschen.
    /// </summary>
    public static readonly Dictionary<string, int> Gates = new()
    {
        ["s_glas"] = 0,
        ["u_picknick"] = 1,
        ["wert"] = 4,
        ["aoe"] = 8,
        ["bier_wert"] = 14,
        ["liegen"] = 20,
        ["glueck"] = 30,
        ["ev_geburtstag"] = 45,
    };

    /// <summary>Ab wie vielen ausgegebenen Kronkorken der Knoten lernbar ist (nur Äste direkt an der Wurzel).</summary>
    public static int GateOf(SkillDef d) => d.Parent == null && Gates.TryGetValue(d.Id, out var g) ? g : 0;
}
