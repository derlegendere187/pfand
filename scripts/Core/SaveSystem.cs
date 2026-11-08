using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

namespace Pfand.Core;

public class SaveData
{
    public int Version { get; set; } = 4;
    public double Money { get; set; }
    public double TotalEarned { get; set; }
    public long BottlesFed { get; set; }
    public int PeakVisitors { get; set; }
    public int Kronkorken { get; set; }
    public double TimeOfDay { get; set; }
    public List<int> Bag { get; set; } = new();
    public Dictionary<string, int> Upgrades { get; set; } = new();
    public Dictionary<string, int> Ausbau { get; set; } = new();
    public Dictionary<int, int> Caps { get; set; } = new();
    public List<string> Skills { get; set; } = new();            // nur Version 2
    public Dictionary<string, int> SkillLevels { get; set; } = new();
    public Dictionary<string, double> Cooldowns { get; set; } = new();
    public Dictionary<string, int> EventCount { get; set; } = new();
    public bool TutorialDone { get; set; }
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
            Ausbau = new Dictionary<string, int>(s.Ausbau),
            Caps = new Dictionary<int, int>(s.Caps),
            SkillLevels = new Dictionary<string, int>(s.Skills),
            Cooldowns = new Dictionary<string, double>(s.Cooldowns),
            EventCount = new Dictionary<string, int>(s.EventCount),
            TutorialDone = !s.TutorialActive,
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
            s.Ausbau = data.Ausbau ?? new();
            s.Caps = (data.Caps ?? new()).Where(kv => kv.Key >= 0 && kv.Key < CapData.Count).ToDictionary(kv => kv.Key, kv => kv.Value);
            s.Cooldowns = data.Cooldowns ?? new();
            s.EventCount = data.EventCount ?? new();
            // Alte Spielstände kennen die Einführung noch nicht: wer schon Flaschen eingeworfen hat, braucht sie nicht
            s.Tutorial = data.TutorialDone || data.BottlesFed > 0 ? TutorialStep.Done : TutorialStep.Intro;
            // Alte Spielstände (Version 1) kannten noch keine Kronkorken-Währung
            s.Kronkorken = data.Version < 2 ? s.Caps.Values.Sum() : data.Kronkorken;
            if (data.Version < 3)
            {
                // Neuer Skilltree: alte Skills zurücksetzen, Kronkorken dafür erstatten
                s.Skills = new();
                foreach (var id in data.Skills ?? new())
                    if (SkillDefs.OldCosts.TryGetValue(id, out var cost)) s.Kronkorken += cost;
            }
            else
            {
                s.Skills = (data.SkillLevels ?? new())
                    .Where(kv => SkillDefs.Get(kv.Key) is { } d && kv.Value > 0)
                    .ToDictionary(kv => kv.Key, kv => Math.Min(kv.Value, SkillDefs.Get(kv.Key).MaxLevel));
            }
            // Vor Version 4 gab es Kronkorken von Anfang an: wer schon welche hat, bekommt das Sammelalbum
            if (data.Version < 4 && (s.Kronkorken > 0 || s.Caps.Count > 0 || s.Skills.Count > 0)) s.Upgrades["album"] = 1;
            s.PlayTime = data.PlayTime;
            s.FedByType = data.FedByType ?? new();
            s.CollectedByHand = data.CollectedByHand;
            s.CollectedByCollectors = data.CollectedByCollectors;
            s.EventsHeld = data.EventsHeld;
            s.KaestenAbgegeben = data.KaestenAbgegeben;
            s.BestBon = data.BestBon;
            return true;
        }
        catch (Exception e)
        {
            GD.PushError($"Spielstand konnte nicht geladen werden: {e.Message}");
            return false;
        }
    }

    public static string FullPath => ProjectSettings.GlobalizePath(Path);
}
