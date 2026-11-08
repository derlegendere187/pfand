using Godot;
using Pfand.Core;

namespace Pfand.Park;

/// <summary>
/// Pixelkunst per Code. Jede Methode zeichnet ein kleines Raster aus Rechtecken,
/// skaliert mit Pixelgröße p. Später durch echte Sprites ersetzbar.
/// </summary>
public static class Sprites
{
    static void R(CanvasItem ci, Vector2 o, float p, float x, float y, float w, float h, Color c)
        => ci.DrawRect(new Rect2(o.X + x * p, o.Y + y * p, w * p, h * p), c);

    /// <summary>Person von oben (8×8), zentriert auf pos.</summary>
    public static void Person(CanvasItem ci, Vector2 pos, Color shirt, Color hair, float p = 3)
    {
        var o = pos - new Vector2(4 * p, 4 * p);
        R(ci, o, p, 1, 7, 6, 1, new Color(0, 0, 0, 0.15f)); // Schatten
        R(ci, o, p, 1, 3, 6, 4, shirt);
        R(ci, o, p, 0, 4, 1, 2, Palette.Skin);
        R(ci, o, p, 7, 4, 1, 2, Palette.Skin);
        R(ci, o, p, 2, 0, 4, 5, hair);
        R(ci, o, p, 3, 5, 2, 1, Palette.Skin);
    }

