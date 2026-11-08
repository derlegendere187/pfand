using System;
using System.Collections.Generic;
using Godot;

namespace Pfand.Core;

/// <summary>Ein Ast des Skilltrees (Farbe für Rahmen und Linien).</summary>
public record SkillBranch(string Name, Color Color);

/// <summary>
/// Ein Knoten im Skilltree. Er sitzt auf einem Rasterplatz (Pos), hängt an einem Elternknoten
/// und kann mehrere Stufen haben. Jede Stufe kostet Kronkorken (Costs[stufe]).
/// </summary>
public class SkillDef
{
    public string Id;
    public SkillBranch Branch;
    public string Name;
    public Func<int, string> Effect;   // Wirkung bei Stufe n (n >= 1)
    public int[] Costs;
    public string Parent;              // null = hängt an der Wurzel
    public Vector2I Pos;
    public string[] Icon;              // 8×8 Pixel: '#' = Astfarbe, 'o' = hell, '.' = leer

    public int MaxLevel => Costs.Length;
}

public static class SkillDefs
{
    public const string Root = "wurzel";

    static Color H(string hex) => Color.FromHtml(hex);

    public static readonly SkillBranch Wert = new("PFANDWERT", H("#c9962e"));
    public static readonly SkillBranch Selten = new("SELTENE FLASCHEN", H("#4f7fb0"));
    public static readonly SkillBranch Events = new("EVENTS", H("#c4553f"));
    public static readonly SkillBranch Sammeln = new("SAMMELN", H("#4f8a5a"));
    public static readonly SkillBranch Gaeste = new("GÄSTE", H("#8a5aa8"));
    public static readonly SkillBranch Park = new("PARK", H("#8a5a34"));
    public static readonly SkillBranch Start = new("START", H("#4a3424"));

    static string Pct(double v) => $"{Math.Round(v * 100)} %";

    /// <summary>Text für Knoten, die ein Upgrade freischalten: was es macht und wo man es kauft.</summary>
    static string Unlock(string upgradeId)
    {
        var u = UpgradeDefs.Get(upgradeId);
        return $"Schaltet das Upgrade {u.Name} frei: {u.Effect}. Kaufen kannst du es danach rechts bei den Upgrades.";
    }

    /// <summary>Wie viele Knoten zwischen Wurzel und d liegen (direkt an der Wurzel = 1).</summary>
    public static int Depth(SkillDef d)
    {
        int n = 0;
        for (var x = d; x != null && x.Id != Root; x = Get(x.Parent ?? Root)) n++;
        return n;
    }

    /// <summary>Preis einer Stufe: je weiter weg von der Wurzel, desto teurer.</summary>
    public static int Cost(SkillDef d, int level)
        => (int)Math.Ceiling(d.Costs[level] * (1 + Balance.SkillDepthCost * Math.Max(0, Depth(d) - 1)));

