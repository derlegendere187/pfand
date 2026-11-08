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
    public static readonly SkillBranch Ausbau = new("AUSBAU", H("#8a5a34"));
    public static readonly SkillBranch Start = new("START", H("#4a3424"));

    static string Pct(double v) => $"{Math.Round(v * 100)} %";

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
        new SkillDef { Id = "ev_geburtstag", Branch = Events, Name = "GEBURTSTAG", Costs = new[] { 2 }, Pos = new(-1, 0),
            Effect = _ => "Event: Geburtstagsfeier",
            Icon = new[] { "...o....", "...#....", "..####..", ".#oooo#.", ".######.", ".#o#o##.", ".######.", "........" } },
        new SkillDef { Id = "ev_bierball", Branch = Events, Name = "BIERBALL", Costs = new[] { 5 }, Parent = "ev_geburtstag", Pos = new(-2, 0),
            Effect = _ => "Event: Bierball-Turnier",
            Icon = new[] { "........", "#......#", "#..##..#", "#..##..#", "#.####.#", "#.####.#", "#......#", "........" } },
        new SkillDef { Id = "ev_konzert", Branch = Events, Name = "KONZERT", Costs = new[] { 10 }, Parent = "ev_bierball", Pos = new(-3, 0),
            Effect = _ => "Event: Konzert im Park",
            Icon = new[] { "....###.", "....#.#.", "....#.#.", "....#.#.", "..###.#.", ".####.#.", ".###.##.", "....###." } },
        new SkillDef { Id = "ev_cooldown", Branch = Events, Name = "GUTE KONTAKTE", Costs = new[] { 4, 8, 14 }, Parent = "ev_bierball", Pos = new(-2, 1),
            Effect = n => $"Event-Pausen -{n * 15} %",
            Icon = new[] { "..####..", ".#oooo#.", "#oo#ooo#", "#oo#ooo#", "#oo###o#", "#oooooo#", ".#oooo#.", "..####.." } },

        new SkillDef { Id = "ev_feiertag", Branch = Events, Name = "FEIERTAG", Costs = new[] { 12 }, Parent = "ev_geburtstag", Pos = new(-1, 1),
            Effect = _ => "Event: Feiertag – einen ganzen Tag lang volle Wiese",
            Icon = new[] { "#......#", "##....##", "#o#..#o#", "#oo##oo#", "########", "........", ".#.#.#..", "........" } },
        new SkillDef { Id = "ev_maennertag", Branch = Events, Name = "MÄNNERTAG", Costs = new[] { 15 }, Parent = "ev_bierball", Pos = new(-3, 1),
            Effect = _ => "Event: Männertag – Bollerwagen ziehen durch den Park",
            Icon = new[] { "........", ".#o#o#..", ".#####..", "########", "#......#", "########", ".##..##.", ".##..##." } },
        new SkillDef { Id = "ev_oktoberfest", Branch = Events, Name = "OKTOBERFEST", Costs = new[] { 30 }, Parent = "ev_konzert", Pos = new(-4, 0),
            Effect = _ => "Event: Oktoberfest im Festzelt",
            Icon = new[] { "...##...", "..#oo#..", ".#o##o#.", "#o#oo#o#", "########", "#oooooo#", "#oo##oo#", "#oo##oo#" } },
        new SkillDef { Id = "ev_sponsor", Branch = Events, Name = "SPONSOREN", Costs = new[] { 4, 8, 14 }, Parent = "ev_konzert", Pos = new(-3, -1),
            Effect = n => $"Events kosten {n * 15} % weniger",
            Icon = new[] { "...##...", ".######.", "##.##...", ".#####..", "...##.#.", "...##.##", ".######.", "...##..." } },

        // ---- Sammeln (unten) ----
        new SkillDef { Id = "aoe", Branch = Sammeln, Name = "GREIFZANGE", Costs = new[] { 1, 3, 6, 12 }, Pos = new(0, 1),
            Effect = n => $"Ein Klick sammelt alle Flaschen im Umkreis ({n} von 4)",
            Icon = new[] { "..####..", ".#....#.", "#..##..#", "#.#oo#.#", "#.#oo#.#", "#..##..#", ".#....#.", "..####.." } },
        new SkillDef { Id = "lastenrad", Branch = Sammeln, Name = "LASTENRAD", Costs = new[] { 8 }, Parent = "aoe", Pos = new(0, 2),
            Effect = _ => "Ganze Bierkästen auf einmal abgeben, volle Kästen bringen € 1,50 Kastenpfand",
            Icon = new[] { "........", "..####..", "..#oo#..", "#######.", "#....#..", ".##..##.", "#oo##oo#", ".##..##." } },
        new SkillDef { Id = "flink", Branch = Sammeln, Name = "FLINKE SAMMLER", Costs = new[] { 3, 6, 12 }, Parent = "aoe", Pos = new(-1, 2),
            Effect = n => $"Sammler +{n * 25} % schneller, tragen {n * 2} Flaschen mehr",
            Icon = new[] { "...##...", "..####..", "...##...", ".######.", "#.####.#", "..#..#..", ".#....#.", "#......#" } },

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

        // ---- Ausbau (oben links) ----
        new SkillDef { Id = "bau", Branch = Ausbau, Name = "BAUGENEHMIGUNG", Costs = new[] { 4, 9, 18 }, Pos = new(-1, -1),
            Effect = n => $"Mülleimer, Bänke und Laternen: Maximum +{n}",
            Icon = new[] { "########", "#oooooo#", "#o####o#", "#oooooo#", "#o###oo#", "#oooooo#", "#ooo#o##", "########" } },
        new SkillDef { Id = "rucksack", Branch = Ausbau, Name = "WANDERRUCKSACK", Costs = new[] { 2, 5, 10 }, Parent = "bau", Pos = new(-2, -2),
            Effect = n => $"Tasche+: Maximum +{n * 2}",
            Icon = new[] { "..####..", ".#....#.", "########", "#oooooo#", "#o####o#", "#oooooo#", "#oooooo#", "########" } },
        new SkillDef { Id = "personal", Branch = Ausbau, Name = "PERSONALCHEF", Costs = new[] { 5, 10, 20 }, Parent = "bau", Pos = new(-1, -2),
            Effect = n => $"Sammler: Maximum +{n * 2}",
            Icon = new[] { "...##...", "..####..", "...##...", "..o##o..", ".######.", ".#o##o#.", "..#..#..", "..#..#.." } },
    };

    public static SkillDef Get(string id) => All.Find(s => s.Id == id);

    /// <summary>Kosten der Skills aus Version 2 (für die Erstattung beim Umstieg).</summary>
    public static readonly Dictionary<string, int> OldCosts = new()
    {
        ["wert1"] = 1, ["wert2"] = 3, ["wert3"] = 8, ["retro"] = 2, ["glueck"] = 4, ["gold"] = 6,
        ["ev_geburtstag"] = 2, ["ev_bierball"] = 4, ["ev_konzert"] = 8, ["ev_cooldown"] = 5,
    };
}
