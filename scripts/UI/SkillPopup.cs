using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;
using Pfand.Core;
using Pfand.Park;

namespace Pfand.UI;

/// <summary>
/// Der Skilltree als Overlay im Holz-Stil des Hauptbildschirms: nur der Baum, Infos kommen beim Drüberfahren.
/// </summary>
public partial class SkillPopup : Control
{
    SkillCanvas _canvas;
    Label _balance;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;

        var dim = new ColorRect { Color = new Color(Wood.Shadow, 0.6f) };
        dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        dim.GuiInput += e => { if (e is InputEventMouseButton mb && mb.Pressed) Close(); };
        AddChild(dim);

        var frame = new PanelContainer();
        frame.AddThemeStyleboxOverride("panel", PfandTheme.WoodPanel(14));
        frame.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        frame.OffsetLeft = 40; frame.OffsetTop = 30; frame.OffsetRight = -40; frame.OffsetBottom = -30;
        AddChild(frame);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 10);
        frame.AddChild(col);

        var head = new HBoxContainer();
        head.AddThemeConstantOverride("separation", 14);
        var title = PfandTheme.Text("Skilltree", 36, Wood.Ink);
        title.VerticalAlignment = VerticalAlignment.Center;
        head.AddChild(title);
        var hint = PfandTheme.Text("Klick: lernen   Ziehen: verschieben", 22, Wood.Muted);
        hint.VerticalAlignment = VerticalAlignment.Center;
        head.AddChild(hint);
        head.AddChild(PfandTheme.Spacer());
        head.AddChild(new CapIcon { Index = 0, Found = true, CustomMinimumSize = new Vector2(24, 24), SizeFlagsVertical = SizeFlags.ShrinkCenter });
        _balance = PfandTheme.Text("", 30, Wood.Accent);
        _balance.VerticalAlignment = VerticalAlignment.Center;
        head.AddChild(_balance);
        var close = new Button { Text = "SCHLIESSEN", CustomMinimumSize = new Vector2(150, 40) };
        close.Pressed += Close;
        head.AddChild(close);
        col.AddChild(head);

        var canvasFrame = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        canvasFrame.AddThemeStyleboxOverride("panel", new SlotBox(3));
        _canvas = new SkillCanvas();
        canvasFrame.AddChild(_canvas);
        col.AddChild(canvasFrame);

        GameState.I.Changed += Refresh;
    }

    public override void _ExitTree() => GameState.I.Changed -= Refresh;

    void Refresh()
    {
        if (!Visible) return;
        _balance.Text = $"{GameState.I.Kronkorken} Kronkorken";
        _canvas.QueueRedraw();
    }

    public void Open()
    {
        Visible = true;
        _canvas.CallDeferred(SkillCanvas.MethodName.CenterOnRoot);
        Refresh();
    }

    public void Close() => Visible = false;

    public override void _UnhandledInput(InputEvent e)
    {
        if (Visible && e is InputEventKey k && k.Pressed && k.Keycode == Key.Escape) { Close(); GetViewport().SetInputAsHandled(); }
    }
}

/// <summary>
/// Zeichnet den Baum: kleine Knoten mit Symbol und Zusatzzeichen (mehr, schneller, mehr wert, billiger),
/// dünne Linien, Zustand am Rahmen. Ein Klick lernt, Ziehen verschiebt.
/// </summary>
public partial class SkillCanvas : Control
{
    public enum NodeState { Hidden, Teaser, Available, Learned }

    const float Spacing = 96, NodeSize = 46;

    static readonly Color Back = Color.FromHtml("#2e2014");
    static readonly Color BackLine = Color.FromHtml("#241810");
    static readonly Color BackLight = Color.FromHtml("#3a2a1c");
    static readonly Color Edge = Color.FromHtml("#100c08");
    static readonly Color Inside = Color.FromHtml("#1c1812");
    static readonly Color RingGot = Color.FromHtml("#f2c45a");
    static readonly Color RingGotHi = Color.FromHtml("#fff0b0");
    static readonly Color RingOpen = Color.FromHtml("#e8dcc0");
    static readonly Color RingPoor = Color.FromHtml("#8a7a60");
    static readonly Color RingDark = Color.FromHtml("#4a4438");
    static readonly Color LineGot = Color.FromHtml("#d8cdb4");
    static readonly Color LineOpen = Color.FromHtml("#8a7e66");
    static readonly Color BadgeGreen = Color.FromHtml("#7ad05a");
    static readonly Color GoodInk = Color.FromHtml("#9ad870");
    static readonly Color BadInk = Color.FromHtml("#f08a6a");

