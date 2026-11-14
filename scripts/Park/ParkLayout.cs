using System.Collections.Generic;
using Godot;
using Pfand.Core;

namespace Pfand.Park;

/// <summary>
/// Feste Plätze für gekaufte Park-Objekte, als Anteile der Spielfeldgröße.
/// Das n-te gekaufte Objekt landet auf dem n-ten Platz.
/// </summary>
public static partial class ParkLayout
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

    // Die letzten drei Plätze jeder Liste gibt es erst mit dem Skill "Baugenehmigung"
    static readonly Vector2[] BinSlots = { Px(345, 268), Px(468, 380), Px(644, 276), Px(232, 380),
        Px(470, 83), Px(130, 186), Px(740, 504) };
    static readonly Vector2[] BenchSlots = { Px(218, 276), Px(490, 276), Px(310, 383), Px(615, 590), Px(218, 76),
        Px(322, 162), Px(539, 614), Px(96, 383) };
    static readonly Vector2[] LampSlots = { Px(300, 280), Px(520, 376), Px(757, 276),
        Px(357, 76), Px(712, 600), Px(130, 276) };

    /// <summary>Platz in Spielpixeln (Park ist fest 870×690) als Anteil.</summary>
    static Vector2 Px(float x, float y) => new(x / 870f, y / 690f);

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

    /// <summary>Platz der Imbissbude (140×100) zwischen den Wegen unten rechts.</summary>
    public static Rect2 ImbissRect(Vector2 size)
    {
        var c = new Vector2(Mathf.Round(0.625f * size.X), Mathf.Round(0.68f * size.Y));
        return new Rect2(c - new Vector2(70, 50), new Vector2(140, 100));
    }

    /// <summary>Wo die Leute am Imbiss anstehen (unter dem Tresen).</summary>
    public static Vector2 ImbissFront(Vector2 size)
    {
        var r = ImbissRect(size);
        return new Vector2(r.GetCenter().X, r.End.Y + 14);
    }

    static readonly Vector2[] TableSlots = { Px(287, 524), Px(774, 440) };

    /// <summary>Tischtennisplatten (Mittelpunkte). Platte ist 32×56 groß, Spieler stehen oben und unten.</summary>
    public static List<Vector2> Tables(Vector2 size, GameState gs) => Points(TableSlots, size, gs.Level("tischtennis"));

    public static readonly Vector2 TableSize = new(32, 56);

    /// <summary>Grillplatz unten links.</summary>
    public static Vector2 Grill(Vector2 size) => new(Mathf.Round(0.10f * size.X), Mathf.Round(0.76f * size.Y));

    /// <summary>Sitzplätze der Grillrunde relativ zum Grill.</summary>
    public static readonly Vector2[] GrillSeats =
    {
        new(-44, -12), new(-44, 18), new(-14, -44), new(20, -44), new(-12, 46), new(46, -22),
    };

    /// <summary>Bierkasten neben dem Grill (26×32).</summary>
    public static Rect2 KastenRect(Vector2 size) => new(Grill(size) + new Vector2(28, 6), new Vector2(26, 32));

    /// <summary>Abstand der zweiten Bank (gegenüber am Tisch) zur ersten.</summary>
    public const float BenchOppositeY = 40;

    // Richtung, in die Tisch und zweite Bank zeigen (weg vom Weg): -1 = nach oben, +1 = nach unten
    static readonly int[] BenchDirs = { -1, -1, 1, 1, -1, 1, 1, 1 };

    /// <summary>Versatz von Tisch (half = true) bzw. zweiter Bank zur Bank Nummer i.</summary>
    public static Vector2 BenchOpposite(int i, bool half = false)
        => new(0, (i < BenchDirs.Length ? BenchDirs[i] : 1) * BenchOppositeY * (half ? 0.5f : 1));

    /// <summary>Fläche einer Bank samt Ausbau (für Klick und Hervorhebung).</summary>
    public static Rect2 BenchRect(Vector2 bench, int i, int level)
    {
        var r = new Rect2(bench - new Vector2(34, 13), 68, 26);
        if (level >= 1) r = r.Expand(bench + BenchOpposite(i, level < 2) + new Vector2(0, BenchOpposite(i).Y > 0 ? 13 : -13));
        return r;
    }

    /// <summary>Oktoberfest: erster langer Tisch so weit unter der Event-Mitte.</summary>
    public const float OktoberfestTableY = 4;
    public const float OktoberfestTableGap = 42;     // Abstand der langen Tische

    /// <summary>Wo Events stattfinden (oben rechts).</summary>
    public static Vector2 EventCenter(Vector2 size) => new(Mathf.Round(0.76f * size.X), Mathf.Round(0.21f * size.Y));

    /// <summary>Pfandkiste der Sammler am linken Rand.</summary>
    public static Vector2 Depot(Vector2 size) => new(26, Mathf.Round(0.31f * size.Y));

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