    /// <summary>Flasche, zentriert auf pos, gedreht um rot (Radiant).</summary>
    public static void Bottle(CanvasItem ci, Vector2 pos, BottleType t, float rot, int colorIdx, float scale = 1f)
    {
        float p = 3 * scale;
        ci.DrawSetTransform(pos, rot, Vector2.One);
        switch (t)
        {
            case BottleType.Bier:
                BottleShape(ci, p, Color.FromHtml("#8a5a2e"), Color.FromHtml("#c8553d"), true);
                break;
            case BottleType.Wasser:
                BottleShape(ci, p, Color.FromHtml("#4f8a4a"), Color.FromHtml("#6fa8d6"), true);
                break;
            case BottleType.Plastik:
                BottleShape(ci, p, Color.FromHtml("#a9d4ee"), Color.FromHtml("#3f7fb5"), false);
                break;
            default:
            {
                var o = new Vector2(-1.5f * p, -2 * p);
                var c = Palette.Cans[colorIdx % Palette.Cans.Length];
                R(ci, o, p, 0, 0, 3, 4, c);
                R(ci, o, p, 0, 0, 3, 1, Color.FromHtml("#d6ccb8"));
                R(ci, o, p, 0, 2, 3, 1, Palette.PanelLight);
                break;
            }
        }
        ci.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    static void BottleShape(CanvasItem ci, float p, Color body, Color cap, bool label)
    {
        var o = new Vector2(-1.5f * p, -4 * p);
        R(ci, o, p, 1, 0, 1, 1, cap);
        R(ci, o, p, 1, 1, 1, 2, body);
        R(ci, o, p, 0, 3, 3, 5, body);
        if (label) R(ci, o, p, 0, 5, 3, 1, Palette.PanelLight);
    }

    /// <summary>Picknickdecke, die sich ausbreitet: open = 0 (Rolle) … 1 (ganz offen).</summary>
    public static void BlanketOpening(CanvasItem ci, Vector2 pos, Color c, float open)
    {
        if (open <= 0) return;
        // in Pixelschritten aufklappen, damit es zum Look passt
        float sx = Mathf.Max(0.2f, Mathf.Snapped(open, 0.2f));
        ci.DrawSetTransform(pos, 0, new Vector2(sx, Mathf.Max(0.4f, sx)));
        Blanket(ci, Vector2.Zero, c);
        ci.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary>Zusammengerollte Decke unterm Arm (5×2).</summary>
    public static void RolledBlanket(CanvasItem ci, Vector2 pos, Color c, float p = 3)
    {
        var o = pos - new Vector2(2.5f * p, p);
        R(ci, o, p, 0, 0, 5, 2, c);
        R(ci, o, p, 1, 0, 1, 2, Palette.PanelLight);
        R(ci, o, p, 3, 0, 1, 2, Palette.PanelLight);
    }

    /// <summary>Imbissbude von oben (28×20, Pixel 5) in rect.</summary>
    public static void Imbiss(CanvasItem ci, Rect2 rect)
    {
        float p = 5;
        var o = rect.Position;
        R(ci, o, p, 0, 0, 28, 12, Palette.PanelLight);
        for (int x = 0; x < 28; x += 4) R(ci, o, p, x, 0, 2, 12, Color.FromHtml("#d9735a"));
        R(ci, o, p, 0, 12, 28, 2, Color.FromHtml("#a8503c"));
        R(ci, o, p, 1, 14, 26, 6, Color.FromHtml("#8a5a34"));
        R(ci, o, p, 2, 15, 24, 2, Color.FromHtml("#d9b88a"));
        R(ci, o, p, 4, 15, 2, 2, Palette.LampLight);
        R(ci, o, p, 10, 15, 2, 2, Color.FromHtml("#e08a4a"));
        R(ci, o, p, 18, 15, 3, 2, Palette.Leergut);
    }

    /// <summary>Picknickdecke (15×11), zentriert.</summary>
    public static void Blanket(CanvasItem ci, Vector2 pos, Color c, float p = 4)
    {
        var o = pos - new Vector2(7.5f * p, 5.5f * p);
        R(ci, o, p, 0, 0, 15, 11, c);
        for (int y = 0; y < 11; y += 3)
        for (int x = (y / 3) % 2 * 3; x < 15; x += 6)
            R(ci, o, p, x, y, 3, Mathf.Min(3, 11 - y), Palette.PanelLight);
    }

    /// <summary>Bank von oben (16×5), zentriert.</summary>
    public static void Bench(CanvasItem ci, Vector2 pos, float p = 4)
    {
        var o = pos - new Vector2(8 * p, 2.5f * p);
        R(ci, o, p, 0, 0, 16, 5, Palette.Wood);
        R(ci, o, p, 0, 1, 16, 1, Palette.WoodDark);
        R(ci, o, p, 0, 3, 16, 1, Palette.WoodDark);
        R(ci, o, p, 0, 0, 1, 5, Palette.InkSoft);
        R(ci, o, p, 15, 0, 1, 5, Palette.InkSoft);
    }

    /// <summary>Mülleimer von oben (6×6), zentriert. fill = 0..1</summary>
    public static void Bin(CanvasItem ci, Vector2 pos, float fill, float p = 4)
    {
        var o = pos - new Vector2(3 * p, 3 * p);
        R(ci, o, p, 1, 0, 4, 6, Palette.BinBody);
        R(ci, o, p, 0, 1, 6, 4, Palette.BinBody);
        R(ci, o, p, 1, 1, 4, 4, Palette.BinInside);
        if (fill > 0) R(ci, o, p, 1, 1, 2, 2, Color.FromHtml("#8a5a2e"));
        if (fill > 0.34f) R(ci, o, p, 3, 2, 2, 2, Palette.Cans[1]);
        if (fill > 0.67f) R(ci, o, p, 2, 3, 2, 2, Color.FromHtml("#4f8a4a"));
    }

    /// <summary>Laterne von oben (5×5), zentriert.</summary>
    public static void Lamp(CanvasItem ci, Vector2 pos, float p = 4)
    {
        var o = pos - new Vector2(2.5f * p, 2.5f * p);
        R(ci, o, p, 1, 0, 3, 5, Palette.LampPost);
        R(ci, o, p, 0, 1, 5, 3, Palette.LampPost);
        R(ci, o, p, 1, 1, 3, 3, Palette.LampLight);
        R(ci, o, p, 2, 2, 1, 1, Palette.PanelLight);
    }

    /// <summary>Lichtkegel als gestufter Pixelkreis.</summary>
    public static void Glow(CanvasItem ci, Vector2 pos, float strength, float p = 12)
    {
        var c = new Color(Palette.Glow, 0.22f * strength);
        var o = pos - new Vector2(6 * p, 6 * p);
        R(ci, o, p, 4, 0, 4, 12, c); R(ci, o, p, 2, 1, 8, 10, c); R(ci, o, p, 1, 2, 10, 8, c); R(ci, o, p, 0, 4, 12, 4, c);
        R(ci, o, p, 4, 2, 4, 8, c); R(ci, o, p, 3, 3, 6, 6, c); R(ci, o, p, 2, 4, 8, 4, c);
    }

    /// <summary>Deckel mit Motiv (12×12-Raster). found=false zeichnet eine Silhouette mit Fragezeichen.</summary>
    public static void Cap(CanvasItem ci, Vector2 topLeft, float p, CapData.Motif m, bool found)
    {
        var o = topLeft;
        var ring = found ? Color.FromHtml("#c9ccd6") : Color.FromHtml("#d8ccb0");
        var dark = found ? Color.FromHtml("#8a8fa3") : Color.FromHtml("#d8ccb0");
        var face = found ? Color.FromHtml(m.Face) : Color.FromHtml("#d8ccb0");
        R(ci, o, p, 4, 0, 4, 1, ring); R(ci, o, p, 2, 1, 8, 1, ring); R(ci, o, p, 1, 2, 10, 2, ring);
        R(ci, o, p, 0, 4, 12, 4, ring); R(ci, o, p, 1, 8, 10, 2, ring); R(ci, o, p, 2, 10, 8, 1, ring); R(ci, o, p, 4, 11, 4, 1, ring);
        R(ci, o, p, 0, 5, 1, 2, dark); R(ci, o, p, 11, 5, 1, 2, dark); R(ci, o, p, 5, 0, 2, 1, dark); R(ci, o, p, 5, 11, 2, 1, dark);
        R(ci, o, p, 3, 2, 6, 1, face); R(ci, o, p, 2, 3, 8, 6, face); R(ci, o, p, 3, 9, 6, 1, face);
        if (found)
        {
            foreach (var px in m.Pixels) R(ci, o, p, px.X, px.Y, px.W, px.H, Color.FromHtml(px.Col));
        }
        else
        {
            var q = Palette.Muted;
            R(ci, o, p, 4, 3, 4, 1, q); R(ci, o, p, 7, 4, 1, 2, q); R(ci, o, p, 5, 6, 2, 1, q); R(ci, o, p, 5, 8, 2, 1, q);
        }
    }
}
