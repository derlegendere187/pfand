using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Pfand.Core;

/// <summary>
/// Anzeige-Einstellungen (Vollbild oder Fenster, Fenstergröße, VSync).
/// Liegen getrennt vom Spielstand in user://settings.cfg.
/// </summary>
public static class Settings
{
    const string Path = "user://settings.cfg";

    public static bool Fullscreen = true;
    public static Vector2I WindowSize = new(1280, 800);
    public static bool VSync = true;

    /// <summary>Gängige Auflösungen aller Seitenverhältnisse. Angeboten wird, was auf den Bildschirm passt.</summary>
    static readonly (int w, int h)[] Common =
    {
        (1024, 768), (1280, 720), (1280, 800), (1280, 1024), (1366, 768), (1440, 900), (1600, 900),
        (1600, 1200), (1680, 1050), (1920, 1080), (1920, 1200), (2560, 1080), (2560, 1440), (2560, 1600),
        (2880, 1800), (3440, 1440), (3840, 1080), (3840, 1600), (3840, 2160), (5120, 1440), (5120, 2160),
    };

    public static void Load()
    {
        var cfg = new ConfigFile();
        if (cfg.Load(Path) != Error.Ok) return;
        Fullscreen = (bool)cfg.GetValue("display", "fullscreen", true);
        WindowSize = new Vector2I((int)cfg.GetValue("display", "width", 1280), (int)cfg.GetValue("display", "height", 800));
        VSync = (bool)cfg.GetValue("display", "vsync", true);
    }

    public static void Save()
    {
        var cfg = new ConfigFile();
        cfg.SetValue("display", "fullscreen", Fullscreen);
        cfg.SetValue("display", "width", WindowSize.X);
        cfg.SetValue("display", "height", WindowSize.Y);
        cfg.SetValue("display", "vsync", VSync);
        cfg.Save(Path);
    }

    /// <summary>Größe des Bildschirms, auf dem das Spiel gerade läuft.</summary>
    public static Vector2I ScreenSize => DisplayServer.ScreenGetSize(DisplayServer.WindowGetCurrentScreen());

    /// <summary>Fenstergrößen, die auf den Bildschirm passen, aufsteigend. Die Bildschirmgröße selbst ist immer dabei.</summary>
    public static List<Vector2I> Resolutions()
    {
        var screen = ScreenSize;
        var list = Common.Select(r => new Vector2I(r.w, r.h)).Where(r => r.X <= screen.X && r.Y <= screen.Y).ToList();
        if (!list.Contains(screen)) list.Add(screen);
        return list.OrderBy(r => r.X * r.Y).ThenBy(r => r.X).ToList();
    }

    /// <summary>Seitenverhältnis als Text, z. B. 16:9, 21:9, 16:10.</summary>
    public static string Aspect(Vector2I r)
    {
        double a = (double)r.X / r.Y;
        (double v, string name)[] known = { (4 / 3.0, "4:3"), (5 / 4.0, "5:4"), (3 / 2.0, "3:2"), (16 / 10.0, "16:10"), (16 / 9.0, "16:9"), (64 / 27.0, "21:9"), (43 / 18.0, "21:9"), (32 / 9.0, "32:9"), (12 / 5.0, "21:9") };
        foreach (var k in known) if (System.Math.Abs(a - k.v) < 0.02) return k.name;
        int g = Gcd(r.X, r.Y);
        return $"{r.X / g}:{r.Y / g}";
    }

    static int Gcd(int a, int b) => b == 0 ? a : Gcd(b, a % b);

    public static void Apply()
    {
        DisplayServer.WindowSetVsyncMode(VSync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
        if (Fullscreen)
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
            return;
        }
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        var screen = ScreenSize;
        var size = new Vector2I(System.Math.Min(WindowSize.X, screen.X), System.Math.Min(WindowSize.Y, screen.Y));
        DisplayServer.WindowSetSize(size);
        var origin = DisplayServer.ScreenGetPosition(DisplayServer.WindowGetCurrentScreen());
        DisplayServer.WindowSetPosition(origin + (screen - size) / 2);
    }
}
