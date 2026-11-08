using System.Collections.Generic;

namespace Pfand.Core;

public enum EventKind { Geburtstag, Bierball, Konzert, Feiertag, Maennertag, Oktoberfest }

/// <summary>Ein startbares Event. Kostet Geld, läuft eine Weile, danach Cooldown.</summary>
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
}

public static class EventDefs
{
    public static readonly List<EventDef> All = new()
    {
        new EventDef { Id = "geburtstag", Kind = EventKind.Geburtstag, Name = "GEBURTSTAG",
            Desc = "8 Gäste feiern und trinken viel", Skill = "ev_geburtstag", Cost = 12,
            Duration = 40, Cooldown = 150, Guests = 8, GuestDrinkInterval = 4 },
        new EventDef { Id = "bierball", Kind = EventKind.Bierball, Name = "BIERBALL",
            Desc = "Zwei Teams, bei jedem Treffer wird getrunken", Skill = "ev_bierball", Cost = 10,
            Duration = 45, Cooldown = 200, Guests = 8, GuestDrinkInterval = 3 },
        new EventDef { Id = "konzert", Kind = EventKind.Konzert, Name = "KONZERT",
            Desc = "Publikum vor der Bühne, doppelt so viele Besucher", Skill = "ev_konzert", Cost = 100,
            Duration = 60, Cooldown = 300, Guests = 16, GuestDrinkInterval = 5, SpawnBoost = 1.0 },
        new EventDef { Id = "feiertag", Kind = EventKind.Feiertag, Name = "FEIERTAG",
            Desc = "Einen ganzen Tag lang kommen viel mehr Leute, auch Familien", Skill = "ev_feiertag", Cost = 150,
            Duration = 180, Cooldown = 600, ExtraVisitors = 15, SpawnBoost = 1.5 },
        new EventDef { Id = "maennertag", Kind = EventKind.Maennertag, Name = "MÄNNERTAG",
            Desc = "Gruppen mit Bollerwagen ziehen durch den Park und lassen Bier liegen", Skill = "ev_maennertag", Cost = 300,
            Duration = 60, Cooldown = 400 },
        new EventDef { Id = "oktoberfest", Kind = EventKind.Oktoberfest, Name = "OKTOBERFEST",
            Desc = "Festzelt mit 20 Gästen an langen Tischen", Skill = "ev_oktoberfest", Cost = 1000,
            Duration = 90, Cooldown = 600, Guests = 20, GuestDrinkInterval = 3, SpawnBoost = 0.5 },
    };

    public static EventDef Get(string id) => All.Find(e => e.Id == id);
}
