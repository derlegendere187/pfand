using System;
using System.Collections.Generic;
using Godot;

namespace Pfand.Core;

public class UpgradeDef
{
    public string Id;
    public string Name;
    public string Effect;
    public double BasePrice;
    public double Growth;
    public int Max;
    public string Skill;                           // Skill im PARK-Ast, der das Upgrade freischaltet (null = von Anfang an)
    public string LockedText = "";
    public Func<GameState, bool> Unlocked = _ => true;   // zusätzliche Bedingung zum Kaufen
    public Func<GameState, int> MaxBonus;          // z. B. durch Skills
    public int RevealNext = 2;                     // Grund-Upgrades: so oft kaufen, bis das nächste auftaucht
    public Func<GameState, bool> RevealedBy;       // eigene Bedingung statt der Reihe (z. B. Kalles Tipp)
    public Color C1, C2;

    public double Price(int level) => Math.Round(BasePrice * Math.Pow(Growth, level), 2);
}

public static class UpgradeDefs
{
    static Color H(string hex) => Color.FromHtml(hex);

    // Reihenfolge = Reihenfolge in der Liste. Alles mit Skill erscheint erst, wenn es im Skilltree freigeschaltet ist.
    public static readonly List<UpgradeDef> All = new()
    {
        new UpgradeDef { Id = "weg", Name = "WEG", Effect = "Mehr Leute kommen vorbei",
            BasePrice = 2.00, Growth = 2.3, Max = 6, C1 = H("#e6cf9f"), C2 = H("#c9ad7a") },
        // Die Laterne zeigt Kalle um 22 Uhr
        new UpgradeDef { Id = "laterne", Name = "LATERNE", Effect = "Leute bleiben abends länger, nachts kommen noch welche",
            BasePrice = 3.00, Growth = 2.2, Max = 3, MaxBonus = gs => gs.SkillLevel("bau"), RevealedBy = gs => gs.LaterneHint,
            C1 = H("#f2c75c"), C2 = H("#5a4a3a") },
        new UpgradeDef { Id = "tasche", RevealNext = 1, Name = "TASCHE+", Effect = "+4 Plätze in der Tasche",
            BasePrice = 8.00, Growth = 2.6, Max = 6, MaxBonus = gs => 2 * gs.SkillLevel("rucksack"), C1 = H("#b07a4a"), C2 = H("#f4ead2") },
        new UpgradeDef { Id = "picknick", Skill = "u_picknick", Name = "PICKNICKDECKE", Effect = "Leute setzen sich auf Decken und bleiben länger",
            BasePrice = 6.00, Growth = 2.6, Max = 3, C1 = H("#d9735a"), C2 = H("#fffaf0") },
        new UpgradeDef { Id = "muelleimer", Skill = "u_muelleimer", Name = "MÜLLEIMER", Effect = "Flaschen landen im Eimer",
            BasePrice = 10.00, Growth = 2.2, Max = 4, MaxBonus = gs => gs.SkillLevel("bau"), C1 = H("#4f7a5a"), C2 = H("#2f4a38") },
        new UpgradeDef { Id = "bank", Skill = "u_bank", Name = "BANK", Effect = "Leute bleiben länger",
            BasePrice = 18.00, Growth = 2.2, Max = 5, MaxBonus = gs => gs.SkillLevel("bau"), C1 = H("#b07a4a"), C2 = H("#7a5434") },
        new UpgradeDef { Id = "gruppen", Skill = "u_gruppen", Name = "FREUNDESKREIS", Effect = "Leute bringen Freunde mit auf die Decke, manchmal sogar einen Bierkasten",
            BasePrice = 30.00, Growth = 2.6, Max = 3, LockedText = "ERST PICKNICKDECKE",
            Unlocked = gs => gs.Level("picknick") > 0, C1 = H("#6fa8d6"), C2 = H("#d9735a") },
        new UpgradeDef { Id = "sammler", Skill = "u_sammler", Name = "SAMMLER", Effect = "Angestellte sammeln und werfen selbst ein",
            BasePrice = 90.00, Growth = 2.3, Max = 5, MaxBonus = gs => 2 * gs.SkillLevel("personal"), C1 = H("#e08a4a"), C2 = H("#2a1d14") },
        new UpgradeDef { Id = "tischtennis", Skill = "u_tischtennis", Name = "TISCHTENNIS", Effect = "Spieler kommen und trinken viel",
            BasePrice = 200.00, Growth = 3.0, Max = 2, C1 = H("#4f8a6a"), C2 = H("#fffaf0") },
        new UpgradeDef { Id = "imbiss", Skill = "u_imbiss", Name = "IMBISSBUDE", Effect = "Mehr Leute, die mehr trinken",
            BasePrice = 500, Growth = 2.5, Max = 1, C1 = H("#d9735a"), C2 = H("#fffaf0") },
        new UpgradeDef { Id = "grill", Skill = "u_grill", Name = "GRILLPLATZ", Effect = "Grillrunden füllen einen Bierkasten",
            BasePrice = 1800, Growth = 2.5, Max = 1, LockedText = "ERST IMBISS",
            Unlocked = gs => gs.Level("imbiss") > 0, C1 = H("#5a4a3a"), C2 = H("#e08a4a") },
    };

    public static UpgradeDef Get(string id) => All.Find(u => u.Id == id);

    /// <summary>
    /// Was in der Liste steht: freigeschaltete Upgrades aus dem Skilltree, die Laterne nach Kalles Tipp
    /// und die Grund-Upgrades nacheinander (das nächste erst, wenn das vorherige oft genug gekauft wurde).
    /// </summary>
    public static bool Visible(GameState gs, UpgradeDef d)
    {
        if (gs.Level(d.Id) > 0) return true;
        if (d.Skill != null) return gs.HasSkill(d.Skill);
        if (d.RevealedBy != null) return d.RevealedBy(gs);
        bool prevVisible = true, prevDone = true;
        foreach (var u in All)
        {
            if (u.Skill != null || u.RevealedBy != null) continue;
            bool visible = gs.Level(u.Id) > 0 || (prevVisible && prevDone);
            if (u == d) return visible;
            prevVisible = visible;
            prevDone = gs.Level(u.Id) >= Math.Min(u.RevealNext, u.Max);
        }
        return false;
    }
}
