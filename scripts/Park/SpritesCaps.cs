using System.Collections.Generic;
using Godot;
using Pfand.Core;

namespace Pfand.Park;

/// <summary>
/// Kronkorken im feinen Stil: 24×24 Kunstpixel. Deckel mit gezacktem Rand, Metallglanz und Umriss;
/// die 12×12-Motive werden per EPX geglättet verdoppelt und bekommen einen leichten Schatten.
/// </summary>
public static partial class Sprites
{
    const int CapN = 24;
    static readonly Dictionary<CapData.Motif, Color?[]> CapCache = new();
    static Color?[] _capUnknown;

    /// <param name="p">Größe eines Feldes im alten 12er-Raster; gezeichnet wird in halber Größe auf 24×24.</param>
    public static void Cap(CanvasItem ci, Vector2 topLeft, float p, CapData.Motif m, bool found)
    {
        Color?[] img;
        if (found)
        {
            if (!CapCache.TryGetValue(m, out img)) CapCache[m] = img = BuildCap(m);
        }
        else img = _capUnknown ??= BuildCap(null);

        float q = p / 2;
        var o = topLeft.Round();
        for (int y = 0; y < CapN; y++)
            for (int x = 0; x < CapN;)
            {
                var c = img[y * CapN + x];
                if (c == null) { x++; continue; }
                int s = x;
                while (x < CapN && img[y * CapN + x] == c) x++;
                ci.DrawRect(new Rect2(o.X + s * q, o.Y + y * q, (x - s) * q, q), c.Value);
            }
    }

