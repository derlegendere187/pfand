using Godot;
using Pfand.Core;

namespace Pfand.Park;

/// <summary>
/// Pixelkunst per Code. Jede Methode zeichnet ein kleines Raster aus Rechtecken,
/// skaliert mit Pixelgröße p. Später durch echte Sprites ersetzbar.
/// </summary>
public static partial class Sprites
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

    /// <summary>
    /// Zeichnet ein Pixelraster aus Textzeilen. Jedes Zeichen ist ein Kunstpixel der Größe p,
    /// '.' bleibt leer. Gleiche Zeichen in einer Zeile werden zu einem Rechteck zusammengefasst.
    /// </summary>
    static void Map(CanvasItem ci, Vector2 o, float p, string[] rows, System.Func<char, Color?> pal)
    {
        for (int y = 0; y < rows.Length; y++)
        {
            var row = rows[y];
            int x = 0;
            while (x < row.Length)
            {
                char ch = row[x];
                int n = 1;
                while (x + n < row.Length && row[x + n] == ch) n++;
                if (ch != '.' && pal(ch) is { } c) R(ci, o, p, x, y, n, 1, c);
                x += n;
            }
        }
    }

    static readonly string[] GlassBottle =
    {
        "..oo..",
        ".occo.",
        ".oCCo.",
        ".ohbo.",
        ".ohdo.",
        "ohbbdo",
        "ohbbdo",
        "ollLLo",
        "ollLLo",
        "ohbbdo",
        "ohbbdo",
        "ohbddo",
        ".oooo.",
    };

    static readonly string[] CanArt =
    {
        ".oooo.",
        "oSssSo",
        "osTTso",
        "ohccdo",
        "ohwwdo",
        "ohccdo",
        "ohccdo",
        "oSssSo",
        ".oooo.",
    };

    // Farben je Sorte: Glas, Schatten, Glanz, Deckel, Deckel dunkel, Etikett (null = keins)
    static (string body, string dark, string hi, string cap, string capDark, string label) BottleColors(BottleType t) => t switch
    {
        BottleType.Bier => ("#8a5a2e", "#5e3a1c", "#b88450", "#c8553d", "#8e3526", "#f2ead8"),
        BottleType.Wasser => ("#4f8a4a", "#33603a", "#7ab870", "#6fa8d6", "#3f7fb5", "#f2ead8"),
        BottleType.Plastik => ("#a9d4ee", "#78aed0", "#e4f4fc", "#3f7fb5", "#2a5a88", null),
        BottleType.Retro => ("#7fbfa8", "#4f8f78", "#b4e2d0", "#d9735a", "#a04a3a", "#d9735a"),
        _ => ("#f2c45a", "#c99a1e", "#fff0a0", "#c99a1e", "#8a6a10", null),
    };

    /// <summary>Flasche oder Dose, zentriert auf pos, gedreht um rot (Radiant). Feine Pixel (2 × scale).</summary>
    public static void Bottle(CanvasItem ci, Vector2 pos, BottleType t, float rot, int colorIdx, float scale = 1f, bool onGround = false)
    {
        float p = 2 * scale;
        if (onGround)
            ci.DrawRect(new Rect2(pos + new Vector2(-7, 5) * scale, new Vector2(16, 5) * scale), new Color(0, 0, 0, 0.22f));
        ci.DrawSetTransform(pos, rot, Vector2.One);
        var ink = Palette.Ink;
        if (t == BottleType.Dose)
        {
            var c = Palette.Cans[colorIdx % Palette.Cans.Length];
            Map(ci, new Vector2(-3 * p, -4.5f * p), p, CanArt, ch => ch switch
            {
                'o' => ink,
                'S' => Color.FromHtml("#e8e2d4"),
                's' => Color.FromHtml("#b8b0a0"),
                'T' => Color.FromHtml("#8a8478"),
                'c' => c,
                'h' => c.Lightened(0.35f),
                'd' => c.Darkened(0.3f),
                'w' => Palette.PanelLight,
                _ => null,
            });
        }
        else
        {
            var k = BottleColors(t);
            Color H(string x) => Color.FromHtml(x);
            Map(ci, new Vector2(-3 * p, -6.5f * p), p, GlassBottle, ch => ch switch
            {
                'o' => ink,
                'c' => H(k.cap),
                'C' => H(k.capDark),
                'b' => H(k.body),
                'd' => H(k.dark),
                'h' => H(k.hi),
                'l' => k.label == null ? H(k.body) : H(k.label),
                'L' => k.label == null ? H(k.dark) : H(k.label).Darkened(0.15f),
                _ => null,
            });
        }
        ci.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary>Kurzes Funkeln (kleines Plus), um liegende Flaschen zu finden.</summary>
    public static void Sparkle(CanvasItem ci, Vector2 pos, float strength)
    {
        var c = new Color(1, 1, 0.92f, strength);
        ci.DrawRect(new Rect2(pos.X - 1, pos.Y - 5, 2, 10), c);
        ci.DrawRect(new Rect2(pos.X - 5, pos.Y - 1, 10, 2), c);
    }

    /// <summary>Rauch über dem Grill: drei Wölkchen, die aufsteigen.</summary>
    public static void Smoke(CanvasItem ci, Vector2 pos, float time)
    {
        for (int i = 0; i < 3; i++)
        {
            float t = (time * 0.6f + i / 3f) % 1f;
            var pp = pos + new Vector2(Mathf.Round(Mathf.Sin(t * 6 + i) * 6), Mathf.Round(-10 - t * 40));
            float s = 6 + t * 8;
            ci.DrawRect(new Rect2(pp - new Vector2(s / 2, s / 2), s, s), new Color(0.95f, 0.93f, 0.9f, 0.55f * (1 - t)));
        }
    }

    /// <summary>Sammler: Person mit orangener Warnweste und Sack, wenn er was trägt.</summary>
    public static void Collector(CanvasItem ci, Vector2 pos, int carrying)
    {
        Person(ci, pos, Color.FromHtml("#e08a4a"), Color.FromHtml("#5a3a26"));
        var o = pos - new Vector2(12, 12);
        R(ci, o, 3, 2, 4, 4, 1, Color.FromHtml("#f2c75c")); // Reflexstreifen
        if (carrying > 0)
        {
            R(ci, o, 3, 7, 5, 3, 3, Color.FromHtml("#d9c7a3"));
            R(ci, o, 3, 7, 5, 3, 1, Palette.MachineDark);
        }
    }

    /// <summary>Fremder Sammler: graue Kapuzenjacke, Plastiktüte in der Hand, wenn er was hat.</summary>
    public static void Stranger(CanvasItem ci, Vector2 pos, bool carrying)
    {
        Person(ci, pos, Color.FromHtml("#6f7378"), Color.FromHtml("#4a4e54"));
        if (!carrying) return;
        var o = pos - new Vector2(12, 12);
        R(ci, o, 3, 8, 5, 2, 3, Color.FromHtml("#e8e4da"));
        R(ci, o, 3, 8, 5, 2, 1, Color.FromHtml("#b8b2a4"));
    }

    static readonly string[] LampArt =
    {
        "...ooo...",
        "..oRRRo..",
        ".oRrRRRo.",
        "ooooooooo",
        ".oWYoYYo.",
        ".oYYoYYo.",
        ".oYMoMYo.",
        ".oMMoMMo.",
        "ooooooooo",
        "...oPo...",
        "...oPo...",
        "...oPo...",
        "...oPo...",
        "...oPo...",
        "...oPo...",
        "...oPo...",
        "..oPPPo..",
        ".ooooooo.",
    };

    /// <summary>Parklaterne (9×18 feine Pixel), wie die Figuren leicht von vorn. Fuß knapp unter pos.</summary>
    public static void Lamp(CanvasItem ci, Vector2 pos, float p = 2)
    {
        var o = pos - new Vector2(4.5f * p, 12 * p);
        R(ci, o, p, 1, 17, 8, 2, new Color(0, 0, 0, 0.2f)); // Schatten
        Map(ci, o, p, LampArt, ch => ch switch
        {
            'o' => Color.FromHtml("#1e1e24"),
            'R' => Color.FromHtml("#3a3a44"),
            'r' => Color.FromHtml("#5a5a66"),
            'P' => Color.FromHtml("#4a4a56"),
            'W' => Color.FromHtml("#fff6d8"),
            'Y' => Color.FromHtml("#ffe2a0"),
            'M' => Color.FromHtml("#e8b860"),
            _ => null,
        });
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
