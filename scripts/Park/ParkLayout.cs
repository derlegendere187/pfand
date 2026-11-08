using System.Collections.Generic;
using Godot;
using Pfand.Core;

namespace Pfand.Park;

/// <summary>
/// Feste Plätze für gekaufte Park-Objekte, als Anteile der Spielfeldgröße.
/// Das n-te gekaufte Objekt landet auf dem n-ten Platz.
/// </summary>
public static class ParkLayout
{
    public const float PathWidth = 40;

    // (x0, y0, x1, y1) – waagerecht wenn y0 == y1, sonst senkrecht
    static readonly (float x0, float y0, float x1, float y1)[] PathSegments =
    {
        (0f, 0.47f, 1f, 0.47f),
        (0.47f, 0f, 0.47f, 1f),
        (0f, 0.18f, 0.47f, 0.18f),
        (0.78f, 0.47f, 0.78f, 1f),
        (0.47f, 0.80f, 1f, 0.80f),
        (0.20f, 0.47f, 0.20f, 1f),
    };

    static readonly Vector2[] BinSlots = { new(0.43f, 0.40f), new(0.53f, 0.55f), new(0.74f, 0.40f), new(0.25f, 0.55f) };
    static readonly Vector2[] BenchSlots = { new(0.25f, 0.40f), new(0.65f, 0.40f), new(0.33f, 0.555f), new(0.64f, 0.555f), new(0.25f, 0.11f) };
    static readonly Vector2[] LampSlots = { new(0.37f, 0.40f), new(0.57f, 0.555f), new(0.87f, 0.40f) };

    public static List<Rect2> Paths(Vector2 size, GameState gs)
    {
        var list = new List<Rect2>();
        int n = Mathf.Min(gs.Level("weg"), PathSegments.Length);
        for (int i = 0; i < n; i++)
        {
            var s = PathSegments[i];
            if (s.y0 == s.y1)
                list.Add(new Rect2(s.x0 * size.X, s.y0 * size.Y - PathWidth / 2, (s.x1 - s.x0) * size.X, PathWidth));
            else
                list.Add(new Rect2(s.x0 * size.X - PathWidth / 2, s.y0 * size.Y, PathWidth, (s.y1 - s.y0) * size.Y));
        }
        return list;
    }

    public static List<Vector2> Bins(Vector2 size, GameState gs) => Points(BinSlots, size, gs.Level("muelleimer"));
    public static List<Vector2> Benches(Vector2 size, GameState gs) => Points(BenchSlots, size, gs.Level("bank"));
    public static List<Vector2> Lamps(Vector2 size, GameState gs) => Points(LampSlots, size, gs.Level("laterne"));

    static List<Vector2> Points(Vector2[] slots, Vector2 size, int n)
    {
        var list = new List<Vector2>();
        for (int i = 0; i < Mathf.Min(n, slots.Length); i++)
            list.Add(new Vector2(Mathf.Round(slots[i].X * size.X), Mathf.Round(slots[i].Y * size.Y)));
        return list;
    }
}
