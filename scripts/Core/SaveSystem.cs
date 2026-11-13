using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

namespace Pfand.Core;

public class SaveData
{
    public int Version { get; set; } = 6;
    public double Money { get; set; }
    public double TotalEarned { get; set; }
    public long BottlesFed { get; set; }
    public int PeakVisitors { get; set; }
    public int Kronkorken { get; set; }
    public double TimeOfDay { get; set; }
    public List<int> Bag { get; set; } = new();
    public Dictionary<string, int> Upgrades { get; set; } = new();
    public Dictionary<string, int> Ausbau { get; set; }           // nur bis Version 4 (Ausbau pro Objekt)
    public Dictionary<int, int> Caps { get; set; } = new();
    public List<string> Skills { get; set; } = new();            // nur Version 2
    public Dictionary<string, int> SkillLevels { get; set; } = new();
    public double EventPause { get; set; }
    public Dictionary<string, int> EventCount { get; set; } = new();
    public bool TutorialDone { get; set; }
    public bool LaterneHint { get; set; }                        // nur alte Spielstände
    public List<string> SeenHints { get; set; }                  // null = Spielstand von vor Kalles Tipps
    // Statistik (ab Version 3)
    public double PlayTime { get; set; }
    public Dictionary<int, long> FedByType { get; set; } = new();
    public long CollectedByHand { get; set; }
    public long CollectedByCollectors { get; set; }
    public int EventsHeld { get; set; }
    public int KaestenAbgegeben { get; set; }
    public double BestBon { get; set; }
}

/// <summary>Speichert den Spielstand als JSON nach user://save.json.</summary>
public static class SaveSystem
{
    const string Path = "user://save.json";

    public static void Save(GameState s)
    {
        var data = new SaveData
        {
            Money = s.Money,
            TotalEarned = s.TotalEarned,
            BottlesFed = s.BottlesFed,
            PeakVisitors = s.PeakVisitors,
            Kronkorken = s.Kronkorken,
            TimeOfDay = s.TimeOfDay,
            Bag = s.Bag.Select(b => (int)b).ToList(),
            Upgrades = new Dictionary<string, int>(s.Upgrades),
            Caps = new Dictionary<int, int>(s.Caps),
            SkillLevels = new Dictionary<string, int>(s.Skills),
            EventPause = s.EventPauseLeft,
            EventCount = new Dictionary<string, int>(s.EventCount),
            TutorialDone = !s.TutorialActive,
            SeenHints = s.SeenHints.ToList(),
            PlayTime = s.PlayTime,
            FedByType = new Dictionary<int, long>(s.FedByType),
            CollectedByHand = s.CollectedByHand,
            CollectedByCollectors = s.CollectedByCollectors,
            EventsHeld = s.EventsHeld,
            KaestenAbgegeben = s.KaestenAbgegeben,
            BestBon = s.BestBon,
        };
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        using var f = FileAccess.Open(Path, FileAccess.ModeFlags.Write);
        if (f == null)
        {
            GD.PushError($"Speichern fehlgeschlagen: {FileAccess.GetOpenError()}");
            return;
        }
        f.StoreString(json);
    }

