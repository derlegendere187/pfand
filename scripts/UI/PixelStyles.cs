using Godot;
using Pfand.Park;

namespace Pfand.UI;

/// <summary>Farben der Holz-Oberfläche.</summary>
public static class Wood
{
    static Color H(string hex) => Color.FromHtml(hex);
    public static readonly Color Bg = H("#24190f");
    public static readonly Color Plank = H("#7a5232");
    public static readonly Color Plank2 = H("#70492c");
    public static readonly Color Edge = H("#2a1a0c");
    public static readonly Color GrainDark = H("#5c3c22");
    public static readonly Color GrainLight = H("#8c6040");
    public static readonly Color Nail = H("#e0b860");
    public static readonly Color Ink = H("#f4e4c0");
    public static readonly Color Muted = H("#d2b484");
    public static readonly Color Accent = H("#f2c45a");
    public static readonly Color Slot = H("#4e321c");
    public static readonly Color SlotEdge = H("#2a1a0c");
    public static readonly Color SlotLow = H("#5e3e24");
    public static readonly Color Shadow = H("#140c06");
    public static readonly Color Btn = H("#5e7e3a");
    public static readonly Color BtnHi = H("#7a9a50");
    public static readonly Color BtnLo = H("#425a26");
    public static readonly Color BtnHover = H("#6a8c42");
    public static readonly Color BtnDown = H("#4e6a30");
    public static readonly Color BtnInk = H("#f4eed8");
    public static readonly Color Off = H("#5a4632");
    public static readonly Color OffHi = H("#6a5440");
    public static readonly Color OffInk = H("#a8916e");
    public static readonly Color Paper = H("#f2ead8");
    public static readonly Color PaperShade = H("#ddd2b8");
    public static readonly Color Print = H("#3a3a44");
    public static readonly Color PrintSoft = H("#7a7a84");
}

/// <summary>Grundform aller Pixel-Kästen: Rand mit abgeschnittenen Ecken, darin Füllung mit heller Ober- und dunkler Unterkante.</summary>
public partial class PixelBox : StyleBox
{
    public const float P = 3;   // ein Kunst-Pixel der Oberfläche
    public Color Fill = Wood.Plank, Border = Wood.Edge, Hi = Colors.Transparent, Lo = Colors.Transparent;
    public int Bevel = 1;       // Höhe der unteren Schattenkante in Pixeln

    public PixelBox() { }

    public PixelBox(Color fill, Color border, Color? hi = null, Color? lo = null, float margin = 8)
    {
        Fill = fill; Border = border; Hi = hi ?? Colors.Transparent; Lo = lo ?? Colors.Transparent;
        SetContentMarginAll(margin);
    }

    protected static void R(Rid ci, float x, float y, float w, float h, Color c)
    {
        if (w <= 0 || h <= 0 || c.A <= 0) return;
        RenderingServer.CanvasItemAddRect(ci, new Rect2(x, y, w, h), c);
    }

    /// <summary>Rechteck mit um einen Pixel abgeschnittenen Ecken.</summary>
    protected static void Cut(Rid ci, Rect2 r, Color c)
    {
        R(ci, r.Position.X + P, r.Position.Y, r.Size.X - 2 * P, r.Size.Y, c);
        R(ci, r.Position.X, r.Position.Y + P, P, r.Size.Y - 2 * P, c);
        R(ci, r.End.X - P, r.Position.Y + P, P, r.Size.Y - 2 * P, c);
    }

    public override void _Draw(Rid ci, Rect2 rect)
    {
        rect = new Rect2(rect.Position.Round(), rect.Size.Round());
        Cut(ci, rect, Border);
        var inner = rect.Grow(-P);
        R(ci, inner.Position.X, inner.Position.Y, inner.Size.X, inner.Size.Y, Fill);
        R(ci, inner.Position.X, inner.Position.Y, inner.Size.X, P, Hi);
        R(ci, inner.Position.X, inner.End.Y - P * Bevel, inner.Size.X, P * Bevel, Lo);
    }
}

/// <summary>Holzbrett: waagerechte Bretter mit Maserung, Nagelpunkte in den Ecken.</summary>
public partial class WoodBox : PixelBox
{
    public bool Nails = true;

    public WoodBox() { SetContentMarginAll(12); }

    public WoodBox(float margin, bool nails = true) { SetContentMarginAll(margin); Nails = nails; }

