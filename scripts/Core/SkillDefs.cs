using System.Collections.Generic;

namespace Pfand.Core;

/// <summary>Ein Knoten im Skilltree. Kostet Kronkorken und braucht ggf. einen anderen Skill vorher.</summary>
public class SkillDef
{
    public string Id;
    public string Branch;
    public string Name;
    public string Desc;
    public int Cost;
    public string Requires;
}

public static class SkillDefs
{
    public static readonly string[] Branches = { "PFANDWERT", "SELTENE FLASCHEN", "EVENTS" };

    public static readonly List<SkillDef> All = new()
    {
        // Pfandwert
        new SkillDef { Id = "wert1", Branch = "PFANDWERT", Name = "GUTER RIECHER", Desc = "Flaschen +25 % wert", Cost = 1 },
        new SkillDef { Id = "wert2", Branch = "PFANDWERT", Name = "PFANDPROFI", Desc = "Flaschen +25 % wert", Cost = 3, Requires = "wert1" },
        new SkillDef { Id = "wert3", Branch = "PFANDWERT", Name = "PFANDKÖNIG", Desc = "Flaschen +50 % wert", Cost = 8, Requires = "wert2" },

        // Seltene Flaschen
        new SkillDef { Id = "retro", Branch = "SELTENE FLASCHEN", Name = "RETRO-FLASCHEN", Desc = "Alte Glasflaschen tauchen auf (€ 0,75)", Cost = 2 },
        new SkillDef { Id = "glueck", Branch = "SELTENE FLASCHEN", Name = "GLÜCKSDECKEL", Desc = "Doppelte Chance auf Kronkorken", Cost = 4, Requires = "retro" },
        new SkillDef { Id = "gold", Branch = "SELTENE FLASCHEN", Name = "GOLDENE FLASCHE", Desc = "Ganz selten: eine Flasche für € 5,00", Cost = 6, Requires = "glueck" },

        // Events
        new SkillDef { Id = "ev_geburtstag", Branch = "EVENTS", Name = "GEBURTSTAG", Desc = "Event: Geburtstagsfeier", Cost = 2 },
        new SkillDef { Id = "ev_bierball", Branch = "EVENTS", Name = "BIERBALL", Desc = "Event: Bierball-Turnier", Cost = 4, Requires = "ev_geburtstag" },
        new SkillDef { Id = "ev_konzert", Branch = "EVENTS", Name = "KONZERT", Desc = "Event: Konzert im Park", Cost = 8, Requires = "ev_bierball" },
        new SkillDef { Id = "ev_cooldown", Branch = "EVENTS", Name = "GUTE KONTAKTE", Desc = "Event-Cooldowns -30 %", Cost = 5, Requires = "ev_bierball" },
    };

    public static SkillDef Get(string id) => All.Find(s => s.Id == id);
}
