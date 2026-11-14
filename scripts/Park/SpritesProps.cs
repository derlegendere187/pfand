using Godot;
using Pfand.Core;
using Pfand.UI;

namespace Pfand.Park;

/// <summary>
/// Parkmöbel, Imbiss, Decken und Event-Requisiten im feinen Pixelstil (Kunstpixel 2 px, dunkler Umriss, drei Töne).
/// </summary>
public static partial class Sprites
{
    static Color Hx(string h) => Color.FromHtml(h);

    static readonly Color Out = Hx("#2a1d14");
    static readonly Color Shade = new(0, 0, 0, 0.2f);
    static readonly Color Cream = Hx("#f4ead2");

    // Holz der Parkmöbel
    static readonly Color WoodHi = Hx("#c8935a"), WoodMid = Hx("#a8743e"), WoodLo = Hx("#7e5230"), WoodGap = Hx("#4a3020");
    static readonly Color Metal = Hx("#3a3a44"), MetalHi = Hx("#62626e");

    // Bayern
    static readonly Color BavBlue = Hx("#4f8fd0"), BavWhite = Hx("#f6f2e8");
    static readonly Color MugBeer = Hx("#e8b040"), MugHi = Hx("#f8d878"), Foam = Hx("#fffaf0"), FoamShade = Hx("#e8e0d0");
    static readonly Color Brezn = Hx("#a8642a"), BreznHi = Hx("#c8843e");

    /// <summary>Kleidung auf der Wiesn: Trachtenhemd, Loden, Dirndl, Leder.</summary>
    public static readonly Color[] Tracht = { Hx("#f4f0e4"), Hx("#3f6a42"), Hx("#a83a3a"), Hx("#4f6fa8"), Hx("#7a5432"), Hx("#f4f0e4"), Hx("#2f4a6a") };

    // ---------------------------------------------------------------- Bänke und Tische

    /// <summary>Parkbank von oben (32×10 feine Pixel): drei Latten, Metallwangen an den Enden.</summary>
    public static void Bench(CanvasItem ci, Vector2 pos, float p = 2)
    {
        var o = (pos - new Vector2(16 * p, 5 * p)).Round();
        R(ci, o, p, 1, 9, 31, 2, Shade);
        R(ci, o, p, 0, 0, 32, 10, Out);
        R(ci, o, p, 1, 1, 30, 8, WoodGap);
        for (int i = 0; i < 3; i++)
        {
            float y = 1 + i * 3;
            R(ci, o, p, 3, y, 26, 1, WoodHi);
            R(ci, o, p, 3, y + 1, 26, 1, WoodMid);
            R(ci, o, p, 7 + i * 5, y + 1, 2, 1, WoodLo); // Maserung
            R(ci, o, p, 19 - i * 3, y, 1, 1, WoodMid);
        }
        // Metallwangen
        foreach (float x in new[] { 1f, 29f })
        {
            R(ci, o, p, x, 1, 2, 8, Metal);
            R(ci, o, p, x, 1, 1, 8, MetalHi);
        }
    }

    /// <summary>Tisch zwischen zwei Bänken (32×8 feine Pixel) mit Teller, Becher und Flasche.</summary>
    public static void PicnicTable(CanvasItem ci, Vector2 pos, float p = 2)
    {
        var o = (pos - new Vector2(16 * p, 4 * p)).Round();
        R(ci, o, p, 1, 7, 31, 2, Shade);
        R(ci, o, p, 0, 0, 32, 8, Out);
        R(ci, o, p, 1, 1, 30, 3, WoodMid);
        R(ci, o, p, 1, 1, 30, 1, WoodHi);
        R(ci, o, p, 1, 4, 30, 1, WoodGap);
        R(ci, o, p, 1, 5, 30, 2, WoodMid);
        R(ci, o, p, 1, 5, 30, 1, WoodHi);
        // Teller mit Wurst
        R(ci, o, p, 6, 2, 3, 1, Cream); R(ci, o, p, 5, 3, 5, 2, Cream); R(ci, o, p, 6, 5, 3, 1, Cream);
        R(ci, o, p, 6, 3, 3, 1, Hx("#b8503a"));
        // Becher
        R(ci, o, p, 14, 2, 2, 2, Foam); R(ci, o, p, 14, 3, 2, 1, FoamShade);
        // liegende Flasche
        R(ci, o, p, 20, 3, 4, 2, Hx("#8a5a2e")); R(ci, o, p, 24, 3.5f, 2, 1, Hx("#8a5a2e")); R(ci, o, p, 20, 3, 4, 1, Hx("#b88450"));
        R(ci, o, p, 26, 3.5f, 1, 1, Hx("#c8553d"));
    }

    // ---------------------------------------------------------------- Mülleimer

    static readonly string[] BinArt =
    {
        "ooooooooooooo",
        "oKKKKKKKKKKKo",
        "oKiiiiiiiiiKo",
        "oKiiiiiiiiiKo",
        "oKKKKKKKKKKKo",
        "ooooooooooooo",
        "oHHLHHLHHLHHo",
        "oSSSSSSSSSSSo",
        "oHHLHHLHHLHHo",
        "oHHLHHLHHLHHo",
        "oHHLHHLHHLHHo",
        "oSSSSSSSSSSSo",
        "oDDLDDLDDLDDo",
        "ooooooooooooo",
    };

    /// <summary>
    /// Eckiger Mülleimer aus Holzlatten, leicht von vorn (13×14 feine Pixel), zentriert.
    /// fill = 0..1, p = 4 normal, 5 groß. doppelt: zwei Tonnen unter einem Deckel, rechts für Pfand.
    /// </summary>
    public static void Bin(CanvasItem ci, Vector2 pos, float fill, float p = 4, bool doppelt = false)
    {
        float q = p / 2;
        if (doppelt) { DoubleBin(ci, pos, fill, q); return; }
        var o = (pos - new Vector2(6.5f * q, 7 * q)).Round();
        R(ci, o, q, 1, 12, 13, 2, Shade);
        Map(ci, o, q, BinArt, ch => ch switch
        {
            'o' => Out,
            'i' => Hx("#1e1e22"),
            'K' => WoodHi,
            'H' => WoodMid,
            'L' => WoodGap,
            'D' => WoodLo,
            'S' => Hx("#4a4a54"),
            _ => null,
        });
        var brown = Hx("#8a5a2e");
        if (fill > 0) { R(ci, o, q, 3, 3, 3, 1, brown); R(ci, o, q, 3, 3, 1, 1, Hx("#c8553d")); }
        if (fill > 0.34f) { R(ci, o, q, 6, 2, 2, 2, Palette.Cans[1]); R(ci, o, q, 6, 2, 2, 1, Hx("#d6ccb8")); }
        if (fill > 0.67f) { R(ci, o, q, 8, 3, 2, 1, Hx("#4f8a4a")); R(ci, o, q, 4, 4, 3, 1, Hx("#a9d4ee")); }
        if (fill >= 1f)
        {
            R(ci, o, q, 8, -1, 1, 3, brown);
            R(ci, o, q, 8, -2, 1, 1, Hx("#c8553d"));
        }
    }

    static readonly string[] DoubleBinArt = Build2();

    static string[] Build2()
    {
        string[] door =
        {
            "SSSSSSSSSSS",
            "KHoooooooHD",
            "KHoiiiiioHD",
            "KHoooooooHD",
            "KHHHHHHHHHD",
            "KHHwwwwwHHD",
            "KHHwwwwwHHD",
            "KHHwwwwwHHD",
            "SSSSSSSSSSS",
            "DDDDDDDDDDD",
        };
        var rows = new System.Collections.Generic.List<string>
        {
            ".ooooooooooooooooooooooo.",
            "oTTTTTTTTTTTTTTTTTTTTTTTo",
            "oKKKKKKKKKKKKKKKKKKKKKKKo",
            "ooooooooooooooooooooooooo",
        };
        foreach (var d in door) rows.Add("o" + d + "o" + d + "o");
        rows.Add("ooooooooooooooooooooooooo");
        return rows.ToArray();
    }