    Vector2 _offset;
    bool _pressed, _dragging;
    Vector2 _pressPos, _mouse;
    string _hover, _flash;
    float _time, _flashT;

    public override void _Ready()
    {
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Stop;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
    }

    /// <summary>Wurzel etwas unter die Mitte, weil der Baum nach oben weiter wächst.</summary>
    public void CenterOnRoot() { _offset = new Vector2(0, Spacing / 2); QueueRedraw(); }

    public static NodeState StateOf(SkillDef d)
    {
        var gs = GameState.I;
        if (gs.HasSkill(d.Id)) return NodeState.Learned;
        if (gs.SkillAvailable(d)) return NodeState.Available;
        // Der nächste Ast an der Wurzel zeigt sich schon mit Schloss, damit man weiß, worauf man hinarbeitet
        if (gs.NextGatedBranch == d) return NodeState.Teaser;
        return NodeState.Hidden;
    }

    /// <summary>Zusatzzeichen am Knoten: wie der Skill etwas verbessert.</summary>
    enum Badge { None, Plus, Up, Fast, Down }

    static readonly HashSet<string> UpIds = new() { "wert", "bier_wert", "glas_wert", "plastik_wert", "dose_wert", "wert_koenig" };
    static readonly HashSet<string> FastIds = new() { "abgabe", "durst", "flink", "tischrunde", "tt_sieger", "imbiss_kuehl", "grill_kuehl", "ev_cooldown" };

    static Badge BadgeOf(SkillDef d)
    {
        if (UpIds.Contains(d.Id)) return Badge.Up;
        if (FastIds.Contains(d.Id)) return Badge.Fast;
        if (d.Id == "ev_sponsor") return Badge.Down;
        return d.MaxLevel > 1 ? Badge.Plus : Badge.None;
    }

    /// <summary>Mehrere Ausbauten hängen direkt am selben Knoten: lange Linien zuerst zeichnen, damit die kurzen obenauf liegen.</summary>
    static float LineLength(SkillDef d)
    {
        if (d.Id == SkillDefs.Root) return 0;
        var p = SkillDefs.Get(d.Parent ?? SkillDefs.Root);
        return new Vector2(d.Pos.X - p.Pos.X, d.Pos.Y - p.Pos.Y).Length();
    }

    Vector2 Center(SkillDef d) => (Size / 2 + _offset + new Vector2(d.Pos.X, d.Pos.Y) * Spacing).Round();

