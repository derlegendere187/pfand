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
        new("muelleimer", gs => gs.Level("muelleimer") > 0, new[]
        {
            "Ein Mülleimer! Manche Leute werfen ihre leeren Flaschen jetzt da rein. Klick auf den Eimer, dann holst du alle auf einmal raus.",
            "Noch ein Trick: Mit RECHTSKLICK auf den Mülleimer kannst du ihn verbessern, zum Beispiel größer machen.",
            "Das geht auch mit Bänken und anderen Sachen im Park. Einfach mal rechts draufklicken!",
        }),
        new("sammler", gs => gs.Level("sammler") > 0, new[]
        {
            "Dein erster Sammler! Der läuft jetzt selbst durch den Park, sammelt Flaschen ein und bringt sie zum Automaten.",
            "Je mehr Sammler du hast, desto weniger musst du selbst klicken.",
        }),
        new("album", gs => gs.CapsUnlocked, new[]
        {
            "Mit dem Sammelalbum kannst du jetzt Kronkorken sammeln. Ab und zu steckt beim Einwerfen einer in einer Flasche. Dosen haben keine.",
            "Kronkorken sind selten! Unten rechts im ALBUM siehst du, welche Motive du schon hast.",
        }),
        new("kronkorken", gs => gs.CapsUnlocked && (gs.Kronkorken > 0 || gs.Caps.Count > 0), new[]
        {
            "Dein erster Kronkorken! Schau ihn dir im Album an.",
            "Kronkorken sind auch die Währung für den SKILLTREE, unten rechts. Dort lernst du Fähigkeiten, und jeder gelernte Knoten lässt neue Äste wachsen.",
            "Keine Sorge: Im Album bleibt der Kronkorken trotzdem, auch wenn du ihn ausgibst.",
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