    /// <summary>Doppeltonne aus Holz (25×15 feine Pixel), zentriert: links Restmüll, rechts Pfand.</summary>
    static void DoubleBin(CanvasItem ci, Vector2 pos, float fill, float q)
    {
        var o = (pos - new Vector2(12.5f * q, 7.5f * q)).Round();
        R(ci, o, q, 1, 13, 25, 3, Shade);
        Map(ci, o, q, DoubleBinArt, ch => ch switch
        {
            'o' => Out,
            'i' => Hx("#1e1e22"),
            'T' => WoodHi.Lightened(0.15f),
            'K' => WoodHi,
            'H' => WoodMid,
            'D' => WoodLo,
            'S' => Hx("#4a4a54"),
            'w' => Cream,
            _ => null,
        });
        // Schilder: Müllsack links, Flasche rechts
        float l = 1, r = 13;
        R(ci, o, q, l + 4, 10, 3, 2, Hx("#4a4a54"));
        R(ci, o, q, l + 5, 9, 1, 1, Hx("#4a4a54"));
        R(ci, o, q, r + 5, 9, 1, 1, Palette.Leergut);
        R(ci, o, q, r + 4.5f, 10, 2, 2, Palette.Leergut);
        // Inhalt in den Einwurfschlitzen
        var brown = Hx("#8a5a2e");
        if (fill > 0.2f) R(ci, o, q, l + 3, 6, 2, 1, Hx("#e8e0c8"));
        if (fill > 0) R(ci, o, q, r + 3, 6, 2, 1, brown);
        if (fill > 0.34f) R(ci, o, q, r + 5, 6, 1, 1, Hx("#4f8a4a"));
        if (fill > 0.67f) R(ci, o, q, r + 6, 6, 2, 1, Hx("#a9d4ee"));
        if (fill >= 1f)
        {
            // Flasche schaut oben aus dem Pfandschlitz
            R(ci, o, q, r + 5, 4, 1, 2, brown);
            R(ci, o, q, r + 5, 3, 1, 1, Hx("#c8553d"));
        }
    }

    // ---------------------------------------------------------------- Imbiss

    /// <summary>Imbissbude leicht von vorn (70×50 feine Pixel) in rect: Schild, Markise, Fenster mit Regal, Theke.</summary>
    public static void Imbiss(CanvasItem ci, Rect2 rect)
    {
        const float p = 2;
        var o = rect.Position.Round();
        var red = Hx("#c8553d");

        R(ci, o, p, 3, 48, 68, 3, Shade);
        // Korpus aus Brettern
        R(ci, o, p, 2, 24, 66, 25, Out);
        R(ci, o, p, 3, 25, 64, 23, Hx("#b07a4a"));
        for (int x = 9; x < 67; x += 6) R(ci, o, p, x, 25, 1, 23, Hx("#93603a"));
        R(ci, o, p, 3, 25, 1, 23, Hx("#c8925e"));
        R(ci, o, p, 3, 47, 64, 1, Hx("#7e5230"));

        // Fenster mit Regal
        R(ci, o, p, 6, 27, 58, 12, Out);
        R(ci, o, p, 7, 28, 56, 10, Hx("#3a2a1c"));
        R(ci, o, p, 7, 31, 56, 1, Hx("#6a4a32"));
        for (int i = 0; i < 8; i++)
        {
            float x = 9 + i * 4;
            var col = i % 3 == 0 ? Hx("#8a5a2e") : i % 3 == 1 ? Hx("#4f8a4a") : Palette.Cans[i % Palette.Cans.Length];
            R(ci, o, p, x, 29, 2, 2, col);
            R(ci, o, p, x, 28, 1, 1, col.Lightened(0.25f));
        }
        // Würstchenkette
        R(ci, o, p, 9, 32, 30, 1, Hx("#6a4a32"));
        for (int k = 0; k < 7; k++) R(ci, o, p, 10 + k * 4, 33, 3, 1, Hx("#c0603c"));
        // Tafel mit Preisen
        R(ci, o, p, 46, 29, 14, 8, Out);
        R(ci, o, p, 47, 30, 12, 6, Hx("#2a3a30"));
        R(ci, o, p, 48, 31, 7, 1, Hx("#d8d8c8"));
        R(ci, o, p, 48, 33, 9, 1, Hx("#d8d8c8"));
        R(ci, o, p, 48, 35, 5, 1, Hx("#d8d8c8"));

        // Theke
        R(ci, o, p, 4, 38, 62, 3, Out);
        R(ci, o, p, 5, 38, 60, 1, Hx("#e0c49a"));
        R(ci, o, p, 5, 39, 60, 1, Hx("#c8a478"));
        R(ci, o, p, 12, 36, 2, 2, Hx("#c8302a")); R(ci, o, p, 12, 35, 1, 1, Hx("#c8302a")); // Ketchup
        R(ci, o, p, 15, 36, 2, 2, Hx("#e8c040")); R(ci, o, p, 15, 35, 1, 1, Hx("#e8c040")); // Senf
        R(ci, o, p, 38, 36, 2, 2, Cream); R(ci, o, p, 41, 36, 2, 2, Cream);                 // Becher
        // Pfand-Aufkleber
        R(ci, o, p, 46, 42, 14, 4, Out);
        R(ci, o, p, 47, 43, 12, 2, Palette.Leergut);
        R(ci, o, p, 49, 43.5f, 8, 1, Hx("#cfe8c0"));

        // Markise
        R(ci, o, p, 0, 15, 70, 10, Out);
        for (int x = 1; x < 69; x += 4)
        {
            var c = (x / 4) % 2 == 0 ? red : Cream;
            float w = Mathf.Min(4, 69 - x);
            R(ci, o, p, x, 16, w, 8, c);
            R(ci, o, p, x, 24, w, 1, c);
            if (w >= 4)
            {
                R(ci, o, p, x + 1, 25, 2, 1, c);
                R(ci, o, p, x, 25, 1, 1, Out); R(ci, o, p, x + 3, 25, 1, 1, Out);
                R(ci, o, p, x + 1, 26, 2, 1, Out);
            }
        }
        R(ci, o, p, 1, 16, 68, 1, new Color(1, 1, 1, 0.25f));
        R(ci, o, p, 1, 22, 68, 2, new Color(0, 0, 0, 0.12f));

        // Schild (Text zeichnet ParkView)
        R(ci, o, p, 6, 1, 58, 15, Out);
        R(ci, o, p, 7, 2, 56, 13, Cream);
        R(ci, o, p, 7, 14, 56, 1, Hx("#d8ccb0"));
        R(ci, o, p, 7, 2, 56, 1, Foam);
    }

    // ---------------------------------------------------------------- Picknickdecken

    /// <summary>Anzahl der Deckenmuster.</summary>
    public const int BlanketStyles = 5;

    /// <summary>Farbe eines Deckenpixels: 0 Vichy-Karo, 1 Streifen, 2 Punkte mit Rand, 3 Schottenkaro, 4 Wolldecke mit Zickzack.</summary>
    static Color BlanketCell(Color c, int style, int x, int y, int w, int h)
    {
        switch (style)
        {
            case 1:
            {
                int m = x % 8;
                return m < 3 ? c : m == 3 ? c.Darkened(0.3f) : m == 7 ? c.Lerp(Cream, 0.5f) : Cream;
            }
            case 2:
            {
                bool border = x < 2 || y < 2 || x >= w - 2 || y >= h - 2;
                if (border) return c.Darkened(0.25f);
                if (x == 2 || y == 2 || x == w - 3 || y == h - 3) return Cream;
                bool dot = (y % 4 == 1) && ((x + (y / 4) * 2) % 4 == 1);
                return dot ? Cream : c;
            }
            case 3:
            {
                var bg = c.Darkened(0.35f);
                bool bx = x % 10 < 3, by = y % 10 < 3;
                if (x % 10 == 6 || y % 10 == 6) return Hx("#e8c86a");
                return bx && by ? c.Lightened(0.1f) : bx || by ? c : bg;
            }
            case 4:
            {
                if (y < 2 || y >= h - 2) return c.Darkened(0.3f);
                int zig = (x + y) % 6, zag = (x - y + 60) % 6;
                if (y % 6 >= 2 && y % 6 <= 3 && (zig == 0 || zag == 0)) return Cream;
                return c;
            }
            default:
            {
                bool bx = (x / 3) % 2 == 0, by = (y / 3) % 2 == 0;
                return bx && by ? c.Darkened(0.12f) : bx || by ? Cream.Lerp(c, 0.55f) : Cream;
            }
        }
    }

