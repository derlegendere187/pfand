using System;
using System.Collections.Generic;
using Godot;

namespace Pfand.Park;

/// <summary>
/// Boden als ein Bild: ruhige Wiese mit wenigen Halmen, Wege als eine zusammenhängende Fläche.
/// Wo sich Wege kreuzen, gibt es keine Kanten, die Innenecken sind abgerundet.
/// </summary>
public static class GroundArt
{
    public static ImageTexture Build(Vector2 size, List<Rect2> paths, int seed = 7)
    {
        int w = Math.Max(8, (int)size.X), h = Math.Max(8, (int)size.Y);
        const int c = 4;   // ein Kunstpixel
        int cw = (w + c - 1) / c, ch = (h + c - 1) / c;
        var path = new bool[cw, ch];
        for (int y = 0; y < ch; y++)
            for (int x = 0; x < cw; x++)
            {
                var pt = new Vector2(x * c + c / 2f, y * c + c / 2f);
                foreach (var r in paths) if (r.HasPoint(pt)) { path[x, y] = true; break; }
            }
        bool P(int x, int y) => x >= 0 && y >= 0 && x < cw && y < ch && path[x, y];

        // Innenecken an Kreuzungen und Abzweigen runden
        var round = new List<(int, int)>();
        for (int y = 0; y < ch; y++)
            for (int x = 0; x < cw; x++)
            {
                if (path[x, y]) continue;
                int n = 0;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        if ((dx != 0 || dy != 0) && P(x + dx, y + dy)) n++;
                if (n >= 5) round.Add((x, y));
            }
        foreach (var (x, y) in round) path[x, y] = true;

        var img = Image.CreateEmpty(cw * c, ch * c, false, Image.Format.Rgba8);
        var rng = new Random(seed);
        for (int y = 0; y < ch; y++)
            for (int x = 0; x < cw; x++)
            {
                Color col;
                if (path[x, y])
                {
                    bool up = y > 0 && !P(x, y - 1), left = x > 0 && !P(x - 1, y);
                    bool down = y < ch - 1 && !P(x, y + 1), right = x < cw - 1 && !P(x + 1, y);
                    double k = rng.NextDouble();
                    col = up || left ? Palette.PathDark
                        : down || right ? Palette.PathLight
                        : k < 0.07 ? Palette.PathDark : k < 0.14 ? Palette.PathLight : k < 0.17 ? Palette.PathPebble : Palette.Path;
                }
                else
                {
                    double k = rng.NextDouble();
                    col = k < 0.018 ? Palette.TuftLight : k < 0.028 ? Palette.TuftDark : k < 0.030 ? Palette.GrassBright : Palette.Grass;
                    // Halm: zweiter Pixel schräg darüber
                    if (k < 0.018 && x + 1 < cw && y > 0 && !path[x + 1, y - 1])
                        img.FillRect(new Rect2I((x + 1) * c, (y - 1) * c, c, c), Palette.TuftLight);
                    // Wiese direkt unter einem Weg liegt leicht im Schatten
                    else if (y > 0 && P(x, y - 1)) col = Palette.TuftDark;
                }
                if (img.GetPixel(x * c, y * c).A > 0 && !path[x, y] && col == Palette.Grass) continue;
                img.FillRect(new Rect2I(x * c, y * c, c, c), col);
            }
        return ImageTexture.CreateFromImage(img);
    }
}
