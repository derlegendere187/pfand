using System;
using System.Linq;
using Godot;
using Pfand.Core;
using Pfand.Park;

namespace Pfand.UI;

/// <summary>Statistik-Fenster: Spielzeit, Geld, Flaschen nach Sorte, Rekorde.</summary>
public partial class StatsPopup : Control
{
    GridContainer _left, _right;
    double _acc;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;

        var dim = new ColorRect { Color = new Color(Palette.Ink, 0.55f) };
        dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        dim.GuiInput += e => { if (e is InputEventMouseButton mb && mb.Pressed) Close(); };
        AddChild(dim);

        var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var frame = PfandTheme.Panel(Palette.Panel, null, 20);
        center.AddChild(frame);
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 14);
        frame.AddChild(col);
        col.AddChild(PfandTheme.Title("STATISTIK", 16));

        var cols = new HBoxContainer();
        cols.AddThemeConstantOverride("separation", 20);
        col.AddChild(cols);
        _left = Section(cols, "PARK");
        _right = Section(cols, "FLASCHEN");

        var close = new Button { Text = "SCHLIESSEN", CustomMinimumSize = new Vector2(0, 44) };
        close.Pressed += Close;
        col.AddChild(close);
    }

    static GridContainer Section(HBoxContainer parent, string title)
    {
        var panel = PfandTheme.Panel(Palette.PanelLight, Palette.Border, 12);
        panel.CustomMinimumSize = new Vector2(360, 0);
        var v = new VBoxContainer();
        v.AddThemeConstantOverride("separation", 8);
        panel.AddChild(v);
        v.AddChild(PfandTheme.Title(title, 8, Palette.Muted));
        var grid = new GridContainer { Columns = 2 };
        grid.AddThemeConstantOverride("h_separation", 20);
        grid.AddThemeConstantOverride("v_separation", 0);
        v.AddChild(grid);
        parent.AddChild(panel);
        return grid;
    }

    public void Open()
    {
        Visible = true;
        Fill();
    }

    public void Close() => Visible = false;

    public override void _Process(double delta)
    {
        if (!Visible) return;
        _acc += delta;
        if (_acc < 1) return;
        _acc = 0;
        Fill();
    }

    void Fill()
    {
        var gs = GameState.I;
        Clear(_left);
        Clear(_right);

        Row(_left, "Spielzeit", PlayTime(gs.PlayTime));
        Row(_left, "Verdient insgesamt", GameState.Euro(gs.TotalEarned));
        Row(_left, "Pro Minute (Schnitt)", gs.PlayTime > 30 ? GameState.Euro(gs.TotalEarned / (gs.PlayTime / 60)) : "–");
        Row(_left, "Bester Pfandbon", GameState.Euro(gs.BestBon));
        Row(_left, "Meiste Besucher", gs.PeakVisitors.ToString());
        Row(_left, "Upgrades gekauft", gs.Upgrades.Values.Sum().ToString());
        Row(_left, "Events gefeiert", gs.EventsHeld.ToString());
        Row(_left, "Kronkorken im Album", $"{gs.Caps.Count(kv => kv.Value > 0)} / {CapData.Count}");
        Row(_left, "Kronkorken gefunden", gs.Caps.Values.Sum().ToString());
        Row(_left, "Skill-Stufen", gs.Skills.Values.Sum().ToString());

        Row(_right, "Eingeworfen", gs.BottlesFed.ToString("N0"));
        Row(_right, "Selbst gesammelt", gs.CollectedByHand.ToString("N0"));
        Row(_right, "Von Sammlern", gs.CollectedByCollectors.ToString("N0"));
        Row(_right, "Volle Kästen", gs.KaestenAbgegeben.ToString());
        foreach (BottleType t in Enum.GetValues(typeof(BottleType)))
        {
            long n = gs.FedByType.TryGetValue((int)t, out var c) ? c : 0;
            if (n == 0 && (t == BottleType.Retro || t == BottleType.Gold)) continue; // erst zeigen, wenn gefunden
            Row(_right, BottleInfo.Name(t), n.ToString("N0"));
        }
    }

    static string PlayTime(double s)
    {
        int t = (int)s;
        return t >= 3600 ? $"{t / 3600} h {t % 3600 / 60:00} min" : $"{t / 60} min {t % 60:00} s";
    }

    static void Clear(Node n)
    {
        foreach (var c in n.GetChildren()) { n.RemoveChild(c); c.QueueFree(); }
    }

    static void Row(GridContainer g, string label, string value)
    {
        var l = PfandTheme.Text(label, 22, Palette.InkSoft);
        l.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        g.AddChild(l);
        var v = PfandTheme.Text(value, 22);
        v.HorizontalAlignment = HorizontalAlignment.Right;
        g.AddChild(v);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (Visible && e is InputEventKey k && k.Pressed && k.Keycode == Key.Escape) { Close(); GetViewport().SetInputAsHandled(); }
    }
}