    /// <summary>Picknickdecke (w×h feine Pixel), zentriert, mit Fransen an den kurzen Seiten.</summary>
    public static void BlanketSized(CanvasItem ci, Vector2 pos, Color c, int style, int w, int h, float p = 2)
    {
        var o = (pos - new Vector2(w / 2f * p, h / 2f * p)).Round();
        R(ci, o, p, 1, 1, w, h, Shade);
        for (int y = 0; y < h; y++)
        {
            int x = 0;
            while (x < w)
            {
                var col = BlanketCell(c, style, x, y, w, h);
                int n = 1;
                while (x + n < w && BlanketCell(c, style, x + n, y, w, h) == col) n++;
                if (y == 0) col = col.Lightened(0.12f);
                else if (y == h - 1) col = col.Darkened(0.18f);
                R(ci, o, p, x, y, n, 1, col);
                x += n;
            }
        }
        var fringe = style == 2 || style == 4 ? c.Darkened(0.3f) : Cream.Darkened(0.06f);
        for (int y = 1; y < h - 1; y += 2)
        {
            R(ci, o, p, -1, y, 1, 1, fringe);
            R(ci, o, p, w, y, 1, 1, fringe.Darkened(0.1f));
        }
    }

    /// <summary>Picknickdecke (30×22 feine Pixel), zentriert.</summary>
    public static void Blanket(CanvasItem ci, Vector2 pos, Color c, int style = 0, float p = 2)
        => BlanketSized(ci, pos, c, style, 30, 22, p);

