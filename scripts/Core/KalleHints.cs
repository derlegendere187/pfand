using System;
using System.Collections.Generic;

namespace Pfand.Core;

/// <summary>Ein Tipp von Kalle. Erscheint einmal, sobald When zutrifft. Skip: Tipp wird stillschweigend abgehakt.</summary>
public record KalleHint(string Id, Func<GameState, bool> When, string[] Pages, Func<GameState, bool> Skip = null);

/// <summary>
/// Kalle erklärt nach der Einführung immer wieder neue Funktionen, sobald sie das erste Mal wichtig werden.
/// Reihenfolge = Vorrang, wenn mehrere gleichzeitig zutreffen.
/// </summary>
public static class KalleHints
{
    public static readonly List<KalleHint> All = new()
    {
        new("tasche_voll", gs => gs.BagFull, new[]
        {
            "Deine Tasche ist voll! Mehr passt gerade nicht rein.",
            "Wirf die Flaschen links im Automaten ein. Mit TASCHE+ bekommst du mehr Platz.",
        }),
        new("laterne", gs => gs.Phase == DayPhase.Nacht, new[]
        {
            "Oh, schon 22 Uhr! Im Dunkeln gehen die Leute nach Hause, und neue kommen kaum noch.",
            "Kauf dir eine Laterne! Dann bleiben die Leute abends länger, und nachts kommen sogar noch welche. Du findest sie rechts bei den Upgrades.",
        }, gs => gs.Level("laterne") > 0),
        new("kronkorken", gs => gs.Kronkorken > 0 || gs.Caps.Count > 0, new[]
        {
            "Oh, ein Kronkorken! Die stecken ab und zu in den Flaschen, die du einwirfst. Dosen haben keine.",
            "Mit Kronkorken lernst du im SKILLTREE neue Sachen, unten rechts. Dort schaltest du auch Neues für den Park frei, zum Beispiel Picknickdecken und Mülleimer.",
            "Erst im Skilltree freischalten, dann rechts bei den Upgrades kaufen. Und im ALBUM siehst du, welche Motive du schon gesammelt hast.",
        }),
        new("muelleimer", gs => gs.Level("muelleimer") > 0, new[]
        {
            "Ein Mülleimer! Manche Leute werfen ihre leeren Flaschen jetzt da rein. Klick auf den Eimer, dann holst du alle auf einmal raus.",
            "Im Skilltree kannst du später alle Mülleimer auf einmal vergrößern. Genauso Bänke, Imbiss und Co.",
        }),
        new("sammler", gs => gs.Level("sammler") > 0, new[]
        {
            "Dein erster Sammler! Der läuft jetzt selbst durch den Park, sammelt Flaschen ein und bringt sie zum Automaten.",
            "Je mehr Sammler du hast, desto weniger musst du selbst klicken.",
        }),
        new("events", gs => gs.AnyEventUnlocked, new[]
        {
            "Du hast dein erstes Event gelernt! Rechts gibt es jetzt neben UPGRADES auch EVENTS.",
            "Ein Event kostet Geld und bringt für kurze Zeit viele Leute in den Park. Danach braucht es eine Pause, und jedes Mal wird es ein bisschen teurer.",
        }),
        new("grill", gs => gs.Level("grill") > 0, new[]
        {
            "Am Grill sitzen ganze Runden und trinken Bier. Die leeren Flaschen landen im Bierkasten daneben.",
            "Klick auf den Kasten, dann kommen die Flaschen in deine Tasche.",
        }),
    };

    /// <summary>Alle Tipps abhaken, die jetzt schon zutreffen (alte Spielstände, Debug-Start).</summary>
    public static void MarkCurrent(GameState gs)
    {
        foreach (var h in All) if (h.When(gs)) gs.SeenHints.Add(h.Id);
    }
}