    /// <summary>Lädt in den bestehenden Spielstand, damit UI-Abos erhalten bleiben.</summary>
    public static bool Load(GameState s)
    {
        if (!FileAccess.FileExists(Path)) return false;
        try
        {
            using var f = FileAccess.Open(Path, FileAccess.ModeFlags.Read);
            var data = JsonSerializer.Deserialize<SaveData>(f.GetAsText());
            if (data == null) return false;
            s.Money = data.Money;
            s.TotalEarned = data.TotalEarned;
            s.BottlesFed = data.BottlesFed;
            s.PeakVisitors = data.PeakVisitors;
            s.TimeOfDay = data.TimeOfDay;
            s.Bag = data.Bag.Where(b => Enum.IsDefined(typeof(BottleType), b)).Select(b => (BottleType)b).ToList();
            s.Upgrades = data.Upgrades ?? new();
            s.Caps = (data.Caps ?? new()).Where(kv => kv.Key >= 0 && kv.Key < CapData.Count).ToDictionary(kv => kv.Key, kv => kv.Value);
            s.EventPauseLeft = data.EventPause;
            s.EventCount = data.EventCount ?? new();
            // Alte Spielstände kennen die Einführung noch nicht: wer schon Flaschen eingeworfen hat, braucht sie nicht
            s.SeenHints = new HashSet<string>(data.SeenHints ?? new());
            if (data.LaterneHint || s.Upgrades.ContainsKey("laterne")) s.SeenHints.Add("laterne");
            s.Tutorial = data.TutorialDone || data.BottlesFed > 0 ? TutorialStep.Done : TutorialStep.Intro;
            // Alte Spielstände (Version 1) kannten noch keine Kronkorken-Währung
            s.Kronkorken = data.Version < 2 ? s.Caps.Values.Sum() : data.Kronkorken;
            if (data.Version < 3)
            {
                // Uralter Skilltree: einfach neu anfangen
                s.Skills = new();
            }
            else
            {
                s.Skills = (data.SkillLevels ?? new())
                    .Where(kv => SkillDefs.Get(kv.Key) is { } d && kv.Value > 0)
                    .ToDictionary(kv => kv.Key, kv => Math.Min(kv.Value, SkillDefs.Get(kv.Key).MaxLevel));
            }
            // Vor Version 4 gab es Kronkorken von Anfang an: wer schon welche hat, bekommt das Sammelalbum
            if (data.Version < 5) MigrateToSkillUnlocks(s, data.Ausbau);
            s.PlayTime = data.PlayTime;
            s.FedByType = data.FedByType ?? new();
            s.CollectedByHand = data.CollectedByHand;
            s.CollectedByCollectors = data.CollectedByCollectors;
            s.EventsHeld = data.EventsHeld;
            s.KaestenAbgegeben = data.KaestenAbgegeben;
            s.BestBon = data.BestBon;
            // Alte Spielstände: Kalle erklärt nichts, was man schon kennt
            if (data.SeenHints == null) KalleHints.MarkCurrent(s);
            return true;
        }
        catch (Exception e)
        {
            GD.PushError($"Spielstand konnte nicht geladen werden: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Ab Version 5 schaltet der Skilltree die Upgrades frei und baut alle Objekte einer Sorte aus.
    /// Was man schon hatte, gibt es als Skill geschenkt.
    /// </summary>
    static void MigrateToSkillUnlocks(GameState s, Dictionary<string, int> ausbau)
    {
        s.Upgrades.Remove("album");
        foreach (var u in UpgradeDefs.All)
            if (u.Skill != null && s.Level(u.Id) > 0) GrantWithParents(s, u.Skill);
        var steps = new Dictionary<string, string[]>
        {
            ["bank"] = new[] { "bank_tisch", "bank_doppel" },
            ["muelleimer"] = new[] { "eimer_gross", "eimer_ring" },
            ["imbiss"] = new[] { "imbiss_theke", "imbiss_kuehl" },
            ["tischtennis"] = new[] { "tt_turnier" },
            ["grill"] = new[] { "grill_rost", "grill_kuehl" },
        };
        foreach (var kv in ausbau ?? new())
        {
            var kind = kv.Key.Split(':')[0];
            if (!steps.TryGetValue(kind, out var ids)) continue;
            for (int i = 0; i < kv.Value && i < ids.Length; i++) GrantWithParents(s, ids[i]);
        }
    }

    static void GrantWithParents(GameState s, string id)
    {
        for (var d = SkillDefs.Get(id); d != null && d.Id != SkillDefs.Root; d = SkillDefs.Get(d.Parent ?? SkillDefs.Root))
            if (!s.HasSkill(d.Id)) s.Skills[d.Id] = 1;
    }

    public static string FullPath => ProjectSettings.GlobalizePath(Path);
}