    /// <summary>Picknickdecke, die sich ausbreitet: open = 0 (Rolle) … 1 (ganz offen).</summary>
    public static void BlanketOpening(CanvasItem ci, Vector2 pos, Color c, int style, float open)
    {
        if (open <= 0) return;
        float sx = Mathf.Max(0.2f, Mathf.Snapped(open, 0.2f));
        ci.DrawSetTransform(pos, 0, new Vector2(sx, Mathf.Max(0.4f, sx)));
        Blanket(ci, Vector2.Zero, c, style);
        ci.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary>Zusammengerollte Decke unterm Arm (11×5 feine Pixel), im Muster der Decke.</summary>
    public static void RolledBlanket(CanvasItem ci, Vector2 pos, Color c, int style = 0, float p = 2)
    {
        var o = (pos - new Vector2(5.5f * p, 2.5f * p)).Round();
        var edge = c.Darkened(0.5f);
        R(ci, o, p, 1, 0, 9, 5, edge);
        R(ci, o, p, 0, 1, 11, 3, edge);
        for (int x = 1; x < 9; x++)
        {
            var mid = BlanketCell(c, style, 4 + x * 2, 6, 30, 22);
            R(ci, o, p, x, 1, 1, 1, mid.Lightened(0.2f));
            R(ci, o, p, x, 2, 1, 1, mid);
            R(ci, o, p, x, 3, 1, 1, mid.Darkened(0.2f));
        }
        R(ci, o, p, 9, 1, 1, 3, BlanketCell(c, style, 10, 10, 30, 22).Lightened(0.15f));
        R(ci, o, p, 9, 2, 1, 1, edge);
    }

    // ---------------------------------------------------------------- Kleinteile

    static readonly string[] BalloonArt =
    {
        "..ooo..",
        ".oBBho.",
        "oBBBhho",
        "oBBBBBo",
        "oBBBBBo",
        ".oBBBo.",
        "..oBo..",
        "...o...",
    };

    static void Balloon(CanvasItem ci, Vector2 pos, Color c)
    {
        const float p = 2;
        var o = (pos - new Vector2(3.5f * p, 4 * p)).Round();
        ci.DrawLine(o + new Vector2(7, 16), o + new Vector2(7 + 2, 34), new Color(Out, 0.7f), 1);
        Map(ci, o, p, BalloonArt, ch => ch switch { 'o' => Out, 'B' => c, 'h' => c.Lightened(0.45f), _ => null });
    }

    /// <summary>Maßkrug (5×6 feine Pixel), obere linke Ecke bei o.</summary>
    static void Mass(CanvasItem ci, Vector2 o, float p = 1.5f)
    {
        o = o.Round();
        R(ci, o, p, 0, 0, 4, 1, Foam);
        R(ci, o, p, 0, 1, 4, 1, FoamShade);
        R(ci, o, p, 0, 2, 4, 3, MugBeer);
        R(ci, o, p, 1, 2, 1, 3, MugHi);
        R(ci, o, p, 4, 2, 1, 3, Out);
        R(ci, o, p, 0, 5, 4, 1, Out);
    }

    static readonly string[] BreznArt =
    {
        ".bb.bb.",
        "b..b..b",
        "b.hbh.b",
        ".b...b.",
        "..bbb..",
    };

    static void BreznAt(CanvasItem ci, Vector2 o, float p = 1.5f)
        => Map(ci, o.Round(), p, BreznArt, ch => ch switch { 'b' => Brezn, 'h' => BreznHi, _ => null });

    static readonly string[] HeartArt =
    {
        ".ooo.ooo.",
        "oHHHoHHHo",
        "oHwwHwwHo",
        "oHHHHHHHo",
        ".oHHHHHo.",
        "..oHHHo..",
        "...oHo...",
        "....o....",
    };

    /// <summary>Lebkuchenherz mit Zuckerguss, an einer Schnur.</summary>
    static void Lebkuchen(CanvasItem ci, Vector2 o, float p = 1.5f)
        => Map(ci, o.Round(), p, HeartArt, ch => ch switch { 'o' => Out, 'H' => Hx("#9a5228"), 'w' => Hx("#f8e8f0"), _ => null });

    /// <summary>Tirolerhut mit Feder auf dem Kopf einer Figur (Person bei pos, Pixel 3).</summary>
    public static void Hat(CanvasItem ci, Vector2 pos)
    {
        var c = pos.Round();
        var felt = Hx("#3f5a3a");
        ci.DrawRect(new Rect2(c + new Vector2(-9, -14), 18, 4), Out);
        ci.DrawRect(new Rect2(c + new Vector2(-8, -13), 16, 2), felt);
        ci.DrawRect(new Rect2(c + new Vector2(-6, -19), 12, 6), Out);
        ci.DrawRect(new Rect2(c + new Vector2(-5, -18), 10, 4), felt);
        ci.DrawRect(new Rect2(c + new Vector2(-5, -15), 10, 1), Hx("#7a5432"));
        ci.DrawRect(new Rect2(c + new Vector2(4, -24), 2, 7), Hx("#e8e0d0"));
        ci.DrawRect(new Rect2(c + new Vector2(5, -25), 2, 3), Hx("#8a8a80"));
    }

    // ---------------------------------------------------------------- Events

    /// <summary>Requisiten für laufende Events.</summary>
    public static void EventProps(CanvasItem ci, EventKind kind, Vector2 c, float time, bool targetDown = false, float wobble = 0)
    {
        c = c.Round();
        switch (kind)
        {
            case EventKind.Geburtstag: Birthday(ci, c, time); break;
            case EventKind.Bierball: Bierball(ci, c, time, targetDown, wobble); break;
            case EventKind.Oktoberfest: Festzelt(ci, c, time); break;
            case EventKind.Feiertag:
            case EventKind.Maennertag:
                break; // eigene Darstellung in ParkView (Wimpelkette, Bollerwagen)
            default: Stage(ci, c, time); break;
        }
    }

    static void Birthday(CanvasItem ci, Vector2 c, float time)
    {
        BlanketSized(ci, c, Hx("#e8a0b8"), 2, 40, 26);
        // Torte
        const float p = 2;
        var o = c - new Vector2(12, 8);
        R(ci, o, p, 0, 8, 12, 2, Out); R(ci, o, p, 1, 8, 10, 1, Foam);
        R(ci, o, p, 1, 2, 10, 7, Out);
        R(ci, o, p, 2, 3, 8, 5, Hx("#f4e0b8"));
        R(ci, o, p, 2, 5, 8, 1, Hx("#c86a52"));
        R(ci, o, p, 2, 3, 8, 2, Hx("#f08aa8"));
        R(ci, o, p, 3, 5, 1, 1, Hx("#f08aa8")); R(ci, o, p, 6, 5, 1, 1, Hx("#f08aa8")); R(ci, o, p, 8, 5, 1, 1, Hx("#f08aa8"));
        R(ci, o, p, 4, 3, 1, 1, Hx("#c8302a")); R(ci, o, p, 7, 3, 1, 1, Hx("#c8302a"));
        var candles = new[] { "#6fa8d6", "#f2c75c", "#8cc472" };
        for (int i = 0; i < 3; i++)
        {
            float x = 3 + i * 3;
            R(ci, o, p, x, -1, 1, 3, Hx(candles[i]));
            bool tall = ((int)(time * 8) + i) % 2 == 0;
            R(ci, o, p, x, tall ? -3 : -2, 1, tall ? 2 : 1, Hx("#f8c040"));
            R(ci, o, p, x, -2, 1, 1, Hx("#fff0a0"));
        }
        // Geschenke
        Gift(ci, c + new Vector2(22, -4), Hx("#6fa8d6"));
        Gift(ci, c + new Vector2(-34, 2), Hx("#8cc472"));
        // Luftballons
        var colors = new[] { "#d9735a", "#6fa8d6", "#f2c75c", "#b496e0" };
        for (int i = 0; i < 4; i++)
            Balloon(ci, c + new Vector2(-54 + i * 36, -52 + Mathf.Round(Mathf.Sin(time * 2 + i) * 3)), Hx(colors[i]));
    }

    static void Gift(CanvasItem ci, Vector2 pos, Color col)
    {
        const float p = 2;
        var o = pos.Round();
        R(ci, o, p, 1, 7, 8, 1, Shade);
        R(ci, o, p, 0, 1, 8, 7, Out);
        R(ci, o, p, 1, 2, 6, 5, col);
        R(ci, o, p, 1, 2, 6, 1, col.Lightened(0.3f));
        R(ci, o, p, 3.5f, 2, 1, 5, Palette.Gold);
        R(ci, o, p, 1, 4, 6, 1, Palette.Gold);
        R(ci, o, p, 2, 0, 2, 1, Palette.Gold); R(ci, o, p, 4, 0, 2, 1, Palette.Gold);
    }

    static void Bierball(CanvasItem ci, Vector2 c, float time, bool targetDown, float wobble)
    {
        // Kreidelinien gestrichelt
        var chalk = new Color(0.96f, 0.94f, 0.88f, 0.9f);
        for (float y = -48; y < 48; y += 8)
        {
            ci.DrawRect(new Rect2(c + new Vector2(-82, y), 4, 5), chalk);
            ci.DrawRect(new Rect2(c + new Vector2(78, y), 4, 5), chalk);
        }
        // Kreidekreis um die Zielflasche
        for (int i = 0; i < 12; i++)
        {
            float a = i / 12f * Mathf.Tau;
            ci.DrawRect(new Rect2((c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 16).Round() - new Vector2(1.5f, 1.5f), 3, 3), chalk);
        }
        // Kästen der beiden Teams
        Kasten(ci, new Rect2(c + new Vector2(-112, -16), 26, 32), 14);
        Kasten(ci, new Rect2(c + new Vector2(88, -16), 26, 32), 9);
        float rot = targetDown ? 1.5f : Mathf.Sin(time * 30) * 0.25f * wobble;
        Bottle(ci, c + (targetDown ? new Vector2(6, 4) : Vector2.Zero), BottleType.Bier, rot, 0, 1.4f, true);
    }

    static void Stage(CanvasItem ci, Vector2 c, float time)
    {
        const float p = 2;
        var o = c + new Vector2(-80, -70);
        var lights = new[] { "#d9735a", "#6fa8d6", "#f2c75c", "#b496e0", "#8cc472", "#e8a0b8" };

        // Bühnenboden
        R(ci, o, p, 1, 22, 80, 2, Shade);
        R(ci, o, p, 0, 5, 80, 17, Out);
        R(ci, o, p, 1, 6, 78, 13, Hx("#7a5838"));
        for (int y = 8; y < 19; y += 3) R(ci, o, p, 1, y, 78, 1, Hx("#5e4228"));
        R(ci, o, p, 1, 6, 78, 1, Hx("#94704a"));
        R(ci, o, p, 1, 19, 78, 2, Hx("#4a3424"));
        R(ci, o, p, 1, 19, 78, 1, Hx("#5e4430"));

        // Boxen
        foreach (float x in new[] { 2f, 68f })
        {
            R(ci, o, p, x, 6, 10, 13, Out);
            R(ci, o, p, x + 1, 7, 8, 11, Hx("#2a2a30"));
            R(ci, o, p, x + 3, 8, 4, 3, Hx("#4a4a54")); R(ci, o, p, x + 4, 9, 2, 1, Hx("#1a1a20"));
            R(ci, o, p, x + 2, 12, 6, 5, Hx("#4a4a54"));
            R(ci, o, p, x + 3, 13, 4, 3, Hx("#1a1a20"));
            bool beat = ((int)(time * 4)) % 2 == 0;
            if (beat) R(ci, o, p, x + 4, 14, 2, 1, Hx("#62626e"));
        }

        // Traverse mit Scheinwerfern
        R(ci, o, p, 0, 0, 80, 3, Out);
        R(ci, o, p, 1, 1, 78, 1, Hx("#9a9aa4"));
        for (int x = 3; x < 78; x += 4) R(ci, o, p, x, 0, 1, 3, Hx("#62626e"));
        for (int i = 0; i < 6; i++)
        {
            float x = 15 + i * 9;
            bool on = ((int)(time * 4) + i) % 2 == 0;
            var col = Hx(lights[i]);
            R(ci, o, p, x, 3, 4, 3, Out);
            R(ci, o, p, x + 1, 4, 2, 1, on ? col : Hx("#3a3a44"));
            if (on)
            {
                var beam = new Color(col, 0.14f);
                R(ci, o, p, x, 6, 4, 4, beam);
                R(ci, o, p, x - 1, 10, 6, 5, beam);
                R(ci, o, p, x - 2, 15, 8, 5, beam);
            }
        }

        // Mikrofonständer und Musiker mit Gitarre
        var mid = (o + new Vector2(80, 26)).Round();
        Person(ci, mid + new Vector2(0, 4 + Mathf.Round(Mathf.Sin(time * 8) * 2)), Hx("#2a1d14"), Hx("#d9735a"));
        ci.DrawRect(new Rect2(mid + new Vector2(-2, 14), 10, 6), Out);
        ci.DrawRect(new Rect2(mid + new Vector2(-1, 15), 8, 4), Hx("#c8762e"));
        ci.DrawRect(new Rect2(mid + new Vector2(8, 16), 10, 2), Out);
        ci.DrawRect(new Rect2(mid + new Vector2(-14, 0), 2, 18), Out);
        ci.DrawRect(new Rect2(mid + new Vector2(-15, -2), 4, 3), Hx("#62626e"));
    }

    // ---------------------------------------------------------------- Oktoberfest

    /// <summary>Weiß-blaue Raute für Fahnen und Zeltdach.</summary>
    static Color Raute(int x, int y)
    {
        int u = Mathf.FloorToInt((x + 2 * y) / 8f), v = Mathf.FloorToInt((x - 2 * y + 400) / 8f);
        return (u + v) % 2 == 0 ? BavBlue : BavWhite;
    }

    static void RautenFlag(CanvasItem ci, Vector2 polePos, float time, float p = 2)
    {
        var o = polePos.Round();
        R(ci, o, p, 0, 0, 1, 24, Out);
        R(ci, o, p, -0.5f, -1, 2, 1, Palette.Gold);
        int wave = ((int)(time * 3)) % 2;
        for (int y = 0; y < 6; y++)
            for (int x = 0; x < 10; x++)
            {
                float dy = (x >= 5 ? wave : 0);
                R(ci, o, p, 1 + x, 1 + y + dy, 1, 1, Raute(x * 2, y * 2));
            }
        R(ci, o, p, 1, 0, 10, 1, Out);
        R(ci, o, p, 1, 7 + wave, 10, 1, new Color(Out, 0.6f));
    }

    static void Festzelt(CanvasItem ci, Vector2 c, float time)
    {
        const float p = 2;
        var o = c + new Vector2(-96, -86);
        // Fahnen an den Ecken
        RautenFlag(ci, o + new Vector2(-6, -40), time);
        RautenFlag(ci, o + new Vector2(194, -40), time + 0.4f);

        R(ci, o, p, 2, 33, 96, 2, Shade);
        // Dach mit Rauten
        R(ci, o, p, 2, 0, 92, 1, Out);
        R(ci, o, p, 1, 1, 94, 1, Out);
        R(ci, o, p, 0, 2, 96, 20, Out);
        for (int y = 1; y < 21; y++)
        {
            int inset = y < 2 ? 2 : 1;
            for (int x = inset; x < 96 - inset; x++)
            {
                if (y == 1 && (x < 3 || x > 92)) continue;
                R(ci, o, p, x, y, 1, 1, Raute(x, y));
            }
        }
        R(ci, o, p, 1, 2, 94, 1, new Color(1, 1, 1, 0.2f));
        // Volant mit Bögen
        for (int x = 0; x < 96; x += 6)
        {
            var col = (x / 6) % 2 == 0 ? BavBlue : BavWhite;
            R(ci, o, p, x, 21, 6, 2, Out);
            R(ci, o, p, x + 1, 21, 4, 1, col);
            R(ci, o, p, x + 2, 22, 2, 1, col);
        }
        // Holzfassade mit Eingang
        R(ci, o, p, 2, 23, 92, 10, Out);
        R(ci, o, p, 3, 23, 90, 9, WoodMid);
        for (int x = 7; x < 93; x += 5) R(ci, o, p, x, 23, 1, 9, WoodLo);
        R(ci, o, p, 3, 23, 90, 1, new Color(0, 0, 0, 0.25f));
        R(ci, o, p, 39, 24, 18, 9, Out);
        R(ci, o, p, 40, 25, 16, 8, Hx("#1e140c"));
        // Hopfengirlande über dem Eingang
        for (int x = 38; x < 58; x += 2) R(ci, o, p, x, 23 + (x / 2) % 2, 2, 1, Hx("#5a8a3a"));
        // Licht aus dem Zelt
        R(ci, o, p, 42, 30, 12, 3, new Color(1f, 0.82f, 0.45f, 0.35f));
        // Fässer neben dem Eingang
        Barrel(ci, o + new Vector2(60, 50));
        Barrel(ci, o + new Vector2(118, 50));

        // Schild
        R(ci, o, p, 24, 10, 48, 10, Out);
        R(ci, o, p, 25, 11, 46, 8, Cream);
        R(ci, o, p, 25, 18, 46, 1, Hx("#d8ccb0"));
        ci.DrawString(PfandTheme.Body, o + new Vector2(50, 36), "O'ZAPFT IS!", HorizontalAlignment.Center, 92, 20, Hx("#2f5a8a"));

        // Musiknoten steigen aus dem Zelt
        for (int n = 0; n < 3; n++)
        {
            float ph = (time * 0.6f + n * 0.33f) % 1f;
            var np = (o + new Vector2(60 + n * 34 + Mathf.Sin(ph * 6 + n) * 6, -6 - ph * 30)).Round();
            ci.DrawRect(new Rect2(np, 4, 4), Out);
            ci.DrawRect(new Rect2(np + new Vector2(3, -8), 1, 8), Out);
            ci.DrawRect(new Rect2(np + new Vector2(3, -8), 4, 2), Out);
        }

        // Lange Biertische vor dem Zelt
        for (int t = 0; t < 3; t++)
        {
            var tr = new Rect2(c + new Vector2(-84, ParkLayout.OktoberfestTableY + t * ParkLayout.OktoberfestTableGap), 168, 12);
            LongTable(ci, tr);
            for (int i = 0; i < 5; i++)
            {
                Mass(ci, tr.Position + new Vector2(12 + i * 32, 1));
                if (i % 2 == 1) BreznAt(ci, tr.Position + new Vector2(24 + i * 32, 3));
            }
        }
    }

    static void LongTable(CanvasItem ci, Rect2 tr)
    {
        ci.DrawRect(new Rect2(tr.Position + new Vector2(2, 2), tr.Size + new Vector2(2, 2)), Shade);
        ci.DrawRect(tr.Grow(2), Out);
        ci.DrawRect(tr, WoodMid);
        ci.DrawRect(new Rect2(tr.Position, tr.Size.X, 2), WoodHi);
        ci.DrawRect(new Rect2(tr.Position.X, tr.Position.Y + tr.Size.Y / 2 - 1, tr.Size.X, 2), WoodGap);
        ci.DrawRect(new Rect2(tr.Position.X, tr.End.Y - 2, tr.Size.X, 2), WoodLo);
    }

    static readonly string[] BarrelArt =
    {
        ".oooooo.",
        "oWWWWWWo",
        "oMMMMMMo",
        "oWhWWWWo",
        "oWhWWWdo",
        "oMMMMMMo",
        "oWhWWWdo",
        "oWWWWWWo",
        ".oooooo.",
    };

    /// <summary>Bierfass, leicht von vorn (8×9 feine Pixel), obere linke Ecke bei o.</summary>
    static void Barrel(CanvasItem ci, Vector2 o, float p = 2)
    {
        R(ci, o.Round(), p, 1, 8, 8, 2, Shade);
        Map(ci, o.Round(), p, BarrelArt, ch => ch switch
        {
            'o' => Out, 'W' => WoodMid, 'h' => WoodHi, 'd' => WoodLo, 'M' => MetalHi, _ => null,
        });
    }

    /// <summary>Maibaum: weiß-blau gestreift, Kranz, Zunftschilder, oben ein grüner Wipfel. Fuß bei pos.</summary>
    public static void Maibaum(CanvasItem ci, Vector2 pos, float time)
    {
        const float p = 2;
        var o = (pos - new Vector2(3 * p, 66 * p)).Round();
        R(ci, o, p, 0, 66, 9, 2, Shade);
        // Stamm mit Spiralstreifen
        R(ci, o, p, 2, 6, 4, 60, Out);
        for (int y = 6; y < 66; y++)
        {
            bool blue = ((y + 2) / 4) % 2 == 0;
            R(ci, o, p, 3, y, 2, 1, blue ? BavBlue : BavWhite);
            R(ci, o, p, 3, y, 1, 1, (blue ? BavBlue : BavWhite).Lightened(0.2f));
        }
        // Wipfel
        R(ci, o, p, 1, 0, 6, 7, Out);
        R(ci, o, p, 2, 1, 4, 5, Hx("#4f8a3a"));
        R(ci, o, p, 2, 1, 2, 2, Hx("#6aa84e"));
        // Kranz mit Bändern
        R(ci, o, p, -1, 9, 10, 3, Out);
        R(ci, o, p, 0, 10, 8, 1, Hx("#4f8a3a"));
        bool sway = ((int)(time * 2)) % 2 == 0;
        R(ci, o, p, 0, 12, 1, sway ? 6 : 5, BavBlue); R(ci, o, p, 7, 12, 1, sway ? 5 : 6, BavWhite);
        R(ci, o, p, 2, 12, 1, 4, Hx("#c8553d"));
        // Zunftschilder an Querstangen
        for (int i = 0; i < 4; i++)
        {
            float y = 20 + i * 11;
            bool left = i % 2 == 0;
            R(ci, o, p, left ? -6 : 6, y, 8, 1, Out);
            float sx = left ? -8 : 10;
            R(ci, o, p, sx, y + 1, 6, 6, Out);
            R(ci, o, p, sx + 1, y + 2, 4, 4, Cream);
            var sym = i switch { 0 => Hx("#8a5a2e"), 1 => Hx("#c8553d"), 2 => BavBlue, _ => Hx("#4f8a3a") };
            R(ci, o, p, sx + 2, y + 3, 2, 2, sym);
        }
    }

    /// <summary>Langer Biertisch mit zwei Bänken und sechs Leuten in Tracht, die ab und zu den Maßkrug heben.</summary>
    public static void Biertisch(CanvasItem ci, Vector2 c, float time, int seed)
    {
        c = c.Round();
        ci.DrawRect(new Rect2(c + new Vector2(-41, -15), 82, 6), Out);
        ci.DrawRect(new Rect2(c + new Vector2(-40, -14), 80, 4), WoodMid);
        ci.DrawRect(new Rect2(c + new Vector2(-41, 11), 82, 6), Out);
        ci.DrawRect(new Rect2(c + new Vector2(-40, 12), 80, 4), WoodMid);
        LongTable(ci, new Rect2(c + new Vector2(-40, -4), 80, 10));
        BreznAt(ci, c + new Vector2(-8, -3));
        for (int i = 0; i < 6; i++)
        {
            bool top = i < 3;
            var pp = c + new Vector2(-26 + (i % 3) * 26, top ? -18 : 20);
            var shirt = Tracht[(i + seed * 2) % Tracht.Length];
            var hair = Palette.Hair[(i + seed) % Palette.Hair.Length];
            Person(ci, pp, shirt, hair);
            if ((i + seed) % 3 == 0) Hat(ci, pp);
            bool cheers = ((int)(time * 1.5f) + i + seed) % 5 == 0;
            var mug = c + new Vector2(-30 + (i % 3) * 26, top ? -4 : 0) + (cheers ? new Vector2(0, top ? -10 : 10) : Vector2.Zero);
            Mass(ci, mug);
        }
    }

    /// <summary>Kettenkarussell von oben: weiß-blaues Dach in der Mitte, Sitze kreisen an Ketten.</summary>
    public static void Kettenkarussell(CanvasItem ci, Vector2 c, float time)
    {
        c = c.Round();
        ci.DrawRect(new Rect2(c + new Vector2(-50, 40), 100, 6), new Color(0, 0, 0, 0.15f));
        var seats = new[] { "#d9735a", "#6fa8d6", "#f2c75c", "#8cc472", "#b496e0", "#e08a4a", "#e8a0b8", "#fffaf0" };
        for (int i = 0; i < seats.Length; i++)
        {
            float a = time * 1.6f + i * Mathf.Tau / seats.Length;
            var sp = (c + new Vector2(Mathf.Cos(a) * 50, Mathf.Sin(a) * 32)).Round();
            ci.DrawLine(c, sp, new Color(Out, 0.8f), 1);
            var col = Hx(seats[i]);
            ci.DrawRect(new Rect2(sp - new Vector2(6, 5), 12, 11), Out);
            ci.DrawRect(new Rect2(sp - new Vector2(5, 4), 10, 9), col);
            ci.DrawRect(new Rect2(sp - new Vector2(5, 4), 10, 2), col.Lightened(0.3f));
            ci.DrawRect(new Rect2(sp - new Vector2(3, 9), 6, 5), Palette.Hair[i % Palette.Hair.Length]);
        }
        // Dach: Pixel-Ellipse mit weiß-blauen Segmenten
        const float p = 2;
        for (int y = -7; y <= 7; y++)
            for (int x = -10; x <= 10; x++)
            {
                if (!InEllipse(x, y)) continue;
                bool edge = !InEllipse(x + 1, y) || !InEllipse(x - 1, y) || !InEllipse(x, y + 1) || !InEllipse(x, y - 1);
                float ang = Mathf.Atan2(y * 1.4f, x) + Mathf.Pi + time * 0.8f;
                int seg = (int)(ang / (Mathf.Tau / 12)) % 2;
                var col = edge ? Out : seg == 0 ? BavBlue : BavWhite;
                if (!edge && y < -3) col = col.Lightened(0.12f);
                ci.DrawRect(new Rect2(c + new Vector2(x * p - 1, y * p - 1), p, p), col);
            }
        ci.DrawRect(new Rect2(c - new Vector2(4, 5), 8, 8), Out);
        ci.DrawRect(new Rect2(c - new Vector2(3, 4), 6, 6), Palette.Gold);
        ci.DrawRect(new Rect2(c - new Vector2(3, 4), 2, 2), Hx("#fff0a0"));
    }

    static bool InEllipse(int x, int y) => (x / 10.5f) * (x / 10.5f) + (y / 7.5f) * (y / 7.5f) <= 1f;

    /// <summary>Oktoberfest-Bude (40×34), zentriert: weiß-blaue Markise, Theke, Lebkuchenherzen oder Brezn.</summary>
    public static void Bude(CanvasItem ci, Vector2 pos, string sign, float time)
    {
        const float p = 2;
        var o = (pos - new Vector2(20, 17)).Round();
        R(ci, o, p, 1, 16, 20, 2, Shade);
        // Korpus
        R(ci, o, p, 0, 5, 20, 12, Out);
        R(ci, o, p, 1, 6, 18, 10, WoodMid);
        for (int x = 4; x < 19; x += 4) R(ci, o, p, x, 11, 1, 5, WoodLo);
        R(ci, o, p, 2, 6, 16, 5, Hx("#2a1d14"));
        // Theke
        R(ci, o, p, 0, 10, 20, 2, Out);
        R(ci, o, p, 1, 10, 18, 1, WoodHi);
        // Ware an der Schnur
        R(ci, o, p, 2, 6, 16, 1, Hx("#6a4a32"));
        for (int i = 0; i < 3; i++)
        {
            var at = o + new Vector2((3 + i * 5) * p, 7 * p);
            if (sign == "herz") Lebkuchen(ci, at + new Vector2(0, ((int)(time * 2) + i) % 2), 0.9f);
            else BreznAt(ci, at + new Vector2(0, 1), 1.2f);
        }
        // Markise
        R(ci, o, p, 0, 0, 20, 6, Out);
        for (int x = 1; x < 19; x += 3)
        {
            var col = (x / 3) % 2 == 0 ? BavBlue : BavWhite;
            float w = Mathf.Min(3, 19 - x);
            R(ci, o, p, x, 1, w, 4, col);
            if (w >= 3) R(ci, o, p, x + 1, 5, 1, 1, col);
        }
        R(ci, o, p, 1, 1, 18, 1, new Color(1, 1, 1, 0.25f));
    }

    /// <summary>Wimpelkette quer über den Park. Bayerisch: weiß-blaue Rautenwimpel.</summary>
    public static void Garland(CanvasItem ci, float width, float time, float y = 10, bool bavarian = false)
    {
        var colors = bavarian ? new[] { "#4f8fd0", "#f6f2e8" } : new[] { "#d9735a", "#f2c75c", "#6fa8d6", "#8cc472", "#b496e0" };
        y = Mathf.Round(y);
        ci.DrawRect(new Rect2(0, y, width, 2), Out);
        int n = (int)(width / 24);
        const float p = 2;
        for (int i = 0; i < n; i++)
        {
            var o = new Vector2(5 + i * 24, y + 2 + (((int)(time * 2) + i) % 2 == 0 ? 0 : 1));
            var c = Hx(colors[i % colors.Length]);
            R(ci, o, p, 0, 0, 7, 1, Out);
            R(ci, o, p, 0, 1, 7, 1, Out); R(ci, o, p, 1, 1, 5, 1, c);
            R(ci, o, p, 1, 2, 5, 1, Out); R(ci, o, p, 2, 2, 3, 1, c);
            R(ci, o, p, 2, 3, 3, 1, Out); R(ci, o, p, 3, 3, 1, 1, c);
            R(ci, o, p, 3, 4, 1, 1, Out);
            if (bavarian && i % 2 == 0) R(ci, o, p, 3, 1, 1, 1, BavBlue.Darkened(0.2f));
            R(ci, o, p, 1, 1, 2, 1, c.Lightened(0.25f));
        }
    }

    /// <summary>Bollerwagen mit Bierkasten. Räder wackeln, solange er fährt.</summary>
    public static void Bollerwagen(CanvasItem ci, Vector2 pos, float time, float p = 2)
    {
        var o = (pos - new Vector2(12 * p, 8 * p)).Round();
        float wob = Mathf.Sin(time * 12) > 0 ? 0 : 1;
        R(ci, o, p, 1, 14, 22, 2, Shade);
        // Deichsel
        R(ci, o, p, 20, 7, 6, 1, Out);
        R(ci, o, p, 25, 5, 1, 4, Out);
        // Wagenkasten
        R(ci, o, p, 0, 4, 20, 8, Out);
        R(ci, o, p, 1, 5, 18, 6, WoodMid);
        R(ci, o, p, 1, 5, 18, 1, WoodHi);
        R(ci, o, p, 1, 8, 18, 1, WoodLo);
        // Kasten
        R(ci, o, p, 3, 1, 12, 7, Out);
        R(ci, o, p, 4, 2, 10, 5, Hx("#b8432f"));
        R(ci, o, p, 4, 2, 10, 1, Hx("#d9735a"));
        for (int i = 0; i < 4; i++)
        {
            R(ci, o, p, 5 + i * 2, 3, 1, 1, Palette.Gold);
            R(ci, o, p, 5 + i * 2, 5, 1, 1, Palette.Gold);
        }
        // Räder
        foreach (float x in new[] { 2f, 14f })
        {
            R(ci, o, p, x, 11 + wob, 5, 4, Out);
            R(ci, o, p, x + 1, 12 + wob, 3, 2, Hx("#4a4a54"));
            R(ci, o, p, x + 2, 12 + wob, 1, 1, Hx("#8a8a92"));
        }
    }

    // ---------------------------------------------------------------- Grill, Kasten, Tischtennis, Depot

    /// <summary>Kugelgrill von oben (21×21 feine Pixel), zentriert. Glüht, wenn jemand grillt.</summary>
    public static void Grill(CanvasItem ci, Vector2 pos, float time, bool active)
    {
        const float p = 2;
        var o = (pos - new Vector2(10.5f * p, 10.5f * p)).Round();
        R(ci, o, p, 2, 19, 19, 3, Shade);
        // Beine
        R(ci, o, p, 2, 17, 2, 4, Out); R(ci, o, p, 17, 17, 2, 4, Out); R(ci, o, p, 9.5f, 19, 2, 3, Out);
        for (int y = 0; y < 21; y++)
            for (int x = 0; x < 21; x++)
            {
                float dx = x - 10, dy = y - 10, d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > 10.4f) continue;
                Color col;
                if (d > 9.3f) col = Out;
                else if (d > 7.8f) col = dy < -2 ? Hx("#5a5a64") : Hx("#3a3a42");
                else
                {
                    // Glut
                    col = Hx("#3a2418");
                    if (active)
                    {
                        int h = (x * 7 + y * 13 + (int)(time * 3)) % 9;
                        col = h == 0 ? Hx("#f2c75c") : h < 3 ? Hx("#e0703a") : h < 5 ? Hx("#a8402a") : Hx("#4a2a1c");
                    }
                    if (y % 2 == 0) col = Hx("#a8a098"); // Rost
                }
                R(ci, o, p, x, y, 1, 1, col);
            }
        if (active)
        {
            foreach (var (x, y, w) in new[] { (5, 7, 6), (9, 11, 6), (6, 14, 5) })
            {
                R(ci, o, p, x, y, w, 2, Out);
                R(ci, o, p, x + 0.5f, y, w - 1, 1, Hx("#c0603c"));
                R(ci, o, p, x + 0.5f, y + 1, w - 1, 0.5f, Hx("#8a3a22"));
            }
        }
    }

