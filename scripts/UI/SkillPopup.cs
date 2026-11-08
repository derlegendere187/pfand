using System;
using Godot;
using Pfand.Core;
using Pfand.Park;

namespace Pfand.UI;

/// <summary>
/// Der Skilltree als Overlay: links die Wurzel-Fläche (verschiebbar), rechts Infos zum gewählten Knoten.
/// </summary>
public partial class SkillPopup : Control
{
    SkillCanvas _canvas;
    Label _balance, _name, _branch, _level, _nowTitle, _now, _nextTitle, _next;
    Button _buy;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;

        var dim = new ColorRect { Color = new Color(Palette.Ink, 0.55f) };
        dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        dim.GuiInput += e => { if (e is InputEventMouseButton mb && mb.Pressed) Close(); };
        AddChild(dim);

        var frame = PfandTheme.Panel(Palette.Panel, null, 16);
        frame.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        frame.OffsetLeft = 40; frame.OffsetTop = 30; frame.OffsetRight = -40; frame.OffsetBottom = -30;
        AddChild(frame);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 12);
        frame.AddChild(col);

        var head = new HBoxContainer();
        head.AddThemeConstantOverride("separation", 10);
        head.AddChild(PfandTheme.Title("SKILLTREE", 16));
        head.AddChild(PfandTheme.Spacer());
        head.AddChild(new CapIcon { Index = 0, Found = true, CustomMinimumSize = new Vector2(24, 24), SizeFlagsVertical = SizeFlags.ShrinkCenter });
        _balance = PfandTheme.Text("", 28, Palette.MoneyText);
        head.AddChild(_balance);
        var close = new Button { Text = "SCHLIESSEN", CustomMinimumSize = new Vector2(150, 40) };
        close.Pressed += Close;
        head.AddChild(close);
        col.AddChild(head);

        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 14);
        col.AddChild(body);

        var canvasFrame = PfandTheme.Panel(Palette.Border, null, 4);
        canvasFrame.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _canvas = new SkillCanvas();
        _canvas.SelectionChanged += RefreshInfo;
        canvasFrame.AddChild(_canvas);
        body.AddChild(canvasFrame);

        // Info-Spalte
        var info = PfandTheme.Panel(Palette.PanelLight, Palette.Border, 12);
        info.CustomMinimumSize = new Vector2(290, 0);
        var icol = new VBoxContainer();
        icol.AddThemeConstantOverride("separation", 6);
        info.AddChild(icol);
        _name = PfandTheme.Title("", 8);
        _name.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _branch = PfandTheme.Text("", 20, Palette.Muted);
        _level = PfandTheme.Text("", 24);
        _nowTitle = PfandTheme.Title("JETZT", 8, Palette.Muted);
        _now = WrapText();
        _nextTitle = PfandTheme.Title("NÄCHSTE STUFE", 8, Palette.Muted);
        _next = WrapText();
        _buy = new Button { CustomMinimumSize = new Vector2(0, 44) };
        _buy.AddThemeFontSizeOverride("font_size", 24);
        _buy.Pressed += () => { if (SkillDefs.Get(_canvas.Selected) is { } d) GameState.I.TryBuySkill(d); };
        foreach (var c in new Control[] { _name, _branch, _level, _nowTitle, _now, _nextTitle, _next })
            icol.AddChild(c);
        icol.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });
        var hint = PfandTheme.Text("Ziehen verschiebt den Baum. Doppelklick lernt. Jeder gelernte Knoten lässt neue Äste wachsen.", 20, Palette.Muted);
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        icol.AddChild(hint);
        icol.AddChild(_buy);
        body.AddChild(info);

        GameState.I.Changed += Refresh;
    }

    static Label WrapText()
    {
        var l = PfandTheme.Text("", 22);
        l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        return l;
    }

    public override void _ExitTree() => GameState.I.Changed -= Refresh;

    void Refresh()
    {
        if (!Visible) return;
        _balance.Text = $"{GameState.I.Kronkorken} Kronkorken";
        _canvas.QueueRedraw();
        RefreshInfo();
    }

    void RefreshInfo()
    {
        var gs = GameState.I;
        var d = SkillDefs.Get(_canvas.Selected);
        if (d == null) return;
        var state = SkillCanvas.StateOf(d);
        int lvl = gs.SkillLevel(d.Id);
        bool isRoot = d.Id == SkillDefs.Root;

        if (state == SkillCanvas.NodeState.Teaser)
        {
            _name.Text = "???";
            _name.AddThemeColorOverride("font_color", Palette.Muted);
            _branch.Text = d.Branch.Name;
            _level.Text = "";
            _nowTitle.Visible = _now.Visible = false;
            _nextTitle.Visible = _next.Visible = true;
            _next.Text = "Noch unbekannt. Lerne zuerst " + SkillDefs.Get(d.Parent).Name + ".";
            _buy.Text = "UNBEKANNT";
            _buy.Disabled = true;
            return;
        }

        _name.Text = d.Name;
        _name.AddThemeColorOverride("font_color", d.Branch.Color.Darkened(0.2f));
        _branch.Text = isRoot ? "Wurzel" : "Ast: " + d.Branch.Name;
        _level.Text = isRoot ? "" : $"Stufe {lvl} / {d.MaxLevel}";
        _nowTitle.Visible = _now.Visible = lvl > 0 || isRoot;
        _now.Text = d.Effect(Math.Max(1, lvl));
        bool maxed = gs.SkillMaxed(d);
        _nextTitle.Visible = _next.Visible = !maxed && !isRoot;
        if (!maxed && !isRoot) _next.Text = d.Effect(lvl + 1);

        if (isRoot) { _buy.Text = "START"; _buy.Disabled = true; }
        else if (maxed) { _buy.Text = "MAXIMUM"; _buy.Disabled = true; }
        else if (!gs.SkillAvailable(d)) { _buy.Text = "ERST: " + SkillDefs.Get(d.Parent).Name; _buy.Disabled = true; }
        else { _buy.Text = $"LERNEN · {gs.NextSkillCost(d)} KK"; _buy.Disabled = !gs.CanBuySkill(d); }
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

/// <summary>Zeichnet den Baum: Knoten auf einem Raster, Linien vom Eltern- zum Kindknoten.</summary>
public partial class SkillCanvas : Control
{
    public enum NodeState { Hidden, Teaser, Available, Learned }

    const float Spacing = 104, NodeSize = 60;

    public string Selected = SkillDefs.Root;
    public event Action SelectionChanged;

    Vector2 _offset;
    bool _pressed, _dragging;
    Vector2 _pressPos;
    string _hover;
    float _time;

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
        // Teaser: Elternknoten ist schon sichtbar (lernbar), aber noch nicht gelernt
        var parent = SkillDefs.Get(d.Parent ?? SkillDefs.Root);
        return parent != null && gs.SkillAvailable(parent) ? NodeState.Teaser : NodeState.Hidden;
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
        QueueRedraw(); // für das Pulsieren lernbarer Knoten
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton mb)
        {
            if (mb.Pressed && mb.DoubleClick && mb.ButtonIndex == MouseButton.Left)
            {
                if (NodeAt(mb.Position) is { } d) { Select(d.Id); GameState.I.TryBuySkill(d); }
                return;
            }
            if (mb.ButtonIndex is MouseButton.Left or MouseButton.Right or MouseButton.Middle)
            {
                if (mb.Pressed) { _pressed = true; _dragging = false; _pressPos = mb.Position; }
                else
                {
                    if (!_dragging && mb.ButtonIndex == MouseButton.Left && NodeAt(mb.Position) is { } d) Select(d.Id);
                    _pressed = false;
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
                    _offset = _offset.Clamp(-Vector2.One * Spacing * 5, Vector2.One * Spacing * 5);
                }
            }
            var h = NodeAt(mm.Position);
            _hover = h?.Id;
            MouseDefaultCursorShape = _dragging ? CursorShape.Drag : h != null ? CursorShape.PointingHand : CursorShape.Arrow;
        }
    }

    void Select(string id)
    {
        Selected = id;
        SelectionChanged?.Invoke();
        QueueRedraw();
    }

    public override void _Draw()
    {
        var gs = GameState.I;
        DrawRect(new Rect2(Vector2.Zero, Size), Color.FromHtml("#dccaa4"));
        // Erdkrümel im Hintergrund, wandern beim Verschieben mit
        var rng = new Random(3);
        for (int i = 0; i < 160; i++)
        {
            var p = new Vector2(rng.Next(-900, 900), rng.Next(-700, 700)) + Size / 2 + _offset;
            var c = rng.Next(2) == 0 ? Color.FromHtml("#cdb98f") : Color.FromHtml("#e6d7b8");
            DrawRect(new Rect2(p.Floor(), rng.Next(2) == 0 ? 4 : 8, 4), c);
        }

        // Wurzeln (Linien) zuerst, damit die Knoten darüber liegen
        foreach (var d in SkillDefs.All)
        {
            if (d.Id == SkillDefs.Root) continue;
            var state = StateOf(d);
            if (state == NodeState.Hidden) continue;
            var parent = SkillDefs.Get(d.Parent ?? SkillDefs.Root);
            var a = Center(parent);
            var b = Center(d);
            Color c; float w;
            switch (state)
            {
                case NodeState.Learned: c = d.Branch.Color; w = 8; break;
                case NodeState.Available: c = new Color(d.Branch.Color, 0.55f); w = 6; break;
                default: c = new Color(Palette.Muted, 0.45f); w = 4; break;
            }
            // gerade Nachbarn als Pixel-Balken, schräge als diagonale Wurzel
            if (a.X == b.X || a.Y == b.Y) Segment(a, b, c, w);
            else DrawLine(a, b, c, w);
        }

        foreach (var d in SkillDefs.All)
        {
            var state = StateOf(d);
            if (state != NodeState.Hidden) DrawNode(d, state, gs);
        }
    }

    void Segment(Vector2 a, Vector2 b, Color c, float w)
    {
        if (a == b) return;
        var r = new Rect2(new Vector2(Mathf.Min(a.X, b.X), Mathf.Min(a.Y, b.Y)), (b - a).Abs()).Grow(w / 2);
        DrawRect(r, c);
    }

    void DrawNode(SkillDef d, NodeState state, GameState gs)
    {
        var c = Center(d);
        var r = new Rect2(c - Vector2.One * NodeSize / 2, Vector2.One * NodeSize);
        bool root = d.Id == SkillDefs.Root;
        var col = d.Branch.Color;

        if (Selected == d.Id) DrawRect(r.Grow(8), Palette.Ink);
        else if (_hover == d.Id) DrawRect(r.Grow(6), new Color(Palette.Ink, 0.35f));

        Color border, fill, ink, light;
        switch (state)
        {
            case NodeState.Learned:
                border = Palette.Ink; fill = col; ink = Palette.PanelLight; light = col.Darkened(0.35f);
                break;
            case NodeState.Available:
                border = col; fill = Palette.PanelLight; ink = col; light = Palette.Paper;
                // lernbar und bezahlbar: Rahmen pulsiert golden
                if (gs.CanBuySkill(d) && Mathf.Sin(_time * 5) > 0) border = Palette.Gold;
                break;
            default:
                border = Palette.Muted; fill = Color.FromHtml("#cfbf9c"); ink = Palette.Muted; light = fill;
                break;
        }
        if (root) { border = Palette.Ink; fill = Palette.Ink; ink = Palette.PanelLight; light = Palette.Gold; }

        DrawRect(r.Grow(4), border);
        DrawRect(r, fill);

        if (state == NodeState.Teaser)
        {
            DrawString(PfandTheme.Pixel, new Vector2(r.Position.X, c.Y + 12), "?", HorizontalAlignment.Center, NodeSize, 24, ink);
            return;
        }

        // 8×8-Pixel-Icon, 5 px pro Pixel
        const float p = 5;
        var o = c - new Vector2(4 * p, 4 * p) - new Vector2(0, d.MaxLevel > 1 ? 4 : 0);
        for (int y = 0; y < 8; y++)
        for (int x = 0; x < 8; x++)
        {
            char ch = d.Icon[y][x];
            if (ch == '#') DrawRect(new Rect2(o.X + x * p, o.Y + y * p, p, p), ink);
            else if (ch == 'o') DrawRect(new Rect2(o.X + x * p, o.Y + y * p, p, p), light);
        }

        // Stufen als kleine Kästchen unten im Knoten
        if (d.MaxLevel > 1)
        {
            int lvl = gs.SkillLevel(d.Id);
            float pip = 6, gap = 3;
            float total = d.MaxLevel * pip + (d.MaxLevel - 1) * gap;
            var start = new Vector2(c.X - total / 2, r.End.Y - pip - 4);
            for (int i = 0; i < d.MaxLevel; i++)
            {
                var pr = new Rect2(start + new Vector2(i * (pip + gap), 0), pip, pip);
                DrawRect(pr, state == NodeState.Learned ? Palette.Ink : col.Darkened(0.2f));
                if (i >= lvl) DrawRect(pr.Grow(-1), fill);
                else if (state == NodeState.Learned) DrawRect(pr.Grow(-1), Palette.Gold);
            }
        }
    }
}
