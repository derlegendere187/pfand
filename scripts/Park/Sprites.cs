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
    public static void Bottle(CanvasItem ci, Vector2 pos, BottleType t, float rot, int colorIdx, float scale = 1f, bool onGround = false)
    {
        float p = 3 * scale;
        if (onGround)
        {
            // Schatten (nicht gedreht) und dunkler Umriss, damit Flaschen auf dem Gras auffallen
            ci.DrawRect(new Rect2(pos + new Vector2(-7, 4) * scale, new Vector2(16, 6) * scale), new Color(0, 0, 0, 0.22f));
            ci.DrawSetTransform(pos, rot, Vector2.One);
            float w = 1.5f * scale;
            if (t == BottleType.Dose)
                ci.DrawRect(new Rect2(-1.5f * p - w, -2 * p - w, 3 * p + 2 * w, 4 * p + 2 * w), Palette.Ink);
            else
            {
                ci.DrawRect(new Rect2(-0.5f * p - w, -4 * p - w, p + 2 * w, 3 * p + 2 * w), Palette.Ink);
                ci.DrawRect(new Rect2(-1.5f * p - w, -1 * p - w, 3 * p + 2 * w, 5 * p + 2 * w), Palette.Ink);
            }
        }
        else ci.DrawSetTransform(pos, rot, Vector2.One);
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
            case BottleType.Retro:
                BottleShape(ci, p, Color.FromHtml("#7fbfa8"), Color.FromHtml("#d9735a"), true);
                R(ci, new Vector2(-1.5f * p, -4 * p), p, 2, 3, 1, 2, Palette.PanelLight);
                break;
            case BottleType.Gold:
                BottleShape(ci, p, Palette.Gold, Color.FromHtml("#c99a1e"), false);
                R(ci, new Vector2(-1.5f * p, -4 * p), p, 0, 3, 1, 2, Palette.PanelLight);
                break;
            default:
            {
                var o = new Vector2(-1.5f * p, -2 * p);
                var c = Palette.Cans[colorIdx % Palette.Cans.Length];
                R(ci, o, p, 0, 0, 3, 4, c);
                R(ci, o, p, 0, 0, 3, 1, Color.FromHtml("#d6ccb8"));
                R(ci, o, p, 0, 2, 3, 1, Palette.PanelLight);
                if (onGround) R(ci, o, p, 0, 0.5f, 0.6f, 3, new Color(1, 1, 1, 0.55f)); // Glanz
                break;
            }
        }
        // Glanzstreifen auf Glas und Plastik
        if (onGround && t != BottleType.Dose)
            R(ci, new Vector2(-1.5f * p, -4 * p), p, 0.2f, 3.3f, 0.6f, 4, new Color(1, 1, 1, 0.5f));
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

    /// <summary>Kurzes Funkeln (kleines Plus), um liegende Flaschen zu finden.</summary>
    public static void Sparkle(CanvasItem ci, Vector2 pos, float strength)
    {
        var c = new Color(1, 1, 0.92f, strength);
        ci.DrawRect(new Rect2(pos.X - 1, pos.Y - 5, 2, 10), c);
        ci.DrawRect(new Rect2(pos.X - 5, pos.Y - 1, 10, 2), c);
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

    /// <summary>Tischtennisplatte von oben, zentriert.</summary>
    public static void Table(CanvasItem ci, Vector2 pos)
    {
        var s = ParkLayout.TableSize;
        var r = new Rect2(pos - s / 2, s);
        ci.DrawRect(r.Grow(4), Palette.InkSoft);
        ci.DrawRect(r, Color.FromHtml("#4f8a6a"));
        ci.DrawRect(new Rect2(r.Position.X + s.X / 2 - 1, r.Position.Y, 2, s.Y), Palette.PanelLight);
        ci.DrawRect(new Rect2(r.Position.X - 4, r.Position.Y + s.Y / 2 - 2, s.X + 8, 4), Palette.PanelLight);
    }

    public static void Ball(CanvasItem ci, Vector2 pos)
    {
        ci.DrawRect(new Rect2(pos - new Vector2(3, 3), 6, 6), new Color(0, 0, 0, 0.2f));
        ci.DrawRect(new Rect2(pos - new Vector2(3, 5), 6, 6), Color.FromHtml("#fffaf0"));
    }

    /// <summary>Runder Grill von oben (14×14, Pixel 3), zentriert. Glüht, wenn jemand grillt.</summary>
    public static void Grill(CanvasItem ci, Vector2 pos, float time, bool active)
    {
        float p = 3;
        var o = pos - new Vector2(7 * p, 7 * p);
        R(ci, o, p, 2, 13, 11, 2, new Color(0, 0, 0, 0.18f));
        var rim = Color.FromHtml("#3a3530");
        R(ci, o, p, 3, 0, 8, 14, rim);
        R(ci, o, p, 1, 2, 12, 10, rim);
        R(ci, o, p, 2, 1, 10, 12, rim);
        var coal = Color.FromHtml("#4a2a1c");
        R(ci, o, p, 3, 2, 8, 10, coal);
        R(ci, o, p, 2, 3, 10, 8, coal);
        if (active)
        {
            var hot = new[] { Color.FromHtml("#e08a4a"), Color.FromHtml("#f2c75c"), Color.FromHtml("#d9735a") };
            for (int i = 0; i < 9; i++)
            {
                int x = 3 + (i * 5) % 8, y = 3 + (i * 3) % 8;
                int c = ((int)(time * 3) + i) % 3;
                R(ci, o, p, x, y, 1, 1, hot[c]);
            }
        }
        // Rost
        for (int y = 3; y < 12; y += 2) R(ci, o, p, 2, y, 10, 0.34f, Color.FromHtml("#b8b0a0"));
        if (active)
        {
            var wurst = Color.FromHtml("#a8503c");
            R(ci, o, p, 4, 4, 4, 1, wurst);
            R(ci, o, p, 6, 7, 4, 1, wurst);
            R(ci, o, p, 4, 9, 3, 1, wurst);
        }
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

    /// <summary>Bierkasten von oben: 4×5 Fächer, gefüllte zeigen Flaschenhals mit Kronkorken.</summary>
    public static void Kasten(CanvasItem ci, Rect2 r, int count)
    {
        float p = 2;
        var o = r.Position;
        R(ci, o, p, 1, 15, 13, 2, new Color(0, 0, 0, 0.18f));
        R(ci, o, p, 0, 0, 13, 16, Color.FromHtml("#b8432f"));
        R(ci, o, p, 0, 0, 13, 1, Color.FromHtml("#d9735a"));
        for (int i = 0; i < 20; i++)
        {
            int col = i % 4, row = i / 4;
            var cx = 1 + col * 3; var cy = 1 + row * 3;
            if (i < count)
            {
                R(ci, o, p, cx, cy, 2, 2, Color.FromHtml("#5a3a1e"));
                R(ci, o, p, cx + 0.5f, cy + 0.5f, 1, 1, Palette.Gold);
            }
            else R(ci, o, p, cx, cy, 2, 2, Color.FromHtml("#3a1e16"));
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

    /// <summary>Pfandkiste der Sammler am Rand.</summary>
    public static void Depot(CanvasItem ci, Vector2 pos)
    {
        var r = new Rect2(pos - new Vector2(18, 16), 36, 32);
        ci.DrawRect(r, Palette.InkSoft);
        ci.DrawRect(r.Grow(-4), Palette.Machine);
        ci.DrawRect(new Rect2(r.Position + new Vector2(8, 8), 20, 8), Palette.Display);
        ci.DrawRect(new Rect2(r.Position + new Vector2(10, 10), 6, 4), Palette.DisplayText);
    }

    /// <summary>Requisiten für laufende Events.</summary>
    public static void EventProps(CanvasItem ci, EventKind kind, Vector2 c, float time, bool targetDown = false, float wobble = 0)
    {
        switch (kind)
        {
            case EventKind.Geburtstag:
            {
                // große Decke mit Kuchen, Luftballons
                var r = new Rect2(c - new Vector2(40, 26), 80, 52);
                ci.DrawRect(r, Color.FromHtml("#e8a0b8"));
                for (int x = 0; x < 80; x += 16)
                for (int y = 0; y < 52; y += 16)
                    if ((x / 16 + y / 16) % 2 == 0) ci.DrawRect(new Rect2(r.Position + new Vector2(x, y), 8, 8), Palette.PanelLight);
                ci.DrawRect(new Rect2(c - new Vector2(10, 8), 20, 16), Palette.PanelLight);
                ci.DrawRect(new Rect2(c - new Vector2(10, 8), 20, 4), Color.FromHtml("#d9735a"));
                ci.DrawRect(new Rect2(c - new Vector2(1, 14), 2, 6), Palette.Gold);
                var colors = new[] { "#d9735a", "#6fa8d6", "#f2c75c", "#b496e0" };
                for (int i = 0; i < 4; i++)
                {
                    var bp = c + new Vector2(-56 + i * 36, -50 + Mathf.Sin(time * 2 + i) * 3);
                    ci.DrawLine(bp + new Vector2(0, 8), bp + new Vector2(0, 24), Palette.InkSoft, 1);
                    ci.DrawRect(new Rect2(bp - new Vector2(6, 8), 12, 16), Color.FromHtml(colors[i]));
                    ci.DrawRect(new Rect2(bp - new Vector2(4, 6), 3, 3), Palette.PanelLight);
                }
                break;
            }
            case EventKind.Bierball:
            {
                // Zwei Grundlinien, in der Mitte die Zielflasche (steht, wackelt oder liegt)
                ci.DrawRect(new Rect2(c + new Vector2(-82, -48), 4, 96), Palette.PanelLight);
                ci.DrawRect(new Rect2(c + new Vector2(78, -48), 4, 96), Palette.PanelLight);
                ci.DrawRect(new Rect2(c - new Vector2(8, 8), 16, 16), new Color(0, 0, 0, 0.15f));
                float rot = targetDown ? 1.5f : Mathf.Sin(time * 30) * 0.25f * wobble;
                Bottle(ci, c + (targetDown ? new Vector2(6, 4) : Vector2.Zero), BottleType.Bier, rot, 0, 1.3f);
                break;
            }
            case EventKind.Oktoberfest:
            {
                // Festzelt mit blau-weißem Rautendach, davor zwei lange Biertische
                var tent = new Rect2(c + new Vector2(-96, -86), 192, 66);
                ci.DrawRect(tent.Grow(4), Palette.InkSoft);
                ci.DrawRect(tent, Palette.PanelLight);
                for (int x = 0; x < 192; x += 16)
                for (int y = 0; y < 48; y += 16)
                    if ((x / 16 + y / 16) % 2 == 0) ci.DrawRect(new Rect2(tent.Position + new Vector2(x, y), 16, 16), Color.FromHtml("#6fa8d6"));
                ci.DrawRect(new Rect2(tent.Position + new Vector2(0, 48), 192, 4), Palette.InkSoft);
                ci.DrawRect(new Rect2(tent.Position + new Vector2(80, 52), 32, 14), Palette.Ink); // Eingang
                for (int i = 0; i < 4; i++) // Wimpel auf dem Dach
                {
                    bool up = ((int)(time * 3) + i) % 2 == 0;
                    ci.DrawRect(new Rect2(tent.Position + new Vector2(20 + i * 50, up ? -10 : -8), 8, 6), i % 2 == 0 ? Color.FromHtml("#6fa8d6") : Palette.PanelLight);
                }
                for (int t = 0; t < 2; t++)
                {
                    var tr = new Rect2(c + new Vector2(-84, ParkLayout.OktoberfestTableY + t * 50), 168, 12);
                    ci.DrawRect(tr.Grow(2), Palette.WoodDark);
                    ci.DrawRect(tr, Palette.Wood);
                    for (int i = 0; i < 5; i++) // Maßkrüge
                        ci.DrawRect(new Rect2(tr.Position + new Vector2(14 + i * 32, 3), 6, 6), Palette.Gold);
                }
                break;
            }
            case EventKind.Feiertag:
            case EventKind.Maennertag:
                break; // eigene Darstellung in ParkView (Wimpelkette, Bollerwagen)
            default:
            {
                // Bühne mit Boxen und Lichtern
                var stage = new Rect2(c + new Vector2(-80, -70), 160, 44);
                ci.DrawRect(stage.Grow(4), Palette.InkSoft);
                ci.DrawRect(stage, Color.FromHtml("#5a4a3a"));
                ci.DrawRect(new Rect2(stage.Position + new Vector2(6, 6), 18, 30), Palette.Ink);
                ci.DrawRect(new Rect2(stage.End - new Vector2(24, 38), 18, 30), Palette.Ink);
                var lights = new[] { "#d9735a", "#6fa8d6", "#f2c75c", "#b496e0", "#8cc472" };
                for (int i = 0; i < 5; i++)
                {
                    bool on = ((int)(time * 4) + i) % 2 == 0;
                    ci.DrawRect(new Rect2(stage.Position + new Vector2(34 + i * 20, 4), 10, 6), on ? Color.FromHtml(lights[i]) : Palette.InkSoft);
                }
                Person(ci, stage.GetCenter() + new Vector2(0, 4 + Mathf.Round(Mathf.Sin(time * 8) * 2)), Color.FromHtml("#2a1d14"), Color.FromHtml("#d9735a"));
                break;
            }
        }
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

    /// <summary>Tisch zwischen zwei Bänken (16×4, Pixel 4), zentriert.</summary>
    public static void PicnicTable(CanvasItem ci, Vector2 pos, float p = 4)
    {
        var o = pos - new Vector2(8 * p, 2 * p);
        R(ci, o, p, 0, 0, 16, 4, Palette.WoodDark);
        R(ci, o, p, 0.5f, 0.5f, 15, 3, Palette.Wood);
        R(ci, o, p, 3, 1, 1.5f, 2, Palette.PanelLight);   // Teller
        R(ci, o, p, 10, 1, 1, 2, Color.FromHtml("#8a5a2e")); // Flasche
    }

    /// <summary>Pfandring: Metallring um den Eimer, in dem Flaschen stehen.</summary>
    public static void Pfandring(CanvasItem ci, Vector2 pos)
    {
        ci.DrawArc(pos, 21, 0, Mathf.Tau, 24, Color.FromHtml("#8a8a80"), 4);
        for (int i = 0; i < 6; i++)
        {
            float a = i / 6f * Mathf.Tau + 0.3f;
            var bp = pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 21;
            ci.DrawRect(new Rect2(bp.Round() - new Vector2(2, 2), 4, 4), i % 2 == 0 ? Color.FromHtml("#8a5a2e") : Color.FromHtml("#4f8a4a"));
        }
    }

    /// <summary>Bollerwagen mit Bierkasten. Räder wackeln, solange er fährt.</summary>
    public static void Bollerwagen(CanvasItem ci, Vector2 pos, float time, float p = 3)
    {
        var o = pos - new Vector2(6 * p, 4 * p);
        float wob = Mathf.Sin(time * 12) > 0 ? 0 : 0.5f;
        R(ci, o, p, 0, 7, 12, 1, new Color(0, 0, 0, 0.15f));
        R(ci, o, p, 0, 1, 12, 6, Palette.WoodDark);
        R(ci, o, p, 1, 2, 10, 4, Palette.Wood);
        R(ci, o, p, 2, 2, 6, 4, Color.FromHtml("#b8432f"));                 // Kasten
        for (int i = 0; i < 3; i++) R(ci, o, p, 2.5f + i * 2, 3, 1, 1, Palette.Gold);
        R(ci, o, p, 12, 3, 4, 1, Palette.InkSoft);                          // Deichsel
        R(ci, o, p, 1, 7 + wob, 2, 1, Palette.Ink);
        R(ci, o, p, 9, 7 + wob, 2, 1, Palette.Ink);
    }

    /// <summary>Wimpelkette quer über den oberen Rand (Feiertag).</summary>
    public static void Garland(CanvasItem ci, float width, float time)
    {
        var colors = new[] { "#d9735a", "#f2c75c", "#6fa8d6", "#8cc472", "#b496e0" };
        ci.DrawRect(new Rect2(0, 10, width, 2), Palette.InkSoft);
        int n = (int)(width / 24);
        for (int i = 0; i < n; i++)
        {
            float x = 6 + i * 24;
            float sway = ((int)(time * 2) + i) % 2 == 0 ? 0 : 1;
            var c = Color.FromHtml(colors[i % colors.Length]);
            ci.DrawRect(new Rect2(x, 12, 12, 4 + sway), c);
            ci.DrawRect(new Rect2(x + 2, 16 + sway, 8, 4), c);
            ci.DrawRect(new Rect2(x + 4, 20 + sway, 4, 3), c);
        }
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