    /// <summary>Bierkasten von oben (13×16 feine Pixel): 4×5 Fächer, volle zeigen Flaschenhals mit Kronkorken.</summary>
    public static void Kasten(CanvasItem ci, Rect2 r, int count)
    {
        const float p = 2;
        var o = r.Position.Round();
        R(ci, o, p, 1, 15, 13, 2, Shade);
        R(ci, o, p, 0, 0, 13, 16, Out);
        R(ci, o, p, 0.5f, 0.5f, 12, 15, Hx("#b8432f"));
        R(ci, o, p, 0.5f, 0.5f, 12, 1, Hx("#d9735a"));
        R(ci, o, p, 0.5f, 14.5f, 12, 1, Hx("#8a2e22"));
        // Griffloch
        R(ci, o, p, 4.5f, 0.5f, 4, 0.5f, Out);
        for (int i = 0; i < 20; i++)
        {
            int col = i % 4, row = i / 4;
            float cx = 1 + col * 3, cy = 1.5f + row * 2.7f;
            R(ci, o, p, cx, cy, 2, 2, Hx("#3a1e16"));
            if (i < count)
            {
                R(ci, o, p, cx, cy, 2, 2, Hx("#5a3a1e"));
                R(ci, o, p, cx + 0.5f, cy + 0.5f, 1, 1, Palette.Gold);
            }
        }
    }

