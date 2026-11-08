using Godot;
using Pfand.Core;
using Pfand.Park;

namespace Pfand.UI;

/// <summary>Kleines Fenster zum Ausbauen eines einzelnen Objekts im Park (Stufen nacheinander kaufen).</summary>
public partial class AusbauPopup : Control
{
    string _kind;
    int _index;
    Label _title;
    VBoxContainer _steps;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;

        var dim = new ColorRect { Color = new Color(Palette.Ink, 0.45f) };
        dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        dim.GuiInput += e => { if (e is InputEventMouseButton mb && mb.Pressed) Close(); };
        AddChild(dim);

        var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var frame = PfandTheme.Panel(Palette.Panel, null, 18);
        frame.CustomMinimumSize = new Vector2(440, 0);
        center.AddChild(frame);
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 12);
        frame.AddChild(col);

        var head = new HBoxContainer();
        _title = PfandTheme.Title("", 16);
        head.AddChild(_title);
        head.AddChild(PfandTheme.Spacer());
        head.AddChild(PfandTheme.Title("AUSBAU", 8, Palette.Muted));
        col.AddChild(head);

        _steps = new VBoxContainer();
        _steps.AddThemeConstantOverride("separation", 8);
        col.AddChild(_steps);

        var close = new Button { Text = "SCHLIESSEN", CustomMinimumSize = new Vector2(0, 40) };
        close.Pressed += Close;
        col.AddChild(close);

        GameState.I.Changed += Refresh;
    }

    public override void _ExitTree() => GameState.I.Changed -= Refresh;

    public void Open(string kind, int index)
    {
        if (!AusbauDefs.Kinds.ContainsKey(kind)) return;
        _kind = kind;
        _index = index;
        Visible = true;
        Refresh();
    }

    void Refresh()
    {
        if (!Visible || _kind == null) return;
        var gs = GameState.I;
        var (title, steps) = AusbauDefs.Kinds[_kind];
        bool single = _kind is "imbiss" or "grill";
        _title.Text = single ? title : $"{title} {_index + 1}";

        foreach (var c in _steps.GetChildren()) { _steps.RemoveChild(c); c.QueueFree(); }
        int lvl = gs.AusbauLevel(_kind, _index);
        for (int i = 0; i < steps.Length; i++)
        {
            bool done = i < lvl, next = i == lvl;
            var card = PfandTheme.Panel(done ? Color.FromHtml("#e4f0d4") : next ? Palette.PanelLight : Palette.Bg,
                done ? Palette.GoodText : next ? Palette.Border : Palette.MachineDark, 10);
            var v = new VBoxContainer();
            v.AddThemeConstantOverride("separation", 2);
            card.AddChild(v);
            v.AddChild(PfandTheme.Title($"STUFE {i + 1}: {steps[i].Name}", 8, next || done ? Palette.Ink : Palette.Muted));
            var d = PfandTheme.Text(steps[i].Desc, 22, next || done ? Palette.InkSoft : Palette.Muted);
            d.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            v.AddChild(d);
            var b = new Button { CustomMinimumSize = new Vector2(0, 36) };
            b.AddThemeFontSizeOverride("font_size", 22);
            if (done) { b.Text = "GEBAUT"; b.Disabled = true; }
            else if (next)
            {
                b.Text = GameState.Euro(AusbauDefs.Price(_kind, _index, i));
                b.Disabled = !gs.CanBuyAusbau(_kind, _index);
                b.Pressed += () => gs.TryBuyAusbau(_kind, _index);
            }
            else { b.Text = "ERST STUFE " + i; b.Disabled = true; }
            v.AddChild(b);
            _steps.AddChild(card);
        }
    }

    public void Close() => Visible = false;

    public override void _UnhandledInput(InputEvent e)
    {
        if (Visible && e is InputEventKey k && k.Pressed && k.Keycode == Key.Escape) { Close(); GetViewport().SetInputAsHandled(); }
    }
}
