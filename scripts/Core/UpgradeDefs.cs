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
    public string LockedText = "";
    public Func<GameState, string> LockedTextFn;
    public Func<GameState, bool> Unlocked = _ => true;
    public Func<GameState, int> MaxBonus;          // z. B. durch Skills
    public int RevealNext = 2;                     // so oft kaufen, bis das nächste Upgrade auftaucht
    public Func<GameState, bool> RevealedBy;       // eigene Bedingung statt der Reihe (z. B. Kalles Tipp)
    public Color C1, C2;

    public double Price(int level) => Math.Round(BasePrice * Math.Pow(Growth, level), 2);
}

public static class UpgradeDefs
{
    static Color H(string hex) => Color.FromHtml(hex);

    // Reihenfolge = Reihenfolge, in der die Upgrades in der Liste auftauchen (nach Preis sortiert)
    public static readonly List<UpgradeDef> All = new()
    {
        new UpgradeDef { Id = "weg", Name = "WEG", Effect = "Mehr Leute kommen vorbei",
            BasePrice = 2.00, Growth = 2.3, Max = 6, C1 = H("#e6cf9f"), C2 = H("#c9ad7a") },
        // Die Laterne zeigt Kalle um 22 Uhr. Sie steht außerhalb der Reihe und blockiert nichts.
        new UpgradeDef { Id = "laterne", Name = "LATERNE", Effect = "Leute bleiben abends länger, nachts kommen noch welche",
            BasePrice = 8.00, Growth = 2.6, Max = 3, MaxBonus = gs => gs.SkillLevel("bau"), RevealedBy = gs => gs.LaterneHint,
            C1 = H("#f2c75c"), C2 = H("#5a4a3a") },
        new UpgradeDef { Id = "tasche", Name = "TASCHE+", Effect = "+4 Plätze in der Tasche",
            BasePrice = 3.00, Growth = 2.2, Max = 6, MaxBonus = gs => 2 * gs.SkillLevel("rucksack"), C1 = H("#b07a4a"), C2 = H("#f4ead2") },
        new UpgradeDef { Id = "picknick", RevealNext = 1, Name = "PICKNICKDECKE", Effect = "Leute setzen sich auf Decken und bleiben länger",
            BasePrice = 6.00, Growth = 2.6, Max = 3, C1 = H("#d9735a"), C2 = H("#fffaf0") },
        new UpgradeDef { Id = "muelleimer", Name = "MÜLLEIMER", Effect = "Flaschen landen im Eimer",
            BasePrice = 10.00, Growth = 2.2, Max = 4, MaxBonus = gs => gs.SkillLevel("bau"), C1 = H("#4f7a5a"), C2 = H("#2f4a38") },
        new UpgradeDef { Id = "bank", Name = "BANK", Effect = "Leute bleiben länger",
            BasePrice = 18.00, Growth = 2.2, Max = 5, MaxBonus = gs => gs.SkillLevel("bau"), C1 = H("#b07a4a"), C2 = H("#7a5434") },
        new UpgradeDef { Id = "gruppen", RevealNext = 1, Name = "FREUNDESKREIS", Effect = "Leute bringen Freunde mit auf die Decke",
            BasePrice = 30.00, Growth = 2.6, Max = 3, LockedText = "ERST PICKNICKDECKE",
            Unlocked = gs => gs.Level("picknick") > 0, C1 = H("#6fa8d6"), C2 = H("#d9735a") },
        new UpgradeDef { Id = "sammler", RevealNext = 1, Name = "SAMMLER", Effect = "Angestellte sammeln und werfen selbst ein",
            BasePrice = 90.00, Growth = 2.3, Max = 5, MaxBonus = gs => 2 * gs.SkillLevel("personal"), C1 = H("#e08a4a"), C2 = H("#2a1d14") },
        new UpgradeDef { Id = "album", RevealNext = 1, Name = "SAMMELALBUM", Effect = "Ab jetzt findest du Kronkorken: fürs Album und den Skilltree",
            BasePrice = 120.00, Growth = 1, Max = 1, C1 = H("#6b3f2a"), C2 = H("#f2c75c") },
        new UpgradeDef { Id = "tischtennis", RevealNext = 1, Name = "TISCHTENNIS", Effect = "Spieler kommen und trinken viel",
            BasePrice = 200.00, Growth = 3.0, Max = 2, C1 = H("#4f8a6a"), C2 = H("#fffaf0") },
        new UpgradeDef { Id = "imbiss", RevealNext = 1, Name = "IMBISSBUDE", Effect = "Mehr Leute, die mehr trinken",
            BasePrice = 500, Growth = 2.5, Max = 1,
            Unlocked = gs => gs.PeakVisitors >= Balance.ImbissUnlockVisitors,
            LockedTextFn = gs => $"{Math.Min(gs.PeakVisitors, Balance.ImbissUnlockVisitors)}/{Balance.ImbissUnlockVisitors} BESUCHER",
            C1 = H("#d9735a"), C2 = H("#fffaf0") },
        new UpgradeDef { Id = "grill", Name = "GRILLPLATZ", Effect = "Grillrunden füllen einen Bierkasten",
            BasePrice = 1800, Growth = 2.5, Max = 1, LockedText = "ERST IMBISS",
            Unlocked = gs => gs.Level("imbiss") > 0, C1 = H("#5a4a3a"), C2 = H("#e08a4a") },
    };

    /// <summary>
    /// Upgrades tauchen nacheinander auf: das nächste erst, wenn das vorherige oft genug gekauft wurde
    /// (RevealNext, höchstens bis zu dessen Maximum). Was man schon hat, bleibt immer sichtbar.
    /// </summary>
    public static bool Visible(GameState gs, UpgradeDef d)
    {
        bool prevVisible = true, prevDone = true;
        if (d.RevealedBy != null) return gs.Level(d.Id) > 0 || d.RevealedBy(gs);
        foreach (var u in All)
        {
            if (u.RevealedBy != null) continue;
            bool visible = gs.Level(u.Id) > 0 || (prevVisible && prevDone);
            if (u == d) return visible;
            prevVisible = visible;
            prevDone = gs.Level(u.Id) >= Math.Min(u.RevealNext, u.Max);
        }
        return false;
    }
}