    /// <summary>Tischtennisplatte von oben, zentriert: grüne Platte, weiße Linien, Netz quer.</summary>
    public static void Table(CanvasItem ci, Vector2 pos)
    {
        var s = ParkLayout.TableSize;
        var r = new Rect2((pos - s / 2).Round(), s);
        ci.DrawRect(new Rect2(r.Position + new Vector2(4, 4), r.Size + new Vector2(2, 2)), Shade);
        // Füße
        ci.DrawRect(new Rect2(r.Position + new Vector2(2, s.Y), 4, 4), Out);
        ci.DrawRect(new Rect2(r.Position + new Vector2(s.X - 6, s.Y), 4, 4), Out);
        ci.DrawRect(r.Grow(2), Out);
        ci.DrawRect(r, Hx("#2f6a4a"));
        ci.DrawRect(new Rect2(r.Position, s.X, 2), Hx("#3f8a5e"));
        // Linien
        var line = Hx("#f4f0e4");
        ci.DrawRect(new Rect2(r.Position, 2, s.Y), line);
        ci.DrawRect(new Rect2(r.Position + new Vector2(s.X - 2, 0), 2, s.Y), line);
        ci.DrawRect(new Rect2(r.Position, s.X, 2), line);
        ci.DrawRect(new Rect2(r.Position + new Vector2(0, s.Y - 2), s.X, 2), line);
        ci.DrawRect(new Rect2(r.Position + new Vector2(s.X / 2 - 1, 0), 2, s.Y), new Color(line, 0.7f));
        // Netz mit Pfosten
        float ny = r.Position.Y + s.Y / 2 - 2;
        ci.DrawRect(new Rect2(r.Position.X - 5, ny - 1, s.X + 10, 6), Out);
        ci.DrawRect(new Rect2(r.Position.X - 3, ny, s.X + 6, 4), Hx("#e8e4dc"));
        for (float x = r.Position.X - 2; x < r.End.X + 2; x += 3) ci.DrawRect(new Rect2(x, ny + 1, 1, 2), Hx("#a8a49c"));
    }

