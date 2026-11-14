using System;
using System.Linq;
using Godot;
using Pfand.Core;
using Pfand.Park;

namespace Pfand.UI;

/// <summary>Das Kronkorken-Album als aufgeschlagenes Buch: links und rechts je ein Set, Blättern mit Pfeilen.</summary>
public partial class AlbumPopup : Control
{
    AlbumBook _book;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;

        var dim = new ColorRect { Color = new Color(Palette.Ink, 0.55f) };
        dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        dim.GuiInput += e => { if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left) Close(); };
        AddChild(dim);

        _book = new AlbumBook { CloseRequested = Close };
        AddChild(_book);
        Resized += Fit;
    }

    void Fit()
    {
        var s = Mathf.Min(1f, Mathf.Min((Size.X - 24) / AlbumBook.BookSize.X, (Size.Y - 24) / AlbumBook.BookSize.Y));
        _book.Scale = new Vector2(s, s);
        _book.Position = ((Size - AlbumBook.BookSize * s) / 2).Round();
    }

    public void Open()
    {
        _book.Refresh();
        Visible = true;
        Fit();
    }

    public void Close() => Visible = false;

    public override void _UnhandledInput(InputEvent e)
    {
        if (!Visible || e is not InputEventKey { Pressed: true } k) return;
        if (k.Keycode == Key.Escape) Close();
        else if (k.Keycode is Key.Right or Key.D or Key.Pagedown) _book.Turn(1);
        else if (k.Keycode is Key.Left or Key.A or Key.Pageup) _book.Turn(-1);
        else return;
        GetViewport().SetInputAsHandled();
    }
}

/// <summary>Das Buch selbst, komplett gezeichnet: Ledereinband, zwei Seiten, Rücken, Blätter-Animation.</summary>
public partial class AlbumBook : Control
{
    public static readonly Vector2 BookSize = new(1040, 660);
    public Action CloseRequested;

    const float Margin = 36, Spine = 26, PageW = (1040 - 2 * 36 - 26) / 2f, PageTop = 38, PageH = 660 - 2 * 38;
    const float FlipTime = 0.32f;

    static readonly Color Leather = Color.FromHtml("#6b3f2a"), LeatherDark = Color.FromHtml("#4a2a1a"), LeatherHi = Color.FromHtml("#8a5638");
    static readonly Color Stitch = Color.FromHtml("#d8b878"), Brass = Color.FromHtml("#c99a3e"), BrassHi = Color.FromHtml("#f0d488");
    static readonly Color Page = Color.FromHtml("#efe3c8"), PageEdge = Color.FromHtml("#d8c8a4"), PageLine = Color.FromHtml("#c4b08a");

    string[] _sets = Array.Empty<string>();
    int _spread;            // linke Seite zeigt Set 2*_spread, rechte 2*_spread+1
    int _from = -1, _dir;   // laufendes Blättern
    float _flip;
    int _found;

    Rect2 LeftPage => new(Margin, PageTop, PageW, PageH);
    Rect2 RightPage => new(Margin + PageW + Spine, PageTop, PageW, PageH);
    float SpineX => Margin + PageW + Spine / 2;
    int Spreads => (_sets.Length + 1) / 2;
    Rect2 PrevRect => new(LeftPage.Position + new Vector2(14, PageH - 46), new Vector2(40, 34));
    Rect2 NextRect => new(RightPage.End - new Vector2(54, 46), new Vector2(40, 34));
    Rect2 CloseRect => new(BookSize.X - 34, 4, 30, 30);

    public override void _Ready()
    {
        Size = BookSize;
        MouseFilter = MouseFilterEnum.Stop;
    }

    public void Refresh()
    {
        _sets = CapData.Sets;
        _found = 0;
        foreach (var kv in GameState.I.Caps) if (kv.Value > 0) _found++;
        _spread = Mathf.Clamp(_spread, 0, Math.Max(0, Spreads - 1));
        _from = -1;
        QueueRedraw();
    }

