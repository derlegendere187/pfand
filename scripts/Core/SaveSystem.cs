using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

namespace Pfand.Core;

public class SaveData
{
    public int Version { get; set; } = 2;
    public double Money { get; set; }
    public double TotalEarned { get; set; }
    public long BottlesFed { get; set; }
    public int PeakVisitors { get; set; }
    public int Kronkorken { get; set; }
    public double TimeOfDay { get; set; }
    public List<int> Bag { get; set; } = new();
    public Dictionary<string, int> Upgrades { get; set; } = new();
    public Dictionary<int, int> Caps { get; set; } = new();
    public List<string> Skills { get; set; } = new();
    public Dictionary<string, double> Cooldowns { get; set; } = new();
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
            Skills = s.Skills.ToList(),
            Cooldowns = new Dictionary<string, double>(s.Cooldowns),
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
            s.Skills = new HashSet<string>(data.Skills ?? new());
            s.Cooldowns = data.Cooldowns ?? new();
            // Alte Spielstände (Version 1) kannten noch keine Kronkorken-Währung
            s.Kronkorken = data.Version < 2 ? s.Caps.Values.Sum() : data.Kronkorken;
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
