using System.Collections.Generic;

namespace Pfand.Core;

public enum EventKind { Geburtstag, Bierball, Konzert }

/// <summary>Ein startbares Event. Läuft eine Weile, danach Cooldown.</summary>
public class EventDef
{
    public string Id;
    public EventKind Kind;
    public string Name;
    public string Desc;
    public string Skill;           // Skill, der das Event freischaltet
    public double Duration;        // Sekunden
    public double Cooldown;        // Sekunden nach dem Start, bis es wieder geht
    public int Guests;             // so viele Gäste kommen extra
    public double GuestDrinkInterval;
    public double SpawnBoost;      // zusätzliche normale Besucher (Faktor)
}

public static class EventDefs
{
    public static readonly List<EventDef> All = new()
    {
        new EventDef { Id = "geburtstag", Kind = EventKind.Geburtstag, Name = "GEBURTSTAG",
            Desc = "8 Gäste feiern und trinken viel", Skill = "ev_geburtstag",
            Duration = 40, Cooldown = 150, Guests = 8, GuestDrinkInterval = 4, SpawnBoost = 0 },
        new EventDef { Id = "bierball", Kind = EventKind.Bierball, Name = "BIERBALL",
            Desc = "Zwei Teams, jede Menge Bierflaschen", Skill = "ev_bierball",
            Duration = 45, Cooldown = 200, Guests = 8, GuestDrinkInterval = 3, SpawnBoost = 0 },
        new EventDef { Id = "konzert", Kind = EventKind.Konzert, Name = "KONZERT",
            Desc = "Publikum vor der Bühne, doppelt so viele Besucher", Skill = "ev_konzert",
            Duration = 60, Cooldown = 300, Guests = 16, GuestDrinkInterval = 5, SpawnBoost = 1.0 },
    };

    public static EventDef Get(string id) => All.Find(e => e.Id == id);
}
