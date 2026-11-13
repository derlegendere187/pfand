using System;
using System.Collections.Generic;

namespace Pfand.Core;

/// <summary>
/// Ein Tipp von Kalle. Erscheint einmal, sobald When zutrifft. Skip: Tipp wird stillschweigend abgehakt.
/// Focus: worauf jede Seite zeigt (alles andere wird abgedunkelt); fehlt ein Eintrag, gilt der vorige.
/// </summary>
public record KalleHint(string Id, Func<GameState, bool> When, string[] Pages, Func<GameState, bool> Skip = null, string[] Focus = null);

/// <summary>
/// Kalle erklärt nach der Einführung immer wieder neue Funktionen, sobald sie das erste Mal wichtig werden.
/// Reihenfolge = Vorrang, wenn mehrere gleichzeitig zutreffen.
/// </summary>
public static class KalleHints
{
    public static readonly List<KalleHint> All = new()
    {
        new("flyer", gs => gs.FlyerVisible, new[]
        {
            "Guck mal, da liegt ein Flyer im Park! Der startet eine Aktion, und die kostet dich nichts.",
            "Klick ihn direkt an, bevor er wegweht. Drüberwischen reicht nicht. Solche Flyer kommen nur ab und zu.",
        }, Focus: new[] { "flyer" }),
        new("tasche_voll", gs => gs.BagFull, new[]
        {
            "Deine Tasche ist voll! Mehr passt gerade nicht rein.",
            "Wirf die Flaschen links im Automaten ein. Später kannst du im Skilltree eine größere Tasche freischalten.",
        }, Focus: new[] { "bag", "feed" }),
        new("flasche_weg", gs => gs.BottleTaken, new[]
        {
            "Da hat dir jemand eine Flasche vor der Nase weggeschnappt! Wenn Flaschen zu lange liegen, kommen andere Sammler und nehmen sie mit.",
            "In Mülleimern und Kästen bleiben sie dagegen liegen, bis du kommst.",
        }, Focus: new[] { "stranger", "bin" }),
        new("kombo", gs => gs.KomboSeen, new[]
        {
            "Langer Bon! Ab der 10. Flasche auf einmal gibt dir der Automat einen Kombo-Bonus auf den ganzen Bon.",
            "Je voller die Tasche beim Einwerfen, desto mehr Bonus. Eine große Tasche lohnt sich also.",
        }, Focus: new[] { "bon", "bag" }),
        new("laterne", gs => gs.Phase == DayPhase.Nacht, new[]
        {
            "Oh, schon 22 Uhr! Im Dunkeln gehen die Leute nach Hause, und neue kommen kaum noch.",
            "Kauf dir eine Laterne! Dann bleiben die Leute abends länger, und nachts kommen sogar noch welche. Du findest sie rechts bei den Upgrades.",
        }, gs => gs.Level("laterne") > 0, new[] { null, "laterne" }),
        new("kronkorken", gs => gs.Kronkorken > 0 || gs.Caps.Count > 0, new[]
        {
            "Oh, ein Kronkorken! Die stecken ab und zu in den Flaschen, die du selbst einwirfst. Dosen haben keine.",
            "Mit Kronkorken lernst du im SKILLTREE neue Sachen, unten rechts. Fang mit den Glasflaschen an, dann trinken die Leute nicht nur Bier.",
            "Je mehr Kronkorken du dort ausgibst, desto mehr Äste wachsen. Später schaltest du auch Neues für den Park frei, zum Beispiel Picknickdecken und Mülleimer.",
            "Erst im Skilltree freischalten, dann rechts bei den Upgrades kaufen. Und im ALBUM siehst du, welche Motive du schon gesammelt hast.",
        }, Focus: new[] { null, "skills", "skills", "album" }),
        new("muelleimer", gs => gs.Level("muelleimer") > 0, new[]
        {
            "Ein Mülleimer! Manche Leute werfen ihre leeren Flaschen jetzt da rein. Klick auf den Eimer, dann holst du alle auf einmal raus.",
            "Im Skilltree kannst du später alle Mülleimer auf einmal vergrößern. Genauso Bänke, Imbiss und Co.",
        }, Focus: new[] { "bin", "skills" }),
        new("sammler", gs => gs.Level("sammler") > 0, new[]
        {
            "Dein erster Sammler! Der läuft jetzt selbst durch den Park, sammelt Flaschen ein und bringt sie zum Automaten.",
            "Je mehr Sammler du hast, desto weniger musst du selbst klicken.",
        }, Focus: new[] { "collector" }),
        new("events", gs => gs.AnyEventUnlocked, new[]
        {
            "Du hast dein erstes Event gelernt! Rechts gibt es jetzt neben UPGRADES auch EVENTS.",
            "Ein Event kostet Kronkorken und bringt für kurze Zeit viele Leute in den Park. Danach haben alle Events 5 Minuten Pause, und jedes Mal wird es ein bisschen teurer.",
        }, Focus: new[] { "events" }),
        new("grill", gs => gs.Level("grill") > 0, new[]
        {
            "Am Grill sitzen ganze Runden und trinken Bier. Die leeren Flaschen landen im Bierkasten daneben.",
        }, Focus: new[] { "grill" }),
    };

    /// <summary>Alle Tipps abhaken, die jetzt schon zutreffen (alte Spielstände, Debug-Start).</summary>
    public static void MarkCurrent(GameState gs)
    {
        foreach (var h in All) if (h.When(gs)) gs.SeenHints.Add(h.Id);
    }
}
