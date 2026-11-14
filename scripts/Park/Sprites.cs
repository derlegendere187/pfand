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
        // Feines Raster: 12×12 Kunstpixel in derselben Größe wie früher 8×8
        float q = p / 1.5f;
        var o = (pos - new Vector2(6 * q, 6 * q)).Round();
        var ink = Palette.Ink;
        var skin = Palette.Skin;
        R(ci, o, q, 1.5f, 10.5f, 9, 1.5f, new Color(0, 0, 0, 0.18f)); // Schatten
        // Arme
        R(ci, o, q, -0.5f, 5.5f, 2, 4, ink);
        R(ci, o, q, 10.5f, 5.5f, 2, 4, ink);
        // Oberkörper
        R(ci, o, q, 0.5f, 3.5f, 11, 7.5f, ink);
        R(ci, o, q, 1, 4, 10, 6.5f, shirt);
        R(ci, o, q, 1, 4, 1, 6.5f, shirt.Lightened(0.18f));
        R(ci, o, q, 9.5f, 4, 1.5f, 6.5f, shirt.Darkened(0.18f));
        R(ci, o, q, 1, 9.5f, 10, 1, shirt.Darkened(0.25f));
        R(ci, o, q, 0, 6, 1, 3, skin);
        R(ci, o, q, 11, 6, 1, 3, skin);
        R(ci, o, q, 0, 8, 1, 1, skin.Darkened(0.12f));
        R(ci, o, q, 11, 8, 1, 1, skin.Darkened(0.12f));
        // Gesicht unter den Haaren
        R(ci, o, q, 3.5f, 6, 5, 2, ink);
        R(ci, o, q, 4, 6.5f, 4, 1, skin);
        // Kopf mit Haaren
        R(ci, o, q, 2.5f, -0.5f, 7, 7.5f, ink);
        R(ci, o, q, 3, 0, 6, 6.5f, hair);
        R(ci, o, q, 3, 0, 6, 1, hair.Lightened(0.2f));
        R(ci, o, q, 4, 1, 2, 1, hair.Lightened(0.35f));
        R(ci, o, q, 8, 1, 1, 5.5f, hair.Darkened(0.2f));
        R(ci, o, q, 3, 5.5f, 6, 1, hair.Darkened(0.15f));
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
        const float q = 2;
        var o = (pos - new Vector2(12, 12)).Round();
        R(ci, o, q, 1, 8.5f, 10, 1, Color.FromHtml("#f2e070")); // Reflexstreifen
        if (carrying > 0)
        {
            // Sack über der Schulter
            R(ci, o, q, 9.5f, 6.5f, 5, 5, Palette.Ink);
            R(ci, o, q, 10, 7, 4, 4, Color.FromHtml("#d9c7a3"));
            R(ci, o, q, 10, 7, 4, 1, Color.FromHtml("#efe2c4"));
            R(ci, o, q, 13, 7, 1, 4, Palette.MachineDark);
        }
    }

    /// <summary>Fremder Sammler: graue Kapuzenjacke, Plastiktüte in der Hand, wenn er was hat.</summary>
    public static void Stranger(CanvasItem ci, Vector2 pos, bool carrying)
    {
        Person(ci, pos, Color.FromHtml("#6f7378"), Color.FromHtml("#4a4e54"));
        if (!carrying) return;
        const float q = 2;
        var o = (pos - new Vector2(12, 12)).Round();
        // Plastiktüte
        R(ci, o, q, 10.5f, 6.5f, 4, 5, Palette.Ink);
        R(ci, o, q, 11, 7, 3, 4, Color.FromHtml("#e8e4da"));
        R(ci, o, q, 11, 7, 3, 1, Color.FromHtml("#b8b2a4"));
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
}
