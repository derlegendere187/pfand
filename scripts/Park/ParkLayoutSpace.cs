using System.Collections.Generic;
using Godot;
using Pfand.Core;

namespace Pfand.Park;

/// <summary>
/// Belegte Flächen im Park: wo Objekte stehen, damit nichts übereinander landet
/// (Decken, Kästen, Oktoberfest-Aufbauten). Dazu ein Prüfmodus für feste Plätze.
/// </summary>
public static partial class ParkLayout
{
    /// <summary>Fläche eines Mülleimers je nach Ausbau (normal, groß, Doppeltonne).</summary>
    public static Rect2 BinRect(Vector2 bin, int level)
    {
        float q = level >= 1 ? 2.5f : 2f;
        var s = level >= 2 ? new Vector2(25, 15) * q : new Vector2(13, 14) * q;
        return new Rect2(bin - s / 2, s + new Vector2(q, 2 * q));
    }

    /// <summary>Fläche einer Laterne (Kopf oben, Fuß knapp unter dem Punkt).</summary>
    public static Rect2 LampRect(Vector2 lamp) => new(lamp - new Vector2(9, 24), new Vector2(18, 40));

    public static Rect2 TableRect(Vector2 t) => new Rect2(t - TableSize / 2, TableSize).Grow(6);

    public static Rect2 GrillRect(Vector2 size) => new Rect2(Grill(size) - new Vector2(22, 22), new Vector2(44, 46)).Merge(KastenRect(size));

    public static Rect2 DepotRect(Vector2 size) => new(Depot(size) - new Vector2(18, 16), new Vector2(38, 36));

    /// <summary>Alle fest stehenden Objekte mit Namen (für Prüfung und freie Plätze).</summary>
    public static List<(string name, Rect2 rect)> Objects(Vector2 size, GameState gs)
    {
        var list = new List<(string, Rect2)>();
        var bins = Bins(size, gs);
        for (int i = 0; i < bins.Count; i++) list.Add(($"Eimer {i + 1}", BinRect(bins[i], gs.AusbauLevel("muelleimer", i))));
        var benches = Benches(size, gs);
        for (int i = 0; i < benches.Count; i++) list.Add(($"Bank {i + 1}", BenchRect(benches[i], i, gs.AusbauLevel("bank", i))));
        var lamps = Lamps(size, gs);
        for (int i = 0; i < lamps.Count; i++) list.Add(($"Laterne {i + 1}", LampRect(lamps[i])));
        var tables = Tables(size, gs);
        for (int i = 0; i < tables.Count; i++) list.Add(($"Tischtennis {i + 1}", TableRect(tables[i])));
        if (gs.Level("imbiss") > 0) list.Add(("Imbiss", ImbissRect(size).Grow(2)));
        if (gs.Level("grill") > 0) list.Add(("Grill", GrillRect(size)));
        if (gs.Level("sammler") > 0) list.Add(("Depot", DepotRect(size)));
        list.Add((EventArea, EventRect(size)));
        return list;
    }

    const string EventArea = "Eventfläche";

    /// <summary>Platz für Event-Aufbauten (Bühne, Zelt, Bierball-Feld) oben rechts, bleibt frei.</summary>
    public static Rect2 EventRect(Vector2 size) => new(EventCenter(size) - new Vector2(112, 128), new Vector2(228, 228));

    // ---- Oktoberfest ----
    public static readonly Vector2[] WiesnTables = { Px(490, 158), Px(823, 150), Px(823, 262), Px(261, 635), Px(300, 52) };
    public static readonly (Vector2 pos, string sign)[] WiesnBuden =
    {
        (Px(61, 55), "brezel"), (Px(822, 60), "herz"), (Px(40, 472), "brezel"),
        (Px(71, 170), "herz"), (Px(460, 610), "brezel"), (Px(61, 649), "herz"),
    };
    public static readonly Vector2 KarussellAt = Px(790, 635);
    public static readonly Vector2 MaibaumAt = Px(830, 500);

    public static Vector2 At(Vector2 frac, Vector2 size) => (frac * size).Round();

    public static Rect2 BiertischRect(Vector2 c) => new(c - new Vector2(42, 34), new Vector2(84, 66));
    public static Rect2 BudeRect(Vector2 c) => new(c - new Vector2(20, 17), new Vector2(42, 38));
    public static Rect2 KarussellRect(Vector2 c) => new(c - new Vector2(58, 44), new Vector2(116, 92));
    public static Rect2 MaibaumRect(Vector2 c) => new(c - new Vector2(18, 134), new Vector2(42, 138));

    /// <summary>Festzelt mit Fahnen und den langen Tischen davor.</summary>
    public static Rect2 FestzeltRect(Vector2 size)
    {
        var c = EventCenter(size);
        return new Rect2(c + new Vector2(-104, -128), new Vector2(220, 228));
    }

    public static List<(string name, Rect2 rect)> WiesnObjects(Vector2 size)
    {
        var list = new List<(string, Rect2)>();
        for (int i = 0; i < WiesnTables.Length; i++) list.Add(($"Biertisch {i + 1}", BiertischRect(At(WiesnTables[i], size))));
        for (int i = 0; i < WiesnBuden.Length; i++) list.Add(($"Bude {i + 1}", BudeRect(At(WiesnBuden[i].pos, size))));
        list.Add(("Karussell", KarussellRect(At(KarussellAt, size))));
        list.Add(("Maibaum", MaibaumRect(At(MaibaumAt, size))));
        list.Add(("Festzelt", FestzeltRect(size)));
        return list;
    }

    /// <summary>Ob eine Fläche frei ist: im Park, nicht auf Wegen, nicht auf Objekten.</summary>
    public static bool IsFree(Rect2 r, Vector2 size, List<Rect2> paths, List<(string name, Rect2 rect)> objects, float margin = 4)
    {
        if (r.Position.X < margin || r.Position.Y < margin || r.End.X > size.X - margin || r.End.Y > size.Y - margin) return false;
        foreach (var p in paths) if (p.Grow(margin).Intersects(r)) return false;
        foreach (var o in objects) if (o.rect.Grow(margin).Intersects(r)) return false;
        return true;
    }

    /// <summary>Prüfmodus: alle Überschneidungen fester Plätze untereinander, mit Wegen und mit dem Parkrand.</summary>
    public static List<string> Check(Vector2 size, GameState gs, bool wiesn)
    {
        var problems = new List<string>();
        var paths = Paths(size, gs);
        var objs = Objects(size, gs);
        if (wiesn)
        {
            objs.RemoveAll(o => o.name == EventArea);
            objs.AddRange(WiesnObjects(size));
        }
        var bounds = new Rect2(Vector2.Zero, size);
        for (int i = 0; i < objs.Count; i++)
        {
            var a = objs[i];
            if (!bounds.Encloses(a.rect)) problems.Add($"{a.name} ragt aus dem Park {a.rect}");
            for (int k = 0; k < paths.Count; k++)
                if (paths[k].Grow(4).Intersects(a.rect)) problems.Add($"{a.name} liegt auf Weg {k + 1}");
            for (int j = i + 1; j < objs.Count; j++)
                if (a.rect.Grow(4).Intersects(objs[j].rect)) problems.Add($"{a.name} überschneidet {objs[j].name}");
        }
        return problems;
    }
}