    public override void _Draw(Rid ci, Rect2 rect)
    {
        rect = new Rect2(rect.Position.Round(), rect.Size.Round());
        // Schlagschatten unten rechts
        Cut(ci, new Rect2(rect.Position + new Vector2(P, P), rect.Size), Wood.Shadow);
        Cut(ci, rect, Wood.Edge);
        var r = rect.Grow(-P);
        R(ci, r.Position.X, r.Position.Y, r.Size.X, r.Size.Y, Wood.Plank);
        R(ci, r.Position.X, r.Position.Y, r.Size.X, P, Wood.GrainLight);

        // Bretterfugen alle 7 Pixel
        float plank = 7 * P;
        for (float y = r.Position.Y + plank; y < r.End.Y - 2 * P; y += plank)
        {
            R(ci, r.Position.X, y, r.Size.X, P, Wood.GrainDark);
            R(ci, r.Position.X, y + P, r.Size.X, P, Wood.GrainLight);
        }

        // Maserung: kurze Striche, fest aus der Position abgeleitet (flackert nicht)
        uint seed = (uint)(rect.Position.X * 31 + rect.Position.Y * 17 + rect.Size.X * 7 + rect.Size.Y);
        int cols = (int)(r.Size.X / P), rows = (int)(r.Size.Y / P);
        int n = cols * rows / 45;
        for (int i = 0; i < n; i++)
        {
            seed = seed * 1664525 + 1013904223;
            int cx = (int)(seed % (uint)System.Math.Max(1, cols - 6)) + 1;
            seed = seed * 1664525 + 1013904223;
            int cy = (int)(seed % (uint)System.Math.Max(1, rows - 2)) + 1;
            seed = seed * 1664525 + 1013904223;
            int len = 2 + (int)(seed % 5);
            var c = (seed >> 8) % 2 == 0 ? Wood.Plank2 : Wood.GrainLight;
            R(ci, r.Position.X + cx * P, r.Position.Y + cy * P, len * P, P, c);
        }

        if (Nails && r.Size.X > 8 * P && r.Size.Y > 8 * P)
        {
            R(ci, r.Position.X + P, r.Position.Y + P, P, P, Wood.Nail);
            R(ci, r.End.X - 2 * P, r.Position.Y + P, P, P, Wood.Nail);
            R(ci, r.Position.X + P, r.End.Y - 2 * P, P, P, Wood.Nail);
            R(ci, r.End.X - 2 * P, r.End.Y - 2 * P, P, P, Wood.Nail);
        }
    }
}

/// <summary>Vertiefung im Holz (Taschenplätze, Upgrade-Kacheln).</summary>
public partial class SlotBox : PixelBox
{
    public SlotBox() { SetContentMarginAll(6); }
    public SlotBox(float margin) { SetContentMarginAll(margin); }

    public override void _Draw(Rid ci, Rect2 rect)
    {
        rect = new Rect2(rect.Position.Round(), rect.Size.Round());
        R(ci, rect.Position.X, rect.Position.Y, rect.Size.X, rect.Size.Y, Wood.SlotEdge);
        var r = rect.Grow(-P);
        R(ci, r.Position.X, r.Position.Y, r.Size.X, r.Size.Y, Wood.Slot);
        R(ci, r.Position.X, r.End.Y - P, r.Size.X, P, Wood.SlotLow);
    }
}

/// <summary>Kassenbon: Thermopapier mit gezackter Abrisskante oben und unten.</summary>
public partial class ReceiptBox : PixelBox
{
    public ReceiptBox() { SetContentMarginAll(10); ContentMarginTop = ContentMarginBottom = 16; }

    public override void _Draw(Rid ci, Rect2 rect)
    {
        rect = new Rect2(rect.Position.Round(), rect.Size.Round());
        float z = 2 * P;   // Zackenhöhe
        var body = new Rect2(rect.Position.X, rect.Position.Y + z, rect.Size.X, rect.Size.Y - 2 * z);
        // Schatten rechts und unten
        R(ci, body.Position.X + P, body.Position.Y + P, body.Size.X, body.Size.Y, new Color(0, 0, 0, 0.35f));
        R(ci, body.Position.X, body.Position.Y, body.Size.X, body.Size.Y, Wood.Paper);
        R(ci, body.End.X - P, body.Position.Y, P, body.Size.Y, Wood.PaperShade);
        // Zacken: Dreiecke aus Pixelstufen
        float step = 2 * P;
        for (float x = rect.Position.X; x < rect.End.X - 0.5f; x += step)
        {
            float w = Mathf.Min(step, rect.End.X - x);
            R(ci, x, body.Position.Y - P, w, P, Wood.Paper);
            R(ci, x + P * 0.5f, body.Position.Y - 2 * P, Mathf.Max(0, w - P), P, Wood.Paper);
            R(ci, x, body.End.Y, w, P, Wood.Paper);
            R(ci, x + P * 0.5f, body.End.Y + P, Mathf.Max(0, w - P), P, Wood.Paper);
        }
    }
}

/// <summary>Gestrichelte Linie wie auf einem Kassenbon.</summary>
public partial class DashLine : Control
{
    public Color Color = Wood.PrintSoft;
    public DashLine() { CustomMinimumSize = new Vector2(0, 8); MouseFilter = MouseFilterEnum.Ignore; }

    public override void _Draw()
    {
        float y = Mathf.Round(Size.Y / 2 - 1);
        for (float x = 0; x < Size.X - 3; x += 9) DrawRect(new Rect2(x, y, 5, 2), Color);
    }
}