    public static void Ball(CanvasItem ci, Vector2 pos)
    {
        pos = pos.Round();
        ci.DrawRect(new Rect2(pos - new Vector2(3, 2), 6, 5), new Color(0, 0, 0, 0.2f));
        ci.DrawRect(new Rect2(pos - new Vector2(4, 6), 8, 8), Out);
        ci.DrawRect(new Rect2(pos - new Vector2(3, 5), 6, 6), Hx("#f8a040"));
        ci.DrawRect(new Rect2(pos - new Vector2(3, 5), 2, 2), Hx("#ffe0b0"));
    }

    static readonly string[] DepotArt =
    {
        ".oooooooooooooooo.",
        "oGGGGGGGGGGGGGGGGo",
        "oGgggggggggggggGGo",
        "oooooooooooooooooo",
        "oMhMMMMMMMMMMMMMdo",
        "oMhooooooooooMMMdo",
        "oMhoDDDDDDDDoMMMdo",
        "oMhoDTTTDDDDoMMMdo",
        "oMhooooooooooMMMdo",
        "oMhMMMMMMMMMMMMMdo",
        "oMhMMooooooooMMMdo",
        "oMhMMoSSSSSSoMMMdo",
        "oMhMMooooooooMMMdo",
        "oMhMMMMMMMMMMMMMdo",
        "oMMMMMMMMMMMMMMMdo",
        ".oooooooooooooooo.",
    };