    /// <summary>Der fertige Kronkorken als 24×24-Farbfeld (null = durchsichtig).</summary>
    static Color?[] BuildCap(CapData.Motif m)
    {
        bool found = m != null;
        var img = new Color?[CapN * CapN];
        var rim = found ? Hx("#c9ccd6") : Hx("#ddd0b2");
        var rimHi = found ? Hx("#f0f2f8") : Hx("#efe4cc");
        var rimLo = found ? Hx("#7c8296") : Hx("#b8a882");
        var outline = found ? Hx("#3a3c4a") : Hx("#9a8a6a");
        var face = found ? Hx(m.Face) : Hx("#e6dbc0");

        // Ganz bedruckte Motive reichen bis an die Zacken
        bool fullPrint = false;
        if (found) foreach (var px in m.Pixels) if (px.X == 1 && px.W == 10) { fullPrint = true; break; }
        float faceR = fullPrint ? 10.2f : 8.3f;
        const int teeth = 21;
        var light = new Vector2(-0.6f, -0.8f);

        // Deckel: Zacken, Metall, Rille um die Fläche
        var inside = new bool[CapN * CapN];
        for (int y = 0; y < CapN; y++)
            for (int x = 0; x < CapN; x++)
            {
                var d = new Vector2(x + 0.5f - 12, y + 0.5f - 12);
                float r = d.Length();
                float a = Mathf.Atan2(d.Y, d.X);
                float outer = Mathf.Cos(a * teeth) > -0.2f ? 12f : 11.1f;
                if (r > outer) continue;
                inside[y * CapN + x] = true;
                Color c;
                if (r <= faceR) c = face;
                else
                {
                    float lit = r > 0.01f ? d.Normalized().Dot(light) : 0;
                    c = lit > 0.45f ? rimHi : lit < -0.35f ? rimLo : rim;
                    if (!fullPrint && r <= faceR + 1.1f) c = rimLo;   // Rille um die Fläche
                    else if (Mathf.Cos(a * teeth) < -0.6f && r > 9.6f) c = c.Darkened(0.12f); // Falten zwischen den Zacken
                }
                img[y * CapN + x] = c;
            }

        // Motiv: 12er-Raster mit Fläche als Grund, per EPX verdoppelt
        if (found)
        {
            var g = new Color[12 * 12];
            for (int i = 0; i < g.Length; i++) g[i] = face;
            foreach (var px in m.Pixels)
            {
                var c = Hx(px.Col);
                for (int yy = px.Y; yy < px.Y + px.H; yy++)
                    for (int xx = px.X; xx < px.X + px.W; xx++)
                        if (xx >= 0 && xx < 12 && yy >= 0 && yy < 12) g[yy * 12 + xx] = c;
            }
            var big = Epx(g, 12);
            var shade = face.Darkened(0.22f);
            for (int y = 0; y < CapN; y++)
                for (int x = 0; x < CapN; x++)
                {
                    int i = y * CapN + x;
                    if (!inside[i]) continue;
                    var c = big[i];
                    if (c != face) img[i] = c;
                    // Schlagschatten des Motivs auf der Fläche (nach rechts unten)
                    else if (x > 0 && y > 0 && img[i] == face && big[(y - 1) * CapN + x - 1] != face)
                        img[i] = shade;
                }
            // Kanten der Motivteile: unten rechts etwas dunkler, oben links etwas heller
            var copy = (Color?[])img.Clone();
            for (int y = 1; y < CapN - 1; y++)
                for (int x = 1; x < CapN - 1; x++)
                {
                    int i = y * CapN + x;
                    var c = big[i];
                    if (c == face || !inside[i]) continue;
                    if (big[i + CapN] != c && big[i + 1] != c) copy[i] = c.Darkened(0.18f);
                    else if (big[i - CapN] != c && big[i - 1] != c) copy[i] = c.Lightened(0.15f);
                }
            img = copy;
        }
        else
        {
            // Fragezeichen auf unentdeckten Deckeln
            string[] qm =
            {
                "..xxxx..",
                ".xx..xx.",
                "......xx",
                ".....xx.",
                "...xxx..",
                "...xx...",
                "........",
                "...xx...",
            };
            var col = Hx("#a8987a");
            for (int y = 0; y < qm.Length; y++)
                for (int x = 0; x < 8; x++)
                    if (qm[y][x] == 'x') img[(y + 7) * CapN + x + 8] = col;
        }

        // Glanz oben links auf der Fläche
        for (int k = 0; k < 4; k++)
        {
            int gx = 7 + k, gy = 6 - (k > 1 ? 1 : 0);
            int i = gy * CapN + gx;
            if (img[i] is Color c && (!found || fullPrint || new Vector2(gx + 0.5f - 12, gy + 0.5f - 12).Length() <= faceR))
                img[i] = c.Lerp(Colors.White, 0.35f);
        }

        // Umriss: jeder Deckelpixel mit leerem Nachbarn
        var res = (Color?[])img.Clone();
        for (int y = 0; y < CapN; y++)
            for (int x = 0; x < CapN; x++)
            {
                int i = y * CapN + x;
                if (!inside[i]) continue;
                bool edge = x == 0 || y == 0 || x == CapN - 1 || y == CapN - 1
                    || !inside[i - 1] || !inside[i + 1] || !inside[i - CapN] || !inside[i + CapN];
                if (edge) res[i] = outline;
            }
        return res;
    }

    /// <summary>EPX/Scale2x: verdoppelt Pixelkunst und glättet Treppen an Diagonalen.</summary>
    static Color[] Epx(Color[] src, int n)
    {
        int m = n * 2;
        var dst = new Color[m * m];
        Color At(int x, int y) => src[Mathf.Clamp(y, 0, n - 1) * n + Mathf.Clamp(x, 0, n - 1)];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                var p = At(x, y);
                var a = At(x, y - 1); var b = At(x + 1, y); var c = At(x - 1, y); var d = At(x, y + 1);
                var e1 = p; var e2 = p; var e3 = p; var e4 = p;
                if (c == a && c != d && a != b) e1 = a;
                if (a == b && a != c && b != d) e2 = b;
                if (d == c && d != b && c != a) e3 = c;
                if (b == d && b != a && d != c) e4 = d;
                dst[(2 * y) * m + 2 * x] = e1;
                dst[(2 * y) * m + 2 * x + 1] = e2;
                dst[(2 * y + 1) * m + 2 * x] = e3;
                dst[(2 * y + 1) * m + 2 * x + 1] = e4;
            }
        return dst;
    }
}