    SkillDef NodeAt(Vector2 p)
    {
        foreach (var d in SkillDefs.All)
            if (StateOf(d) != NodeState.Hidden && new Rect2(Center(d) - Vector2.One * NodeSize / 2, Vector2.One * NodeSize).Grow(4).HasPoint(p))
                return d;
        return null;
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree()) return;
        _time += (float)delta;
        if (_flashT > 0) _flashT -= (float)delta;
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex is MouseButton.Left or MouseButton.Right or MouseButton.Middle)
            {
                if (mb.Pressed) { _pressed = true; _dragging = false; _pressPos = mb.Position; }
                else
                {
                    // Klick ohne Ziehen auf einen Knoten: lernen
                    if (!_dragging && mb.ButtonIndex == MouseButton.Left && NodeAt(mb.Position) is { } d
                        && StateOf(d) != NodeState.Teaser && GameState.I.TryBuySkill(d))
                    {
                        _flash = d.Id;
                        _flashT = 0.3f;
                    }
                    _pressed = false;
                    _dragging = false;   // sonst bleibt der Tooltip bis zum nächsten Klick weg
                    _hover = NodeAt(mb.Position)?.Id;
                }
            }
        }
        else if (e is InputEventMouseMotion mm)
        {
            if (_pressed)
            {
                if (!_dragging && mm.Position.DistanceTo(_pressPos) > 5) _dragging = true;
                if (_dragging)
                {
                    _offset += mm.Relative;
                    _offset = _offset.Clamp(-Vector2.One * Spacing * 8, Vector2.One * Spacing * 8);
                }
            }
            var h = NodeAt(mm.Position);
            _hover = h?.Id;
            _mouse = mm.Position;
            MouseDefaultCursorShape = _dragging ? CursorShape.Drag : h != null ? CursorShape.PointingHand : CursorShape.Arrow;
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationMouseExit) _hover = null;
    }

    public override void _Draw()
    {
        var gs = GameState.I;
        DrawBackground();

        // Linien zuerst, damit die Knoten darüber liegen
        var lines = new List<SkillDef>(SkillDefs.All);
        lines.Sort((x, y) => LineLength(y).CompareTo(LineLength(x)));
        foreach (var d in lines)
        {
            if (d.Id == SkillDefs.Root) continue;
            var state = StateOf(d);
            if (state == NodeState.Hidden) continue;
            var a = Center(SkillDefs.Get(d.Parent ?? SkillDefs.Root));
            var b = Center(d);
            var c = state == NodeState.Learned ? LineGot : state == NodeState.Available ? LineOpen : RingDark;
            DrawLine(a, b, c, 4);
        }

        foreach (var d in SkillDefs.All)
        {
            var state = StateOf(d);
            if (state != NodeState.Hidden) DrawNode(d, state, gs);
        }

        if (_hover != null && !_dragging && SkillDefs.Get(_hover) is { } hd) DrawTooltip(hd, gs);
    }

    /// <summary>Dunkles Holz, Bretter wandern beim Verschieben mit.</summary>
    void DrawBackground()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), Back);
        const float plank = 30;
        float start = Mathf.PosMod(_offset.Y + Size.Y / 2, plank);
        for (float y = start; y < Size.Y; y += plank)
        {
            DrawRect(new Rect2(0, Mathf.Round(y), Size.X, 3), BackLine);
            DrawRect(new Rect2(0, Mathf.Round(y) + 3, Size.X, 3), BackLight);
        }
        var rng = new Random(3);
        for (int i = 0; i < 260; i++)
        {
            var p = new Vector2(rng.Next(-1200, 1200), rng.Next(-900, 900)) + Size / 2 + _offset;
            p = new Vector2(p.X - Mathf.PosMod(p.X, 3), p.Y - Mathf.PosMod(p.Y, 3));
            DrawRect(new Rect2(p, 3 * rng.Next(2, 7), 3), rng.Next(2) == 0 ? BackLight : BackLine);
        }
    }

    void DrawNode(SkillDef d, NodeState state, GameState gs)
    {
        var c = Center(d);
        var r = new Rect2(c - Vector2.One * NodeSize / 2, Vector2.One * NodeSize);
        bool root = d.Id == SkillDefs.Root;
        int lvl = gs.SkillLevel(d.Id);
        bool hover = _hover == d.Id;
        const float p = 3;

        Color ring = state switch
        {
            NodeState.Learned => RingGot,
            NodeState.Available => gs.CanBuySkill(d) ? RingOpen : RingPoor,
            _ => RingDark,
        };
        if (root) ring = RingGot;
        if (_flash == d.Id && _flashT > 0) ring = Colors.White;

        if (hover) DrawRect(r.Grow(2 * p), new Color(RingOpen, 0.25f));
        DrawRect(r, Edge);
        DrawRect(r.Grow(-p), ring);
        if (state == NodeState.Learned || root) DrawRect(new Rect2(r.Position + new Vector2(p, p), r.Size.X - 2 * p, p), RingGotHi);
        DrawRect(r.Grow(-2 * p), Inside);

        // Symbol (8×8)
        if (state == NodeState.Teaser)
        {
            DrawString(PfandTheme.Body, new Vector2(r.Position.X, c.Y + 12), "?", HorizontalAlignment.Center, NodeSize, 36, RingDark.Lightened(0.3f));
            DrawLock(r);
            return;
        }
        var ink = state == NodeState.Learned || root ? Wood.Ink : Wood.Ink.Darkened(0.15f);
        var light = d.Branch.Color.Lightened(0.15f);
        var o = c - new Vector2(4 * p, 4 * p);
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
            {
                char ch = d.Icon[y][x];
                if (ch == '#') DrawRect(new Rect2(o.X + x * p, o.Y + y * p, p, p), ink);
                else if (ch == 'o') DrawRect(new Rect2(o.X + x * p, o.Y + y * p, p, p), light);
            }

        if (!root) DrawBadge(r, BadgeOf(d));

        // Stufe als kleine Zahl unten links, sobald etwas gelernt ist
        if (d.MaxLevel > 1 && lvl > 0)
        {
            bool maxed = lvl >= d.MaxLevel;
            string t = maxed ? "max" : lvl.ToString();
            float w = PfandTheme.Body.GetStringSize(t, HorizontalAlignment.Left, -1, 18).X + 6;
            var box = new Rect2(r.Position.X - p, r.End.Y - 6, Mathf.Round(w), 15);
            DrawRect(box, Edge);
            DrawString(PfandTheme.Body, new Vector2(box.Position.X + 3, box.End.Y - 2), t, HorizontalAlignment.Left, -1, 18, maxed ? GoodInk : RingGot);
        }
    }

    /// <summary>Zusatzzeichen unten rechts am Knoten, mit dunkler Kontur.</summary>
    void DrawBadge(Rect2 r, Badge b)
    {
        if (b == Badge.None) return;
        const float p = 3;
        var o = new Vector2(r.End.X - 4 * p, r.End.Y - 4 * p);
        void R(float x, float y, float w, float h, Color c) => DrawRect(new Rect2(o.X + x * p, o.Y + y * p, w * p, h * p), c);
        switch (b)
        {
            case Badge.Plus:
                R(0, 1, 5, 3, Edge); R(1, 0, 3, 5, Edge);
                R(1, 2, 3, 1, BadgeGreen); R(2, 1, 1, 3, BadgeGreen);
                break;
            case Badge.Up:
                R(1, 0, 3, 5, Edge); R(0, 1, 5, 2, Edge);
                R(2, 1, 1, 3, BadgeGreen); R(1, 2, 3, 1, BadgeGreen);
                break;
            case Badge.Down:
                R(1, 0, 3, 5, Edge); R(0, 2, 5, 2, Edge);
                R(2, 1, 1, 3, BadgeGreen); R(1, 3, 3, 1, BadgeGreen);
                break;
            case Badge.Fast:
                // zwei Pfeilspitzen nach rechts, je 5 Pixel hoch
                R(-1, -1, 7, 7, Edge);
                for (int i = 0; i < 2; i++)
                {
                    float x = i * 3;
                    R(x, 0, 1, 1, BadgeGreen); R(x + 1, 1, 1, 1, BadgeGreen); R(x + 2, 2, 1, 1, BadgeGreen);
                    R(x + 1, 3, 1, 1, BadgeGreen); R(x, 4, 1, 1, BadgeGreen);
                }
                break;
        }
    }

    void DrawLock(Rect2 r)
    {
        const float p = 3;
        var o = new Vector2(r.End.X - 4 * p, r.End.Y - 5 * p);
        void R(float x, float y, float w, float h, Color c) => DrawRect(new Rect2(o.X + x * p, o.Y + y * p, w * p, h * p), c);
        R(-1, -1, 6, 7, Edge);
        R(1, 0, 2, 1, RingGot); R(0, 1, 1, 2, RingGot); R(3, 1, 1, 2, RingGot);
        R(0, 2, 4, 3, RingGot);
        R(1.5f, 3, 1, 1, Edge);
    }

    const string CompareMark = "\u0001";
    static readonly Regex Number = new(@"[+-]?\d+(?:[.,]\d+)?(?:\s?%|\s+s\b)?");

    /// <summary>Erster Wert, der sich mit der nächsten Stufe ändert: (jetzt, danach). null bei Einmal-Skills.</summary>
    static (string from, string to)? Change(SkillDef d, int lvl)
    {
        if (d.MaxLevel <= 1) return null;
        var a = Number.Matches(d.Effect(lvl));
        var b = Number.Matches(d.Effect(lvl + 1));
        if (a.Count != b.Count) return null;
        for (int i = 0; i < a.Count; i++)
            if (a[i].Value != b[i].Value) return (a[i].Value, b[i].Value);
        return null;
    }

    /// <summary>"statt …"-Angaben fallen weg, der Pfeil zeigt den Vergleich schon.</summary>
    static string WithoutStatt(string s)
    {
        s = Regex.Replace(s, @"\s*\([^)]*statt[^)]*\)", "");
        s = Regex.Replace(s, @" statt [+-]?\d+(?:[.,]\d+)?(?:\s?%)?", "");
        return s;
    }

    /// <summary>Jetzt-Wert grau, kleiner Pixelpfeil, neuer Wert grün.</summary>
    void DrawCompare(Font font, Vector2 baseline, string from, string to)
    {
        const int size = 26;
        DrawString(font, baseline, from, HorizontalAlignment.Left, -1, size, Wood.Muted);
        float x = baseline.X + font.GetStringSize(from, HorizontalAlignment.Left, -1, size).X + 10;
        float cy = baseline.Y - 8;
        const float q = 2;
        DrawRect(new Rect2(x, cy - q / 2, 5 * q, q), RingGot);
        for (int i = 0; i < 4; i++)
        {
            float hh = (7 - 2 * i) * q;
            DrawRect(new Rect2(x + (5 + i) * q, cy - hh / 2, q, hh), RingGot);
        }
        x += 9 * q + 10;
        DrawString(font, new Vector2(x, baseline.Y), to, HorizontalAlignment.Left, -1, size, GoodInk);
    }

    /// <summary>Beschreibung am Mauszeiger im Holz-Stil: Name, Stufe, Effekt, Preis.</summary>
    void DrawTooltip(SkillDef d, GameState gs)
    {
        const float width = 290, pad = 12;
        const int size = 22;
        var font = PfandTheme.Body;
        int lvl = gs.SkillLevel(d.Id);
        bool maxed = gs.SkillMaxed(d);
        if (d.Id == SkillDefs.Root) return;

        var lines = new List<(string text, Color color)>();
        string title = d.Name;
        if (StateOf(d) == NodeState.Teaser) title = "NEUER AST";
        else
        {
            if (d.MaxLevel > 1) lines.Add(($"Stufe {lvl}/{d.MaxLevel}", Wood.Muted));
            var change = maxed ? null : Change(d, lvl);
            string effect = d.Effect(maxed ? lvl : lvl + 1);
            if (change != null) effect = WithoutStatt(effect);
            lines.Add((effect, Wood.Ink));
            if (change != null) lines.Add((CompareMark + change.Value.from + CompareMark + change.Value.to, Wood.Ink));
            if (maxed) lines.Add(("Voll ausgebaut", GoodInk));
            else
            {
                int cost = gs.NextSkillCost(d);
                lines.Add(($"{cost} Kronkorken", gs.Kronkorken >= cost ? GoodInk : BadInk));
            }
        }

        float inner = width - 2 * pad;
        float h = pad + 22;
        var heights = new float[lines.Count];
        for (int i = 0; i < lines.Count; i++)
        {
            heights[i] = lines[i].text.StartsWith(CompareMark, StringComparison.Ordinal) ? 30
                : font.GetMultilineStringSize(lines[i].text, HorizontalAlignment.Left, inner, size).Y;
            h += heights[i] + 2;
        }
        h += pad - 2;

        var pos = _mouse + new Vector2(18, 18);
        if (pos.X + width > Size.X - 6) pos.X = _mouse.X - width - 12;
        if (pos.Y + h > Size.Y - 6) pos.Y = Size.Y - h - 6;
        pos = pos.Max(new Vector2(6, 6)).Round();

        const float p = 3;
        var r = new Rect2(pos, width, h);
        DrawRect(new Rect2(r.Position + new Vector2(p, p), r.Size), Wood.Shadow);
        DrawRect(r, Edge);
        var ir = r.Grow(-p);
        DrawRect(ir, Wood.Plank);
        DrawRect(new Rect2(ir.Position, ir.Size.X, p), RingGot);
        DrawRect(new Rect2(ir.Position.X, ir.End.Y - p, ir.Size.X, p), Wood.GrainDark);

        float y = pos.Y + pad + 18;
        DrawString(font, new Vector2(pos.X + pad, y), title, HorizontalAlignment.Left, inner, 26, RingGot);
        y += 6;
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].text.StartsWith(CompareMark, StringComparison.Ordinal))
            {
                var parts = lines[i].text.Split(CompareMark);
                DrawCompare(font, new Vector2(pos.X + pad, y + 24), parts[1], parts[2]);
                y += heights[i] + 2;
                continue;
            }
            DrawMultilineString(font, new Vector2(pos.X + pad, y + size * 0.8f), lines[i].text, HorizontalAlignment.Left, inner, size, -1, lines[i].color);
            y += heights[i] + 2;
        }
    }
}
