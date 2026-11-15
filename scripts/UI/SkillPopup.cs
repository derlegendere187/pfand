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

    const float NodeSize = 46;

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
    // Hauptzweige (führen zu weiteren Knoten) golden und dicker, Abzweigungen grün wie junge Triebe und dünner
    static readonly Color TwigGot = Color.FromHtml("#a8d86a");
    static readonly Color TwigOpen = Color.FromHtml("#5f8a3c");
    static readonly Color TrunkGot = Color.FromHtml("#e8b84a");
    static readonly Color TrunkOpen = Color.FromHtml("#9a7630");
    static HashSet<string> _trunks;

    /// <summary>
    /// Hauptzweig: von jedem Ast an der Wurzel immer weiter zum Kind mit dem größten Unterbaum.
    /// Alles, was davon seitlich abgeht, ist eine Abzweigung, auch wenn daran noch etwas hängt.
    /// </summary>
    static bool IsTrunk(SkillDef d)
    {
        if (_trunks == null)
        {
            var kids = new Dictionary<string, List<SkillDef>>();
            foreach (var s in SkillDefs.All)
            {
                if (s.Id == SkillDefs.Root) continue;
                var pid = s.Parent ?? SkillDefs.Root;
                if (!kids.TryGetValue(pid, out var l)) kids[pid] = l = new List<SkillDef>();
                l.Add(s);
            }
            var size = new Dictionary<string, int>();
            int Size(string id)
            {
                if (size.TryGetValue(id, out var n)) return n;
                n = 1;
                if (kids.TryGetValue(id, out var l)) foreach (var c in l) n += Size(c.Id);
                return size[id] = n;
            }
            _trunks = new HashSet<string>();
            foreach (var start in kids[SkillDefs.Root])
            {
                for (var cur = start; cur != null;)
                {
                    _trunks.Add(cur.Id);
                    SkillDef next = null;
                    if (kids.TryGetValue(cur.Id, out var l))
                        foreach (var c in l)
                            if (Size(c.Id) > 1 && (next == null || Size(c.Id) > Size(next.Id))) next = c;
                    cur = next;
                }
            }
        }
        return _trunks.Contains(d.Id);
    }
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
    public void CenterOnRoot() { _offset = Vector2.Zero; QueueRedraw(); }

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

    static readonly HashSet<string> UpIds = new() { "wert", "bier_wert", "glas_wert", "plastik_wert", "dose_wert", "retro_wert", "gold_wert", "wert_koenig" };
    static readonly HashSet<string> FastIds = new() { "abgabe", "durst", "flink", "tischrunde", "tt_sieger", "imbiss_kuehl", "grill_kuehl", "ev_cooldown" };

    static Badge BadgeOf(SkillDef d)
    {
        if (UpIds.Contains(d.Id)) return Badge.Up;
        if (FastIds.Contains(d.Id)) return Badge.Fast;
        if (d.Id == "ev_sponsor") return Badge.Down;
        return d.MaxLevel > 1 ? Badge.Plus : Badge.None;
    }

    /// <summary>Lange Linien zuerst zeichnen, damit die kurzen obenauf liegen.</summary>
    static float LineLength(SkillDef d)
    {
        if (d.Id == SkillDefs.Root) return 0;
        return Layout[d.Id].DistanceTo(Layout[d.Parent ?? SkillDefs.Root]);
    }

    // --- Anordnung: der Baum wächst von der Wurzel strahlenförmig nach außen ---
    // Jeder Knoten bekommt einen Winkelbereich, so groß, dass er und alle seine Unterpunkte Platz haben.
    // Unterpunkte fächern sich in diesem Bereich um ihren Elternknoten auf, jede Ebene sitzt auf einem größeren Ring.
    const float Ring1 = 190, RingStep = 130, NodeGap = 92;
    static Dictionary<string, Vector2> _layout;
    static float _extent;

    static float RingRadius(int depth) => depth <= 0 ? 0 : Ring1 + (depth - 1) * RingStep;

    /// <summary>Winkel des Knotens im alten Raster: bestimmt nur die Reihenfolge der Äste und Unterpunkte.</summary>
    static float GridAngle(SkillDef d) => Mathf.Atan2(d.Pos.Y, d.Pos.X);

    static Dictionary<string, Vector2> Layout
    {
        get
        {
            if (_layout != null) return _layout;
            var kids = new Dictionary<string, List<SkillDef>>();
            foreach (var d in SkillDefs.All)
            {
                if (d.Id == SkillDefs.Root) continue;
                var pid = d.Parent ?? SkillDefs.Root;
                if (!kids.TryGetValue(pid, out var l)) kids[pid] = l = new List<SkillDef>();
                l.Add(d);
            }
            List<SkillDef> Kids(string id) => kids.TryGetValue(id, out var l) ? l : new List<SkillDef>();

            // Benötigter Winkel: eigener Platz auf dem Ring oder die Summe der Unterpunkte, je nachdem, was größer ist
            var need = new Dictionary<string, float>();
            float Need(SkillDef d, int depth)
            {
                float own = NodeGap / RingRadius(depth);
                float sum = 0;
                foreach (var c in Kids(d.Id)) sum += Need(c, depth + 1);
                return need[d.Id] = Mathf.Max(own, sum);
            }

            _layout = new() { [SkillDefs.Root] = Vector2.Zero };
            _extent = 0;
            void Place(SkillDef d, int depth, float from, float width)
            {
                float mid = from + width / 2;
                var pos = (Vector2.FromAngle(mid) * RingRadius(depth)).Round();
                _layout[d.Id] = pos;
                _extent = Mathf.Max(_extent, pos.Length());
                var list = Kids(d.Id);
                if (list.Count == 0) return;
                // Reihenfolge wie im alten Raster, gemessen relativ zur Richtung des Elternknotens
                list.Sort((x, y) => Mathf.Wrap(GridAngle(x) - GridAngle(d), -Mathf.Pi, Mathf.Pi)
                    .CompareTo(Mathf.Wrap(GridAngle(y) - GridAngle(d), -Mathf.Pi, Mathf.Pi)));
                float sum = 0;
                foreach (var c in list) sum += need[c.Id];
                float scale = width / sum, a = from;
                foreach (var c in list)
                {
                    float w = need[c.Id] * scale;
                    Place(c, depth + 1, a, w);
                    a += w;
                }
            }

            var roots = Kids(SkillDefs.Root);
            roots.Sort((x, y) => GridAngle(x).CompareTo(GridAngle(y)));
            float total = 0;
            foreach (var r in roots) total += Need(r, 1);
            float k = Mathf.Tau / total;
            // Der Pfandwert-Ast zeigt nach rechts, die anderen folgen im Kreis
            float start = 0;
            foreach (var r in roots)
            {
                if (r.Id == "wert") break;
                start += need[r.Id] * k;
            }
            float angle = -start - need["wert"] * k / 2;
            foreach (var r in roots)
            {
                Place(r, 1, angle, need[r.Id] * k);
                angle += need[r.Id] * k;
            }
            Relax(Kids);
            _extent = 0;
            foreach (var v in _layout.Values) _extent = Mathf.Max(_extent, v.Length());
            return _layout;
        }
    }

    /// <summary>
    /// Weich nachziehen: jede Linie zieht Richtung Wunschlänge (Unterpunkte kurz am Elternknoten),
    /// Knoten stoßen sich ab. So rücken Unterpunkte als Fächer an ihren Knoten, und nichts liegt aufeinander.
    /// </summary>
    static void Relax(Func<string, List<SkillDef>> kids)
    {
        var ids = new List<string>(_layout.Keys);
        var pos = new Vector2[ids.Count];
        var index = new Dictionary<string, int>();
        for (int i = 0; i < ids.Count; i++) { index[ids[i]] = i; pos[i] = _layout[ids[i]]; }
        var edges = new List<(int a, int b, float len)>();
        foreach (var d in SkillDefs.All)
        {
            if (d.Id == SkillDefs.Root) continue;
            var pid = d.Parent ?? SkillDefs.Root;
            bool leaf = kids(d.Id).Count == 0;
            float len = pid == SkillDefs.Root ? 190 : leaf ? 104 : 140;
            edges.Add((index[pid], index[d.Id], len));
        }
        int root = index[SkillDefs.Root];
        const float minDist = 100;
        for (int it = 0; it < 600; it++)
        {
            var f = new Vector2[pos.Length];
            foreach (var (a, b, len) in edges)
            {
                var dv = pos[b] - pos[a];
                float dist = Mathf.Max(1, dv.Length());
                var push = dv / dist * (dist - len) * 0.2f;
                f[a] += push; f[b] -= push;
            }
            for (int i = 0; i < pos.Length; i++)
                for (int j = i + 1; j < pos.Length; j++)
                {
                    var dv = pos[j] - pos[i];
                    float dist = dv.Length();
                    if (dist >= minDist * 1.6f) continue;
                    if (dist < 0.01f) { dv = new Vector2(1, 0); dist = 0.01f; }
                    var push = dv / dist * (minDist * 1.6f - dist) * (dist < minDist ? 0.5f : 0.08f);
                    f[i] -= push; f[j] += push;
                }
            float cool = 1 - it / 600f;
            for (int i = 0; i < pos.Length; i++)
            {
                if (i == root) continue;
                var step = f[i];
                if (step.Length() > 30 * cool + 1) step = step.Normalized() * (30 * cool + 1);
                pos[i] += step;
            }
        }
        // Ganzen Baum so drehen, dass er ins breite Fenster passt (breiter als hoch)
        float best = float.MaxValue, bestAngle = 0;
        for (int deg = 0; deg < 360; deg += 5)
        {
            float r = Mathf.DegToRad(deg), worst = 0;
            foreach (var v in pos)
            {
                var q = v.Rotated(r);
                worst = Mathf.Max(worst, Mathf.Max(Mathf.Abs(q.X) / 1.75f, Mathf.Abs(q.Y)));
            }
            if (worst < best) { best = worst; bestAngle = r; }
        }
        for (int i = 0; i < ids.Count; i++) _layout[ids[i]] = pos[i].Rotated(bestAngle).Round();
    }

    static int StateOrder(NodeState s) => s == NodeState.Learned ? 2 : s == NodeState.Available ? 1 : 0;

    Vector2 Center(SkillDef d) => (Size / 2 + _offset + Layout[d.Id]).Round();

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
                    _ = Layout;
                    _offset = _offset.Clamp(-Vector2.One * _extent, Vector2.One * _extent);
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

        // Zustand jedes Knotens einmal pro Frame bestimmen (nicht in jedem Vergleich beim Sortieren neu)
        _states.Clear();
        foreach (var d in SkillDefs.All) _states[d.Id] = StateOf(d);

        // Linien zuerst, damit die Knoten darüber liegen
        // Gelernte Linien zuletzt, damit gemeinsame Schienen hell sind, so weit etwas gelernt ist
        _lines.Clear();
        _lines.AddRange(SkillDefs.All);
        _lines.Sort((x, y) =>
        {
            int s = StateOrder(_states[x.Id]).CompareTo(StateOrder(_states[y.Id]));
            return s != 0 ? s : LineLength(y).CompareTo(LineLength(x));
        });
        foreach (var d in _lines)
        {
            if (d.Id == SkillDefs.Root) continue;
            var state = _states[d.Id];
            if (state == NodeState.Hidden) continue;
            bool trunk = IsTrunk(d);
            var c = state == NodeState.Learned ? (trunk ? TrunkGot : TwigGot)
                : state == NodeState.Available ? (trunk ? TrunkOpen : TwigOpen) : RingDark;
            DrawLine(Center(SkillDefs.Get(d.Parent ?? SkillDefs.Root)), Center(d), c, trunk ? 6 : 3, true);
        }

        foreach (var d in SkillDefs.All)
        {
            var state = _states[d.Id];
            if (state != NodeState.Hidden) DrawNode(d, state, gs);
        }

        if (_hover != null && !_dragging && SkillDefs.Get(_hover) is { } hd) DrawTooltip(hd, gs);
    }

    readonly Dictionary<string, NodeState> _states = new();
    readonly List<SkillDef> _lines = new();
    static ImageTexture _backTile;
    const int Tile = 300;   // Vielfaches der Brettbreite (30)
    static readonly Dictionary<(string, bool), ImageTexture> _icons = new();

    /// <summary>Dunkles Holz als kachelbares Bild: Bretter und Maserung, die beim Verschieben mitwandern.</summary>
    static ImageTexture BackTile()
    {
        if (_backTile != null) return _backTile;
        var img = Image.CreateEmpty(Tile, Tile, false, Image.Format.Rgba8);
        img.Fill(Back);
        for (int y = 0; y < Tile; y += 30)
        {
            img.FillRect(new Rect2I(0, y, Tile, 3), BackLine);
            img.FillRect(new Rect2I(0, y + 3, Tile, 3), BackLight);
        }
        var rng = new Random(3);
        for (int i = 0; i < 70; i++)
        {
            int x = rng.Next(0, Tile / 3) * 3, y = rng.Next(0, Tile / 3) * 3, w = 3 * rng.Next(2, 7);
            var c = rng.Next(2) == 0 ? BackLight : BackLine;
            img.FillRect(new Rect2I(x, y, w, 3), c);
            if (x + w > Tile) img.FillRect(new Rect2I(0, y, x + w - Tile, 3), c);   // nahtlos über den Rand
        }
        return _backTile = ImageTexture.CreateFromImage(img);
    }

    void DrawBackground()
    {
        var start = new Vector2(Mathf.PosMod(_offset.X + Size.X / 2, Tile), Mathf.PosMod(_offset.Y + Size.Y / 2, Tile)) - new Vector2(Tile, Tile);
        DrawTextureRect(BackTile(), new Rect2(start.Round(), Size + new Vector2(2 * Tile, 2 * Tile)), true);
    }

    /// <summary>Knotensymbol (8×8 Kunstpixel) als fertiges Bild statt 64 einzelner Rechtecke.</summary>
    static ImageTexture Icon(SkillDef d, bool bright)
    {
        if (_icons.TryGetValue((d.Id, bright), out var tex)) return tex;
        const int p = 3;
        var ink = bright ? Wood.Ink : Wood.Ink.Darkened(0.15f);
        var light = d.Branch.Color.Lightened(0.15f);
        var img = Image.CreateEmpty(8 * p, 8 * p, false, Image.Format.Rgba8);
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
            {
                char ch = d.Icon[y][x];
                if (ch == '#') img.FillRect(new Rect2I(x * p, y * p, p, p), ink);
                else if (ch == 'o') img.FillRect(new Rect2I(x * p, y * p, p, p), light);
            }
        return _icons[(d.Id, bright)] = ImageTexture.CreateFromImage(img);
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
        if (state == NodeState.Teaser && d.RequiresMax == null)
        {
            DrawString(PfandTheme.Body, new Vector2(r.Position.X, c.Y + 12), "?", HorizontalAlignment.Center, NodeSize, 36, RingDark.Lightened(0.3f));
            DrawLock(r);
            return;
        }
        DrawTexture(Icon(d, state == NodeState.Learned || root), c - new Vector2(4 * p, 4 * p));

        if (state == NodeState.Teaser) { DrawRect(r.Grow(-2 * p), new Color(Inside, 0.55f)); DrawLock(r); return; }
        if (!root) DrawBadge(r, BadgeOf(d));
        if (!root && gs.CanBuySkill(d)) DrawAffordable(r);

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

    /// <summary>Genug Kronkorken für den nächsten Schritt: kleiner grüner Pfeil oben rechts, der leicht hüpft.</summary>
    void DrawAffordable(Rect2 r)
    {
        const float p = 3;
        float bob = Mathf.Round(Mathf.Sin(_time * 5) * 1.5f);
        var o = new Vector2(r.End.X - 3 * p, r.Position.Y - 2 * p + bob);
        void R(float x, float y, float w, float h, Color c) => DrawRect(new Rect2(o.X + x * p, o.Y + y * p, w * p, h * p), c);
        // Pfeil nach oben mit dunkler Kontur, in Gold, damit er sich vom grünen Plus unterscheidet
        R(1, -1, 3, 1, Edge); R(0, 0, 5, 1, Edge); R(-1, 1, 7, 2, Edge); R(1, 3, 3, 3, Edge);
        R(2, 0, 1, 1, RingGotHi); R(1, 1, 3, 1, RingGot); R(0, 2, 5, 1, RingGot); R(2, 3, 1, 2, RingGot);
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
        if (StateOf(d) == NodeState.Teaser && d.RequiresMax != null)
            lines.Add(($"Erst {SkillDefs.Get(d.RequiresMax).Name} voll ausbauen", BadInk));
        else if (StateOf(d) == NodeState.Teaser) title = "NEUER AST";
        else
        {
            if (d.Endless) lines.Add(($"Stufe {lvl} (ohne Ende)", Wood.Muted));
            else if (d.MaxLevel > 1) lines.Add(($"Stufe {lvl}/{d.MaxLevel}", Wood.Muted));
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