    public static readonly List<SkillDef> All = new()
    {
        new SkillDef { Id = Root, Branch = Start, Name = "PFANDSAMMLER", Costs = Array.Empty<int>(), Pos = new(0, 0),
            Effect = _ => "Hier fängt alles an. Von hier wachsen die Äste.",
            Icon = new[] { "..####..", "..#oo#..", "...##...", "..####..", ".#oooo#.", ".#o##o#.", ".#oooo#.", "..####.." } },

        // ---- Pfandwert (rechts) ----
        new SkillDef { Id = "wert", Branch = Wert, Name = "PFANDWERT", Costs = new[] { 1, 2, 4, 7, 12 }, Pos = new(1, 0),
            Effect = n => $"Alle Flaschen +{n * 20} % wert",
            Icon = new[] { "........", ".#....#.", "..#..#..", "...##...", "..####..", "...##...", "..####..", "...##..." } },
        new SkillDef { Id = "abgabe", Branch = Wert, Name = "SCHNELLE ABGABE", Costs = new[] { 2, 4, 8 }, Parent = "wert", Pos = new(2, 0),
            Effect = n => $"Automat zählt {Pct(n * 0.5)} schneller",
            Icon = new[] { "........", "#...#...", "##..##..", "###.###.", "###.###.", "##..##..", "#...#...", "........" } },
        new SkillDef { Id = "auto", Branch = Wert, Name = "AUTO-EINWURF", Costs = new[] { 3, 6 }, Parent = "abgabe", Pos = new(2, -1),
            Effect = n => n >= 2 ? "Volle Tasche und Tasche nach 4 s Pause werden automatisch eingeworfen"
                : "Eine volle Tasche wird automatisch eingeworfen",
            Icon = new[] { "..####..", ".#oooo#.", ".#o##o#.", ".#oooo#.", ".######.", "...##...", ".######.", "..####.." } },
        new SkillDef { Id = "wert_glas", Branch = Wert, Name = "GLASKENNER", Costs = new[] { 3, 6, 12 }, Parent = "wert", Pos = new(1, -1),
            Effect = n => $"Bier- und Glasflaschen +{n * 50} % wert",
            Icon = new[] { "...##...", "...##...", "..#oo#..", "..#oo#..", "..####..", "..####..", "..#oo#..", "..####.." } },
        new SkillDef { Id = "wert_dose", Branch = Wert, Name = "DOSENPROFI", Costs = new[] { 3, 6, 12 }, Parent = "abgabe", Pos = new(2, 1),
            Effect = n => $"Dosen und Plastik +{n * 25} % wert",
            Icon = new[] { "........", "..####..", ".#oooo#.", ".######.", ".#o##o#.", ".######.", ".#oooo#.", "..####.." } },
        new SkillDef { Id = "wert_koenig", Branch = Wert, Name = "PFANDKÖNIG", Costs = new[] { 30 }, Parent = "abgabe", Pos = new(3, 0),
            Effect = _ => "Alle Flaschen doppelt so viel wert",
            Icon = new[] { "........", "#..#..#.", "##.#.##.", "#######.", "#o#o#o#.", "#######.", "........", "........" } },

        // ---- Seltene Flaschen (oben) ----
        new SkillDef { Id = "retro", Branch = Selten, Name = "RETRO-FLASCHEN", Costs = new[] { 2, 5, 10 }, Parent = "glueck", Pos = new(0, -2),
            Effect = n => $"{n * 2} % der Flaschen sind Retro (€ 0,75)",
            Icon = new[] { "...oo...", "...##...", "...##...", "..####..", ".#o##o#.", ".######.", ".#o##o#.", ".######." } },
        new SkillDef { Id = "glueck", Branch = Selten, Name = "GLÜCKSDECKEL", Costs = new[] { 1, 3, 8 }, Pos = new(0, -1),
            Effect = n => $"Chance auf Kronkorken +{n * 50} %",
            Icon = new[] { "..####..", ".#oooo#.", "#o#oo#o#", "#oo##oo#", "#oo##oo#", "#o#oo#o#", ".#oooo#.", "..####.." } },
        new SkillDef { Id = "gold", Branch = Selten, Name = "GOLDENE FLASCHE", Costs = new[] { 10, 25 }, Parent = "retro", Pos = new(0, -3),
            Effect = n => $"Ganz selten ({(n * 0.4).ToString("0.0")} %) eine Flasche für € 5,00",
            Icon = new[] { "...o....", "...##...", "..#oo#..", "..#oo#..", ".#oooo#.", ".#o##o#.", ".#oooo#.", ".######." } },
        new SkillDef { Id = "albumjaeger", Branch = Selten, Name = "ALBUMJÄGER", Costs = new[] { 12 }, Parent = "retro", Pos = new(1, -2),
            Effect = _ => "Kronkorken sind öfter Motive, die dir noch fehlen",
            Icon = new[] { "########", "#oo#oo##", "#oo#oo##", "########", "#oo#..##", "#oo#..##", "########", "........" } },

        // ---- Events (links) ----
        new SkillDef { Id = "ev_geburtstag", Branch = Events, Name = "GEBURTSTAG", Costs = new[] { 8 }, Pos = new(-1, 0),
            Effect = _ => "Event: Geburtstagsfeier",
            Icon = new[] { "...o....", "...#....", "..####..", ".#oooo#.", ".######.", ".#o#o##.", ".######.", "........" } },
        new SkillDef { Id = "ev_bierball", Branch = Events, Name = "BIERBALL", Costs = new[] { 10 }, Parent = "ev_geburtstag", Pos = new(-2, 0),
            Effect = _ => "Event: Bierball-Turnier",
            Icon = new[] { "........", "#......#", "#..##..#", "#..##..#", "#.####.#", "#.####.#", "#......#", "........" } },
        new SkillDef { Id = "ev_konzert", Branch = Events, Name = "KONZERT", Costs = new[] { 15 }, Parent = "ev_bierball", Pos = new(-3, 0),
            Effect = _ => "Event: Konzert im Park",
            Icon = new[] { "....###.", "....#.#.", "....#.#.", "....#.#.", "..###.#.", ".####.#.", ".###.##.", "....###." } },
        new SkillDef { Id = "ev_cooldown", Branch = Events, Name = "GUTE KONTAKTE", Costs = new[] { 6, 12, 20 }, Parent = "ev_bierball", Pos = new(-2, 1),
            Effect = n => $"Event-Pausen -{n * 15} %",
            Icon = new[] { "..####..", ".#oooo#.", "#oo#ooo#", "#oo#ooo#", "#oo###o#", "#oooooo#", ".#oooo#.", "..####.." } },

        new SkillDef { Id = "ev_feiertag", Branch = Events, Name = "FEIERTAG", Costs = new[] { 15 }, Parent = "ev_geburtstag", Pos = new(-1, 1),
            Effect = _ => "Event: Feiertag – einen ganzen Tag lang volle Wiese",
            Icon = new[] { "#......#", "##....##", "#o#..#o#", "#oo##oo#", "########", "........", ".#.#.#..", "........" } },
        new SkillDef { Id = "ev_maennertag", Branch = Events, Name = "MÄNNERTAG", Costs = new[] { 18 }, Parent = "ev_bierball", Pos = new(-3, 1),
            Effect = _ => "Event: Männertag – Bollerwagen ziehen durch den Park",
            Icon = new[] { "........", ".#o#o#..", ".#####..", "########", "#......#", "########", ".##..##.", ".##..##." } },
        new SkillDef { Id = "ev_oktoberfest", Branch = Events, Name = "OKTOBERFEST", Costs = new[] { 25 }, Parent = "ev_konzert", Pos = new(-4, 0),
            Effect = _ => "Event: Oktoberfest im Festzelt",
            Icon = new[] { "...##...", "..#oo#..", ".#o##o#.", "#o#oo#o#", "########", "#oooooo#", "#oo##oo#", "#oo##oo#" } },
        new SkillDef { Id = "ev_sponsor", Branch = Events, Name = "SPONSOREN", Costs = new[] { 6, 12, 20 }, Parent = "ev_maennertag", Pos = new(-3, 2),
            Effect = n => $"Events kosten {n * 15} % weniger",
            Icon = new[] { "...##...", ".######.", "##.##...", ".#####..", "...##.#.", "...##.##", ".######.", "...##..." } },

        // ---- Sammeln (unten) ----
        new SkillDef { Id = "aoe", Branch = Sammeln, Name = "GREIFZANGE", Costs = new[] { 1, 3, 6, 12 }, Pos = new(0, 1),
            Effect = n => $"Ein Klick sammelt alle Flaschen im Umkreis ({n} von 4)",
            Icon = new[] { "..####..", ".#....#.", "#..##..#", "#.#oo#.#", "#.#oo#.#", "#..##..#", ".#....#.", "..####.." } },
        new SkillDef { Id = "rucksack", Branch = Sammeln, Name = "WANDERRUCKSACK", Costs = new[] { 2, 5, 10 }, Parent = "aoe", Pos = new(0, 2),
            Effect = n => $"Tasche+: Maximum +{n * 2}",
            Icon = new[] { "..####..", ".#....#.", "########", "#oooooo#", "#o####o#", "#oooooo#", "#oooooo#", "########" } },
        new SkillDef { Id = "lastenrad", Branch = Sammeln, Name = "LASTENRAD", Costs = new[] { 8 }, Parent = "rucksack", Pos = new(0, 3),
            Effect = _ => "Ganze Bierkästen auf einmal mitnehmen und abgeben (sonst nur Flasche für Flasche), volle Kästen bringen € 1,50 Kastenpfand",
            Icon = new[] { "........", "..####..", "..#oo#..", "#######.", "#....#..", ".##..##.", "#oo##oo#", ".##..##." } },

        // ---- Gäste (unten rechts) ----
        new SkillDef { Id = "liegen", Branch = Gaeste, Name = "SORGLOSE GÄSTE", Costs = new[] { 1, 2, 4, 7, 12 }, Pos = new(1, 1),
            Effect = n => $"Leute lassen {Pct(Balance.LeaveChanceBase + n * Balance.LeaveChancePerLevel)} ihrer Flaschen liegen",
            Icon = new[] { "...##...", "..####..", "...##...", "..####..", "..####..", "........", "....##..", "...####." } },
        new SkillDef { Id = "durst", Branch = Gaeste, Name = "DURSTIGE GÄSTE", Costs = new[] { 3, 6, 12 }, Parent = "liegen", Pos = new(1, 2),
            Effect = n => $"Leute trinken {n * 10} % schneller",
            Icon = new[] { "..o..o..", "...##...", "..#oo#..", "..####..", "..#oo#..", "..####..", "..#oo#..", "..####.." } },
        new SkillDef { Id = "stammgaeste", Branch = Gaeste, Name = "STAMMGÄSTE", Costs = new[] { 3, 7, 14 }, Parent = "liegen", Pos = new(2, 2),
            Effect = n => $"+{n * 15} % Besucher, +{n * 2} Plätze im Park",
            Icon = new[] { "........", ".##..##.", "#oo##oo#", ".##..##.", "####.###", "####.###", "####.###", "........" } },

        // ---- Park (oben links): Upgrades freischalten und Objekte ausbauen ----
        // Stamm nach oben: jedes Upgrade für den Park. Seitenäste nach links: Ausbau aller Objekte dieser Sorte.
        new SkillDef { Id = "u_picknick", Branch = Park, Name = "PICKNICKDECKE", Costs = new[] { 1 }, Pos = new(-1, -1),
            Effect = _ => Unlock("picknick"),
            Icon = new[] { "........", "#o#o#o#o", "o#o#o#o#", "#o#o#o#o", "o#o#o#o#", "#o#o#o#o", "........", "........" } },
        new SkillDef { Id = "u_gruppen", Branch = Park, Name = "FREUNDESKREIS", Costs = new[] { 3 }, Parent = "u_picknick", Pos = new(-2, -1),
            Effect = _ => Unlock("gruppen"),
            Icon = new[] { "........", ".##..##.", "#oo##oo#", ".##..##.", "####.###", "####.###", "####.###", "........" } },
        new SkillDef { Id = "kastenrunde", Branch = Park, Name = "BIERKASTEN", Costs = new[] { 3, 6, 10 }, Parent = "u_gruppen", Pos = new(-3, -1),
            Effect = n => $"Freundesgruppen bringen mit {Pct(Balance.GroupCrateChanceBase + n * Balance.GroupCrateChancePerLevel)} Chance einen Bierkasten mit",
            Icon = new[] { "........", "########", "#o#o#o##", "########", "#o#o#o##", "########", "#o#o#o##", "########" } },
        new SkillDef { Id = "u_muelleimer", Branch = Park, Name = "MÜLLEIMER", Costs = new[] { 2 }, Parent = "u_picknick", Pos = new(-1, -2),
            Effect = _ => Unlock("muelleimer"),
            Icon = new[] { "..####..", "########", ".#o#o#o.", ".#o#o#o.", ".#o#o#o.", ".#o#o#o.", ".######.", "........" } },
        new SkillDef { Id = "eimer_gross", Branch = Park, Name = "GROSSE EIMER", Costs = new[] { 3 }, Parent = "u_muelleimer", Pos = new(-2, -2),
            Effect = _ => "Alle Mülleimer fassen 25 statt 15 Flaschen",
            Icon = new[] { "########", "########", "#o#o#o##", "#o#o#o##", "#o#o#o##", "#o#o#o##", "#o#o#o##", "########" } },
        new SkillDef { Id = "eimer_ring", Branch = Park, Name = "PFANDRINGE", Costs = new[] { 6 }, Parent = "eimer_gross", Pos = new(-3, -2),
            Effect = _ => "Pfandringe an allen Eimern: 40 Plätze, sie fangen mehr Flaschen auf",
            Icon = new[] { "..####..", ".#oooo#.", "#o####o#", "#o#..#o#", "#o#..#o#", "#o####o#", ".#oooo#.", "..####.." } },
        new SkillDef { Id = "u_bank", Branch = Park, Name = "BANK", Costs = new[] { 3 }, Parent = "u_muelleimer", Pos = new(-1, -3),
            Effect = _ => Unlock("bank"),
            Icon = new[] { "........", "########", "#oooooo#", "########", "........", "########", ".#....#.", ".#....#." } },
        new SkillDef { Id = "bank_tisch", Branch = Park, Name = "TISCHE", Costs = new[] { 4 }, Parent = "u_bank", Pos = new(-2, -3),
            Effect = _ => "Alle Bänke bekommen einen Tisch: Leute bleiben 50 % länger und trinken schneller",
            Icon = new[] { "........", "########", "........", "########", "#oooooo#", "########", ".#....#.", ".#....#." } },
        new SkillDef { Id = "bank_doppel", Branch = Park, Name = "SITZGRUPPEN", Costs = new[] { 8 }, Parent = "bank_tisch", Pos = new(-3, -3),
            Effect = _ => "Eine zweite Bank gegenüber am Tisch: 4 statt 2 Plätze",
            Icon = new[] { "########", "........", "########", "#oooooo#", "########", "........", "########", ".#....#." } },
        new SkillDef { Id = "bau", Branch = Park, Name = "BAUGENEHMIGUNG", Costs = new[] { 4, 9, 18 }, Parent = "bank_doppel", Pos = new(-4, -3),
            Effect = n => $"Mülleimer, Bänke und Laternen: Maximum +{n}",
            Icon = new[] { "########", "#oooooo#", "#o####o#", "#oooooo#", "#o###oo#", "#oooooo#", "#ooo#o##", "########" } },
        new SkillDef { Id = "u_sammler", Branch = Park, Name = "SAMMLER", Costs = new[] { 5 }, Parent = "u_bank", Pos = new(-1, -4),
            Effect = _ => Unlock("sammler"),
            Icon = new[] { "...##...", "..####..", "...##...", ".######.", "#.####.#", "..####.o", "..#..#oo", "..#..#oo" } },
        new SkillDef { Id = "personal", Branch = Park, Name = "PERSONALCHEF", Costs = new[] { 5, 10, 20 }, Parent = "u_sammler", Pos = new(-2, -4),
            Effect = n => $"Sammler: Maximum +{n * 2}",
            Icon = new[] { "...##...", "..####..", "...##...", "..o##o..", ".######.", ".#o##o#.", "..#..#..", "..#..#.." } },
        new SkillDef { Id = "flink", Branch = Park, Name = "FLINKE SAMMLER", Costs = new[] { 3, 6, 12 }, Parent = "personal", Pos = new(-3, -4),
            Effect = n => $"Sammler +{n * 25} % schneller, tragen {n * 2} Flaschen mehr",
            Icon = new[] { "...##...", "..####..", "...##...", ".######.", "#.####.#", "..#..#..", ".#....#.", "#......#" } },
        new SkillDef { Id = "u_tischtennis", Branch = Park, Name = "TISCHTENNIS", Costs = new[] { 6 }, Parent = "u_sammler", Pos = new(-1, -5),
            Effect = _ => Unlock("tischtennis"),
            Icon = new[] { "########", "#ooo#oo#", "#ooo#oo#", "########", "#ooo#oo#", "#ooo#oo#", "########", ".#....#." } },
        new SkillDef { Id = "tt_turnier", Branch = Park, Name = "TURNIERPLATTEN", Costs = new[] { 8 }, Parent = "u_tischtennis", Pos = new(-2, -5),
            Effect = _ => "Alle Platten werden Turnierplatten: Spieler bleiben 50 % länger und trinken mehr",
            Icon = new[] { "#..##..#", "########", "#ooo#oo#", "########", "#ooo#oo#", "########", ".#....#.", ".#....#." } },
        new SkillDef { Id = "u_imbiss", Branch = Park, Name = "IMBISSBUDE", Costs = new[] { 10 }, Parent = "u_tischtennis", Pos = new(-1, -6),
            Effect = _ => Unlock("imbiss"),
            Icon = new[] { "#o#o#o#o", "########", "#......#", "#.####.#", "#.#oo#.#", "#......#", "########", "#......#" } },
        new SkillDef { Id = "imbiss_theke", Branch = Park, Name = "GRÖSSERE THEKE", Costs = new[] { 10 }, Parent = "u_imbiss", Pos = new(-2, -6),
            Effect = _ => "+4 Besucher, mehr Leute holen sich was am Imbiss",
            Icon = new[] { "#o#o#o#o", "########", "#......#", "########", "#oooooo#", "########", "#......#", "#......#" } },
        new SkillDef { Id = "imbiss_kuehl", Branch = Park, Name = "GETRÄNKEKÜHLSCHRANK", Costs = new[] { 15 }, Parent = "imbiss_theke", Pos = new(-3, -6),
            Effect = _ => "Alle im Park trinken 15 % schneller",
            Icon = new[] { "..####..", ".#oooo#.", ".#o##o#.", ".#oooo#.", ".######.", ".#oooo#.", ".#o##o#.", ".######." } },
        new SkillDef { Id = "u_grill", Branch = Park, Name = "GRILLPLATZ", Costs = new[] { 15 }, Parent = "u_imbiss", Pos = new(-1, -7),
            Effect = _ => Unlock("grill"),
            Icon = new[] { "..o.o...", "...o.o..", "########", "#o#o#o##", ".######.", "...##...", "..#..#..", ".#....#." } },
        new SkillDef { Id = "grill_rost", Branch = Park, Name = "GROSSER ROST", Costs = new[] { 15 }, Parent = "u_grill", Pos = new(-2, -7),
            Effect = _ => "Grillrunden bleiben 50 % länger",
            Icon = new[] { "........", "########", "#o#o#o##", "########", "#o#o#o##", "########", "..#..#..", ".#....#." } },
        new SkillDef { Id = "grill_kuehl", Branch = Park, Name = "KÜHLBOX", Costs = new[] { 20 }, Parent = "grill_rost", Pos = new(-3, -7),
            Effect = _ => "Grillrunden trinken 30 % schneller",
            Icon = new[] { "........", ".######.", "#oooooo#", "########", "#oooooo#", "#oooooo#", "########", "........" } },
    };

    public static SkillDef Get(string id) => All.Find(s => s.Id == id);

    /// <summary>Kosten der Skills aus Version 2 (für die Erstattung beim Umstieg).</summary>
    public static readonly Dictionary<string, int> OldCosts = new()
    {
        ["wert1"] = 1, ["wert2"] = 3, ["wert3"] = 8, ["retro"] = 2, ["glueck"] = 4, ["gold"] = 6,
        ["ev_geburtstag"] = 2, ["ev_bierball"] = 4, ["ev_konzert"] = 8, ["ev_cooldown"] = 5,
    };
}
