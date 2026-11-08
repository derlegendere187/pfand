using System.Collections.Generic;

namespace Pfand.Core;

public enum EventKind { Geburtstag, Bierball, Konzert, Feiertag, Maennertag, Oktoberfest, Aktion }

/// <summary>
/// Ein startbares Event. Kostet Geld, läuft eine Weile, danach Cooldown.
/// Aktionen (Short) sind kurze Boosts, die auch neben einem großen Event laufen dürfen.
/// </summary>
public class EventDef
{
    public string Id;
    public EventKind Kind;
    public string Name;
    public string Desc;
    public string Skill;           // Skill, der das Event freischaltet
    public double Cost;            // Grundpreis in Euro
    public double Duration;        // Sekunden
    public double Cooldown;        // Sekunden nach dem Start, bis es wieder geht
    public int Guests;             // so viele Gäste kommen extra an feste Plätze
    public int ExtraVisitors;      // so viele normale Besucher passen zusätzlich in den Park
    public double GuestDrinkInterval;
    public double SpawnBoost;      // zusätzliche normale Besucher (Faktor)
    public double ValueBonus = 1;  // solange es läuft, sind alle eingeworfenen Flaschen so viel mehr wert
    public bool Short;             // Aktion: kurzer Boost statt großem Event
}

public static class EventDefs
{
    public static readonly List<EventDef> All = new()
    {
        new EventDef { Id = "geburtstag", Kind = EventKind.Geburtstag, Name = "GEBURTSTAG",
            Desc = "10 Gäste feiern und trinken viel", Skill = "ev_geburtstag", Cost = 15,
            Duration = 45, Cooldown = 150, Guests = 10, GuestDrinkInterval = 3.5, ValueBonus = 1.25 },
        new EventDef { Id = "bierball", Kind = EventKind.Bierball, Name = "BIERBALL",
            Desc = "Zwei Teams werfen auf die Flasche, bei jedem Treffer wird getrunken", Skill = "ev_bierball", Cost = 10,
            Duration = 60, Cooldown = 200, Guests = 8, GuestDrinkInterval = 3, ValueBonus = 1.25 },
        new EventDef { Id = "konzert", Kind = EventKind.Konzert, Name = "KONZERT",
            Desc = "Publikum vor der Bühne, doppelt so viele Besucher", Skill = "ev_konzert", Cost = 60,
            Duration = 60, Cooldown = 300, Guests = 20, GuestDrinkInterval = 4, SpawnBoost = 1.0, ValueBonus = 1.5 },
        new EventDef { Id = "feiertag", Kind = EventKind.Feiertag, Name = "FEIERTAG",
            Desc = "Einen ganzen Tag lang kommen viel mehr Leute, auch Familien", Skill = "ev_feiertag", Cost = 100,
            Duration = 180, Cooldown = 600, ExtraVisitors = 20, SpawnBoost = 1.5, ValueBonus = 1.25 },
        new EventDef { Id = "maennertag", Kind = EventKind.Maennertag, Name = "MÄNNERTAG",
            Desc = "Gruppen mit Bollerwagen ziehen durch den Park und lassen Bier liegen", Skill = "ev_maennertag", Cost = 80,
            Duration = 75, Cooldown = 400, ValueBonus = 1.5 },
        new EventDef { Id = "oktoberfest", Kind = EventKind.Oktoberfest, Name = "OKTOBERFEST",
            Desc = "Der ganze Park feiert: zwei Minuten Wiesn mit Festzelt, Buden, vollen Tischen und Maß um Maß", Skill = "ev_oktoberfest", Cost = 400,
            Duration = 120, Cooldown = 900, Guests = 30, GuestDrinkInterval = 2.5, SpawnBoost = 1.5, ExtraVisitors = 15, ValueBonus = 1.5 },

        // ---- Aktionen: kurz, laufen auch neben einem Event ----
        new EventDef { Id = "pfandaktion", Kind = EventKind.Aktion, Short = true, Name = "PFANDAKTION",
            Desc = "Alle Flaschen sind doppelt so viel wert", Skill = "ev_pfandaktion", Cost = 30,
            Duration = 30, Cooldown = 180, ValueBonus = 2 },
        new EventDef { Id = "happyhour", Kind = EventKind.Aktion, Short = true, Name = "HAPPY HOUR",
            Desc = "Alle trinken doppelt so schnell", Skill = "ev_happyhour", Cost = 20,
            Duration = 30, Cooldown = 150 },
        new EventDef { Id = "gluecksstraehne", Kind = EventKind.Aktion, Short = true, Name = "GLÜCKSSTRÄHNE",
            Desc = "Dreimal so viele Kronkorken", Skill = "ev_gluecksstraehne", Cost = 40,
            Duration = 30, Cooldown = 240 },
    };

    public static EventDef Get(string id) => All.Find(e => e.Id == id);
}
