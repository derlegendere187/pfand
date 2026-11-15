using System.Collections.Generic;
using System.Diagnostics;
using Godot;

namespace Pfand.Core;

/// <summary>
/// Leistungsmessung für --bench=SEKUNDEN: misst nach kurzem Aufwärmen Frame-Zeiten und die teuren Teile
/// (Simulation, Park zeichnen, UI) und schreibt das Ergebnis in die Konsole.
/// </summary>
public static class Bench
{
    public static bool On;
    static readonly Dictionary<string, double> _ms = new();
    static readonly List<double> _frames = new();
    static double _warmup = 3, _left;

    public static void Start(double seconds) { On = true; _left = seconds; }

    public static long T0() => On ? Stopwatch.GetTimestamp() : 0;

    public static void Add(string key, long t0)
    {
        if (!On || _warmup > 0) return;
        double ms = (Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency;
        _ms[key] = (_ms.TryGetValue(key, out var v) ? v : 0) + ms;
    }

    public static void Inc(string key)
    {
        if (!On || _warmup > 0) return;
        _ms[key] = (_ms.TryGetValue(key, out var v) ? v : 0) + 1;
    }

    /// <summary>Einmal pro Frame aufrufen. true = fertig, Spiel beenden.</summary>
    public static bool Frame(double delta)
    {
        if (!On) return false;
        if (_warmup > 0) { _warmup -= delta; return false; }
        _frames.Add(delta * 1000);
        _ms["drawcalls"] = (_ms.TryGetValue("drawcalls", out var dc) ? dc : 0) + Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);
        _ms["primitives"] = (_ms.TryGetValue("primitives", out var pr) ? pr : 0) + Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame);
        _ms["objects"] = (_ms.TryGetValue("objects", out var ob) ? ob : 0) + Performance.GetMonitor(Performance.Monitor.RenderTotalObjectsInFrame);
        _ms["process"] = (_ms.TryGetValue("process", out var pc) ? pc : 0) + Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000;
        _left -= delta;
        if (_left > 0) return false;
        _frames.Sort();
        double sum = 0;
        foreach (var f in _frames) sum += f;
        int n = _frames.Count;
        GD.Print($"BENCH frames={n} avg={sum / n:0.00}ms fps={1000 * n / sum:0} p95={_frames[(int)(n * 0.95)]:0.00}ms max={_frames[n - 1]:0.00}ms");
        foreach (var kv in _ms) GD.Print($"BENCH {kv.Key}={kv.Value / n:0.000}ms/frame");
        return true;
    }
}