    /// <summary>Kleiner Leergut-Automat als Abgabestelle der Sammler (18×16 feine Pixel), zentriert.</summary>
    public static void Depot(CanvasItem ci, Vector2 pos)
    {
        const float p = 2;
        var o = (pos - new Vector2(9 * p, 8 * p)).Round();
        R(ci, o, p, 1, 15, 18, 2, Shade);
        Map(ci, o, p, DepotArt, ch => ch switch
        {
            'o' => Out,
            'G' => Palette.Leergut,
            'g' => Palette.Leergut.Lightened(0.3f),
            'M' => Hx("#d6ccb8"),
            'h' => Hx("#ece4d4"),
            'd' => Hx("#b5a991"),
            'D' => Hx("#1e2a1e"),
            'T' => Hx("#a8e08f"),
            'S' => Hx("#3a3a40"),
            _ => null,
        });
    }

    // ---------------------------------------------------------------- Söder (nur er in hoher Auflösung)

    static readonly string[] SoederTop =
    {
        ".........oooooooo.........",
        ".......ooGGGGGgGGoo.......",
        "......oGGgggGGGGGGGo......",
        ".....oGGgGGGGGGGGGGGo.....",
        ".....oGGGGGGGGGGGdGGo.....",
        ".....oGGSSSGGGGSSSGGo.....",
        ".....oGSSSSSSSSSSSSGo.....",
        ".....oGSSSSSSSSSSSSGo.....",
        ".....oSbbbbSSSSbbbbSo.....",
        "....oSSSeeSSSSSSeeSSSo....",
        "....oSSSssSSSSSSssSSSo....",
        ".....oSSSSSSnnSSSSSSo.....",
        ".....oSSSSSnnnnSSSSSo.....",
        ".....osSSMMMMMMMMSSso.....",
        ".....osSMMSSSSSSMMSso.....",
        ".....osSMSmmmmmmSMSso.....",
        "......osMSSSSSSSSMso......",
        "......osMCCCCCCCCMso......",
        ".......oCCCCCCCCCCo.......",
        "........ooCCCCCCoo........",
        "..ooooooWWWwTTwWWWoooooo..",
        "..ouUUUUUUlWTTWlUUUUUUuo..",
        "..ouUUUUUUlWTtWlUUUUUUuo..",
        "..ouUUUUUUulTTluUUUUUUuo..",
        "..ouUUUUUUUlTtlUUUUUUUuo..",
        "..ouUUUUUUUlTTlUUUUUUUuo..",
        "..ouUUUUUUUuTtuUUUUUUUuo..",
        "..ouUUUUUUUUuuUUUUUUUUuo..",
        "..ouUUUUUUUUBBUUUUUUUUuo..",
        "..ouUUUUUUUUuuUUUUUUUUuo..",
        "..ouUUuUUUUUuuUUUUUuUUuo..",
        "..ouUUuuUUUUuuUUUUuuUUuo..",
        "..oSSouuuuuuuuuuuuuuoSSo..",
        "..oooouuuuuuuuuuuuuuoooo..",
        ".....oDDDDDDDDDDDDDDo.....",
        ".....oDDDDDDDDDDDDDDo.....",
        ".....oDDDDDDdDDDDDDDo.....",
    };

    static readonly string[] SoederLegL =
    {
        ".....oDDDDDo.",
        ".....oDDDDDo.",
        ".....oDDDDDo.",
        ".....oDDDDDo.",
        ".....oDDDDDo.",
        ".....oDDDDDo.",
        ".....oFFFFFo.",
        "....oFFFFFFo.",
        "....oooooooo.",
    };

    static readonly string[] SoederLegR =
    {
        ".oDDDDDo.....",
        ".oDDDDDo.....",
        ".oDDDDDo.....",
        ".oDDDDDo.....",
        ".oDDDDDo.....",
        ".oDDDDDo.....",
        ".oFFFFFo.....",
        ".oFFFFFFo....",
        ".oooooooo....",
    };

    static Color? SoederColor(char ch) => ch switch
    {
        'o' => Out,
        'G' => Hx("#9a9a96"), 'g' => Hx("#c4c4be"), 'd' => Hx("#6c6c68"),
        'S' => Hx("#e8c4a2"), 's' => Hx("#cfa686"), 'n' => Hx("#d6ae8c"),
        'b' => Hx("#4a4440"), 'e' => Hx("#3a3a2e"),
        'M' => Hx("#4a4038"), 'C' => Hx("#a8a29a"), 'm' => Hx("#b07060"),
        'W' => Hx("#c8d8f0"), 'w' => Hx("#a8bcd8"),
        'T' => Hx("#c060a8"), 't' => Hx("#a04888"),
        'U' => Hx("#3a4a9a"), 'u' => Hx("#2c3a7a"), 'l' => Hx("#24306a"), 'B' => Hx("#1e2450"),
        'D' => Hx("#2a2e44"), 'F' => Hx("#1e1e22"),
        _ => null,
    };

    /// <summary>Markus Söder in feiner Auflösung (26×46 Spielpixel), von vorn: graue Haare, Bart, blauer Anzug, rosa Krawatte, Maßkrug. Füße bei pos.</summary>
    /// <param name="pose">0 normal, 1 trinkt aus dem Krug</param>
    public static void Soeder(CanvasItem ci, Vector2 pos, float time, bool walking, int pose = 0, bool shadow = true)
    {
        const float p = 1;
        var o = (pos - new Vector2(13, 44)).Round();
        if (shadow) R(ci, o, p, 4, 43, 18, 3, Shade);
        int step = walking ? ((int)(time * 6)) % 2 : -1;
        Map(ci, o + new Vector2(0, 37 - (step == 0 ? 1 : 0)), p, SoederLegL, SoederColor);
        Map(ci, o + new Vector2(13, 37 - (step == 1 ? 1 : 0)), p, SoederLegR, SoederColor);
        Map(ci, o, p, SoederTop, SoederColor);
        // Anstecknadel am Revers
        R(ci, o, p, 17, 24, 2, 2, Hx("#d8e4f0"));
        R(ci, o, p, 18, 25, 1, 1, Hx("#6f8fc0"));
        if (pose == 1)
        {
            // Arm angewinkelt, Krug am Mund, schräg gekippt
            ci.DrawRect(new Rect2(o + new Vector2(18, 15), 7, 12), Out);
            ci.DrawRect(new Rect2(o + new Vector2(19, 16), 5, 10), Hx("#3a4a9a"));
            ci.DrawRect(new Rect2(o + new Vector2(23, 16), 1, 10), Hx("#2c3a7a"));
            var mm = o + new Vector2(11, 9);
            ci.DrawRect(new Rect2(mm, 11, 10), Out);
            ci.DrawRect(new Rect2(mm + new Vector2(1, 1), 2, 8), Foam);
            ci.DrawRect(new Rect2(mm + new Vector2(3, 1), 7, 8), MugBeer);
            ci.DrawRect(new Rect2(mm + new Vector2(3, 2), 7, 1), MugHi);
            ci.DrawRect(new Rect2(mm + new Vector2(5, 9), 5, 3), Out);
            ci.DrawRect(new Rect2(mm + new Vector2(6, 9), 3, 2), Hx("#e8c4a2")); // Hand
            return;
        }
        // Maßkrug in der rechten Hand
        var m = o + new Vector2(21, 27);
        ci.DrawRect(new Rect2(m, 9, 12), Out);
        ci.DrawRect(new Rect2(m + new Vector2(1, 1), 6, 3), Foam);
        ci.DrawRect(new Rect2(m + new Vector2(1, 4), 6, 7), MugBeer);
        ci.DrawRect(new Rect2(m + new Vector2(2, 4), 1, 7), MugHi);
        ci.DrawRect(new Rect2(m + new Vector2(7, 4), 1, 5), Out);
    }
}
