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
    public Color C1, C2;

    public double Price(int level) => Math.Round(BasePrice * Math.Pow(Growth, level), 2);
}

public static class UpgradeDefs
{
    static Color H(string hex) => Color.FromHtml(hex);

    public static readonly List<UpgradeDef> All = new()
    {
        new UpgradeDef { Id = "weg", Name = "WEG", Effect = "Mehr Leute kommen vorbei",
            BasePrice = 1.50, Growth = 2.2, Max = 6, C1 = H("#e6cf9f"), C2 = H("#c9ad7a") },
        new UpgradeDef { Id = "tasche", Name = "TASCHE+", Effect = "+4 Plätze in der Tasche",
            BasePrice = 1.00, Growth = 2.0, Max = 6, C1 = H("#b07a4a"), C2 = H("#f4ead2") },
        new UpgradeDef { Id = "muelleimer", Name = "MÜLLEIMER", Effect = "Flaschen landen im Eimer",
            BasePrice = 4.00, Growth = 2.0, Max = 4, C1 = H("#4f7a5a"), C2 = H("#2f4a38") },
        new UpgradeDef { Id = "bank", Name = "BANK", Effect = "Leute bleiben länger",
            BasePrice = 8.00, Growth = 2.0, Max = 5, C1 = H("#b07a4a"), C2 = H("#7a5434") },
        new UpgradeDef { Id = "gruppen", Name = "FREUNDESKREIS", Effect = "Leute bringen Freunde mit",
            BasePrice = 6.00, Growth = 2.5, Max = 3, C1 = H("#6fa8d6"), C2 = H("#d9735a") },
        new UpgradeDef { Id = "sammler", Name = "SAMMLER", Effect = "Angestellte sammeln und werfen selbst ein",
            BasePrice = 20.00, Growth = 2.0, Max = 5, C1 = H("#e08a4a"), C2 = H("#2a1d14") },
        new UpgradeDef { Id = "tischtennis", Name = "TISCHTENNIS", Effect = "Spieler kommen und trinken viel",
            BasePrice = 40.00, Growth = 3.0, Max = 2, C1 = H("#4f8a6a"), C2 = H("#fffaf0") },
        new UpgradeDef { Id = "laterne", Name = "LATERNE", Effect = "Leute bleiben auch abends",
            BasePrice = 25.00, Growth = 2.2, Max = 3, C1 = H("#f2c75c"), C2 = H("#5a4a3a") },
        new UpgradeDef { Id = "imbiss", Name = "IMBISSBUDE", Effect = "Mehr Leute, die mehr trinken",
            BasePrice = 150, Growth = 2.5, Max = 1,
            Unlocked = gs => gs.PeakVisitors >= Balance.ImbissUnlockVisitors,
            LockedTextFn = gs => $"{Math.Min(gs.PeakVisitors, Balance.ImbissUnlockVisitors)}/{Balance.ImbissUnlockVisitors} BESUCHER",
            C1 = H("#d9735a"), C2 = H("#fffaf0") },
        new UpgradeDef { Id = "grill", Name = "GRILLPLATZ", Effect = "Grillrunden füllen einen Bierkasten",
            BasePrice = 500, Growth = 2.5, Max = 1, LockedText = "ERST IMBISS",
            Unlocked = gs => gs.Level("imbiss") > 0, C1 = H("#5a4a3a"), C2 = H("#e08a4a") },
    };
}