    public void Turn(int dir)
    {
        int to = _spread + dir;
        if (to < 0 || to >= Spreads || _from >= 0) return;
        _from = _spread;
        _spread = to;
        _dir = dir;
        _flip = 0;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_from < 0) return;
        _flip += (float)delta / FlipTime;
        if (_flip >= 1) _from = -1;
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton { Pressed: true } mb)
        {
            if (mb.ButtonIndex == MouseButton.WheelDown) Turn(1);
            else if (mb.ButtonIndex == MouseButton.WheelUp) Turn(-1);
            else if (mb.ButtonIndex == MouseButton.Left)
            {
                if (CloseRect.HasPoint(mb.Position)) CloseRequested?.Invoke();
                else if (PrevRect.HasPoint(mb.Position)) Turn(-1);
                else if (NextRect.HasPoint(mb.Position)) Turn(1);
            }
            AcceptEvent();
        }
        else if (e is InputEventMouseMotion mm)
        {
            bool hot = CloseRect.HasPoint(mm.Position) || (_spread > 0 && PrevRect.HasPoint(mm.Position)) || (_spread < Spreads - 1 && NextRect.HasPoint(mm.Position));
            MouseDefaultCursorShape = hot ? CursorShape.PointingHand : CursorShape.Arrow;
        }
    }

    public override void _Draw()
    {
        DrawCover();
        // Seitenstapel unter den Seiten
        for (int i = 3; i >= 1; i--)
        {
            DrawRect(new Rect2(LeftPage.Position + new Vector2(-i * 2, i * 2), LeftPage.Size), i % 2 == 0 ? PageEdge : PageLine);
            DrawRect(new Rect2(RightPage.Position + new Vector2(i * 2, i * 2), RightPage.Size), i % 2 == 0 ? PageEdge : PageLine);
        }

        if (_from < 0)
        {
            DrawPage(2 * _spread, LeftPage, true);
            DrawPage(2 * _spread + 1, RightPage, false);
        }
        else
        {
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp(_flip, 0, 1));
            // Vorwärts: rechts liegt schon die neue Seite, das alte rechte Blatt klappt zur Mitte, dann landet die neue linke Seite.
            int oldL = 2 * _from, oldR = 2 * _from + 1, newL = 2 * _spread, newR = 2 * _spread + 1;
            if (_dir > 0)
            {
                DrawPage(oldL, LeftPage, true);
                DrawPage(newR, RightPage, false);
                if (t < 0.5f) Squeezed(oldR, RightPage, false, 1 - t * 2);
                else Squeezed(newL, LeftPage, true, t * 2 - 1);
            }
            else
            {
                DrawPage(newL, LeftPage, true);
                DrawPage(oldR, RightPage, false);
                if (t < 0.5f) Squeezed(oldL, LeftPage, true, 1 - t * 2);
                else Squeezed(newR, RightPage, false, t * 2 - 1);
            }
        }
        DrawSpine();
    }

    /// <summary>Seite, zur Buchmitte hin gestaucht (Blatt im Umklappen).</summary>
    void Squeezed(int set, Rect2 rect, bool left, float sx)
    {
        if (sx <= 0.02f) return;
        float xs = SpineX;
        DrawSetTransformMatrix(new Transform2D(new Vector2(sx, 0), new Vector2(0, 1), new Vector2(xs * (1 - sx), 0)));
        DrawPage(set, rect, left);
        // Schatten auf dem umklappenden Blatt
        DrawRect(rect, new Color(0, 0, 0, 0.25f * (1 - sx)));
        DrawSetTransformMatrix(Transform2D.Identity);
    }

    void DrawCover()
    {
        var r = new Rect2(Vector2.Zero, BookSize);
        DrawRect(new Rect2(r.Position + new Vector2(6, 8), r.Size), new Color(0, 0, 0, 0.35f)); // Schatten
        DrawRect(r, LeatherDark);
        DrawRect(r.Grow(-3), Leather);
        DrawRect(new Rect2(3, 3, BookSize.X - 6, 3), LeatherHi);
        // Naht rundherum
        var s = r.Grow(-12);
        for (float x = s.Position.X; x < s.End.X - 6; x += 12)
        {
            DrawRect(new Rect2(x, s.Position.Y, 6, 2), Stitch);
            DrawRect(new Rect2(x, s.End.Y - 2, 6, 2), Stitch);
        }
        for (float y = s.Position.Y + 8; y < s.End.Y - 8; y += 12)
        {
            DrawRect(new Rect2(s.Position.X, y, 2, 6), Stitch);
            DrawRect(new Rect2(s.End.X - 2, y, 2, 6), Stitch);
        }
        // Messingecken
        foreach (var c in new[] { Vector2.Zero, new Vector2(BookSize.X, 0), new Vector2(0, BookSize.Y), BookSize })
        {
            var d = new Vector2(c.X == 0 ? 1 : -1, c.Y == 0 ? 1 : -1);
            for (int i = 0; i < 18; i += 3)
            {
                float len = 22 - i;
                DrawRect(Norm(new Rect2(c + new Vector2(0, i) * d, new Vector2(len, 3) * d)), i == 0 ? BrassHi : Brass);
                DrawRect(Norm(new Rect2(c + new Vector2(i, 0) * d, new Vector2(3, len) * d)), i == 0 ? BrassHi : Brass);
            }
        }
        // Titel oben, Legende unten in Goldprägung
        var font = PfandTheme.Body;
        string title = $"KRONKORKEN-ALBUM   {_found} / {CapData.Count}";
        float tw = font.GetStringSize(title, HorizontalAlignment.Left, -1, 26).X;
        DrawRect(new Rect2((BookSize.X - tw) / 2 - 12, 6, tw + 24, 26), Leather);   // Naht hinter dem Titel aussparen
        DrawString(font, new Vector2(0, 27), title, HorizontalAlignment.Center, BookSize.X, 26, Stitch);
        float lx = 0;
        string[] parts = new string[CapData.TierNames.Length];
        for (int t = 0; t < parts.Length; t++) { parts[t] = CapData.TierNames[t]; lx += font.GetStringSize(parts[t], HorizontalAlignment.Left, -1, 20).X + 34; }
        float x0 = (BookSize.X - lx) / 2, y0 = BookSize.Y - 12;
        DrawRect(new Rect2(x0 - 12, BookSize.Y - 32, lx + 14, 26), Leather);
        for (int t = 0; t < parts.Length; t++)
        {
            DrawRect(new Rect2(x0, y0 - 13, 12, 12), Stitch);
            DrawRect(new Rect2(x0 + 2, y0 - 11, 8, 8), TierColor(t));
            DrawString(font, new Vector2(x0 + 18, y0), parts[t], HorizontalAlignment.Left, -1, 20, Stitch);
            x0 += font.GetStringSize(parts[t], HorizontalAlignment.Left, -1, 20).X + 34;
        }
        // Schließen-Knopf
        var cr = CloseRect;
        DrawRect(cr, LeatherDark);
        DrawRect(cr.Grow(-2), Brass);
        DrawRect(new Rect2(cr.Position + new Vector2(2, 2), cr.Size.X - 4, 2), BrassHi);
        for (int i = 0; i < 6; i++)
        {
            DrawRect(new Rect2(cr.Position + new Vector2(8 + i * 2, 8 + i * 2), 3, 3), LeatherDark);
            DrawRect(new Rect2(cr.Position + new Vector2(19 - i * 2, 8 + i * 2), 3, 3), LeatherDark);
        }
    }

    static Rect2 Norm(Rect2 r) => r.Abs();

    void DrawSpine()
    {
        float x = Margin + PageW;
        DrawRect(new Rect2(x, PageTop - 6, Spine, PageH + 12), LeatherDark);
        DrawRect(new Rect2(x + 6, PageTop - 6, Spine - 12, PageH + 12), Leather);
        DrawRect(new Rect2(x + Spine / 2 - 1, PageTop - 6, 2, PageH + 12), LeatherDark);
        // Schatten der Seiten zur Bindung hin
        for (int i = 0; i < 5; i++)
        {
            var c = new Color(0, 0, 0, 0.12f - i * 0.022f);
            DrawRect(new Rect2(x - 4 * (i + 1), PageTop, 4, PageH), c);
            DrawRect(new Rect2(x + Spine + 4 * i, PageTop, 4, PageH), c);
        }
    }

    void DrawPage(int setIndex, Rect2 r, bool left)
    {
        DrawRect(r, Page);
        // feine Linien am Rand wie bei Albumkarton
        DrawRect(new Rect2(r.Position + new Vector2(10, 10), r.Size.X - 20, 1), PageLine);
        DrawRect(new Rect2(r.Position + new Vector2(10, r.Size.Y - 11), r.Size.X - 20, 1), PageLine);
        if (setIndex >= _sets.Length)
        {
            DrawString(PfandTheme.Body, r.Position + new Vector2(0, r.Size.Y / 2), "Platz für mehr ...", HorizontalAlignment.Center, r.Size.X, 24, PageLine);
            PageNumber(setIndex, r, left);
            return;
        }

        string set = _sets[setIndex];
        var font = PfandTheme.Body;
        var gs = GameState.I;
        int inSet = 0, foundInSet = 0;
        var idx = new System.Collections.Generic.List<int>();
        for (int i = 0; i < CapData.Count; i++)
        {
            if (CapData.All[i].Set != set) continue;
            idx.Add(i);
            inSet++;
            if (gs.Caps.TryGetValue(i, out var n) && n > 0) foundInSet++;
        }
        // Überall gleiche Reihenfolge: häufig zuerst, legendär zuletzt (stabil, sonst wie angelegt)
        idx = idx.OrderBy(CapData.Tier).ThenBy(i => i).ToList();

        DrawString(font, r.Position + new Vector2(0, 52), set, HorizontalAlignment.Center, r.Size.X, 36, Palette.InkSoft);
        DrawString(font, r.Position + new Vector2(0, 78), $"{foundInSet} / {inSet}", HorizontalAlignment.Center, r.Size.X, 22, foundInSet == inSet ? Color.FromHtml("#3f7a3a") : Palette.Muted);
        DrawRect(new Rect2(r.Position.X + r.Size.X / 2 - 70, r.Position.Y + 88, 140, 2), PageLine);

        // 10 Mulden: 2 Spalten × 5 Reihen, Name rechts neben dem Deckel
        const float cellW = 204, cellH = 88;
        var origin = r.Position + new Vector2((r.Size.X - 2 * cellW) / 2, 100);
        for (int k = 0; k < idx.Count && k < 10; k++)
        {
            int i = idx[k];
            int n = gs.Caps.TryGetValue(i, out var cnt) ? cnt : 0;
            var cell = origin + new Vector2(k % 2 * cellW, k / 2 * cellH);
            var c = cell + new Vector2(44, cellH / 2);
            var tier = TierColor(CapData.Tier(i));
            // Mulde mit Seltenheitsring
            DrawCircle(c + new Vector2(1, 2), 41, PageEdge);
            DrawCircle(c, 41, n > 0 ? tier : new Color(tier, 0.35f));
            DrawCircle(c, 38, Page.Darkened(0.09f));
            DrawArc(c, 37, Mathf.Pi, Mathf.Pi * 1.5f, 10, new Color(0, 0, 0, 0.12f), 3);
            Sprites.Cap(this, c - new Vector2(36, 36), 6, CapData.All[i], n > 0);
            var name = CapData.All[i].Name;
            int fs = font.GetStringSize(name, HorizontalAlignment.Left, -1, 22).X > cellW - 96 ? 18 : 22;
            DrawString(font, cell + new Vector2(92, cellH / 2 + 4), name, HorizontalAlignment.Left, cellW - 94, fs, n > 0 ? Palette.Ink : Palette.Muted);
            if (n > 1) DrawString(font, cell + new Vector2(92, cellH / 2 + 26), $"×{n}", HorizontalAlignment.Left, -1, 20, Palette.Muted);
        }
        PageNumber(setIndex, r, left);
    }

    void PageNumber(int setIndex, Rect2 r, bool left)
    {
        var font = PfandTheme.Body;
        DrawString(font, new Vector2(r.Position.X, r.End.Y - 20), (setIndex + 1).ToString(), HorizontalAlignment.Center, r.Size.X, 20, Palette.Muted);
        // Blätter-Ecke
        bool can = left ? (_spread > 0 && _from < 0) : (_spread < Spreads - 1 && _from < 0);
        if (!can) return;
        var b = left ? PrevRect : NextRect;
        var col = Palette.InkSoft;
        var tip = left ? new Vector2(b.Position.X + 8, b.GetCenter().Y) : new Vector2(b.End.X - 8, b.GetCenter().Y);
        float dir = left ? 1 : -1;
        for (int i = 0; i < 9; i++)
            DrawRect(new Rect2(tip.X + dir * i * 2 - (left ? 0 : 2), tip.Y - i - 1, 2, 2 * i + 2), col);
        DrawRect(new Rect2(left ? tip.X + 18 : tip.X - 30, tip.Y - 3, 12, 6), col);
    }

    static Color TierColor(int tier) => tier switch
    {
        1 => Color.FromHtml("#2f6fae"),
        2 => Color.FromHtml("#7a4fb0"),
        3 => Color.FromHtml("#c99a1e"),
        _ => Palette.InkSoft,
    };
}

/// <summary>Einzelner Kronkorken als Control (z. B. im Skilltree-Kopf).</summary>
public partial class CapIcon : Control
{
    public int Index;
    public bool Found;

    public override void _Draw()
        => Sprites.Cap(this, Vector2.Zero, Mathf.Floor(Size.X / 24f) * 2, CapData.All[Index], Found);
}
