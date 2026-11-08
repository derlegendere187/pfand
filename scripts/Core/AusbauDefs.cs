using System.Collections.Generic;

namespace Pfand.Core;

/// <summary>Eine Ausbaustufe für ein einzelnes Objekt im Park (z. B. Tisch an eine Bank).</summary>
public record AusbauStep(string Name, string Desc, double Price);

/// <summary>
/// Ausbau einzelner Objekte. Jedes Objekt (z. B. "bank:2" = die dritte Bank) hat eigene Stufen,
/// die man nacheinander kauft. Spätere Objekte derselben Sorte sind etwas teurer.
/// </summary>
public static class AusbauDefs
{
    public static readonly Dictionary<string, (string Title, AusbauStep[] Steps)> Kinds = new()
    {
        ["bank"] = ("BANK", new[]
        {
            new AusbauStep("TISCH DAZU", "Leute bleiben 50 % länger und trinken schneller", 12),
            new AusbauStep("ZWEITE BANK", "Gegenüber am Tisch: 4 statt 2 Plätze", 45),
        }),
        ["muelleimer"] = ("MÜLLEIMER", new[]
        {
            new AusbauStep("GROSSER EIMER", "Passt 25 statt 15 Flaschen", 10),
            new AusbauStep("PFANDRING", "40 Plätze, fängt mehr Flaschen auf", 40),
        }),
        ["imbiss"] = ("IMBISSBUDE", new[]
        {
            new AusbauStep("GRÖSSERE THEKE", "+4 Besucher, mehr Leute holen sich was", 300),
            new AusbauStep("GETRÄNKEKÜHLSCHRANK", "Alle trinken 15 % schneller", 900),
        }),
        ["tischtennis"] = ("TISCHTENNIS", new[]
        {
            new AusbauStep("TURNIERPLATTE", "Spieler bleiben 50 % länger und trinken mehr", 120),
        }),
        ["grill"] = ("GRILLPLATZ", new[]
        {
            new AusbauStep("GROSSER ROST", "Grillrunden bleiben 50 % länger", 800),
            new AusbauStep("KÜHLBOX", "Grillrunden trinken 30 % schneller", 2000),
        }),
    };

    public static string Key(string kind, int index) => $"{kind}:{index}";

    /// <summary>Preis der Stufe step für das index-te Objekt dieser Sorte.</summary>
    public static double Price(string kind, int index, int step)
        => System.Math.Round(Kinds[kind].Steps[step].Price * (1 + 0.3 * index), 2);
}
