using Godot;
using Pfand.Core;
using Pfand.Park;

namespace Pfand.UI;

/// <summary>Kopfzeile: ein Holzbrett mit Spielname links und den Kennzahlen rechts.</summary>
public partial class HeaderBar : PanelContainer
{
    public ParkView Park;
    Label _money, _visitors, _rate, _clock, _event;
    HBoxContainer _row;

    public Rect2 MoneyRect => _money.GetParent<Control>().GetGlobalRect();
    double _acc;

    public override void _Ready()
    {
        AddThemeStyleboxOverride("panel", PfandTheme.WoodPanel(10));
        _row = new HBoxContainer();
        _row.AddThemeConstantOverride("separation", 22);
        AddChild(_row);

        var logo = new HBoxContainer();
        logo.AddThemeConstantOverride("separation", 10);
        logo.AddChild(new BottleIcon { CustomMinimumSize = new Vector2(14, 32) });
        var name = PfandTheme.Text("Pfand", 40, Wood.Ink);
        name.VerticalAlignment = VerticalAlignment.Center;
        logo.AddChild(name);
        _row.AddChild(logo);

        // Laufendes Event und Aktionen, z. B. „OKTOBERFEST 2:12 · Flaschen ×1,75 wert“
        _event = PfandTheme.Text("", 26, Wood.Accent);
        _event.VerticalAlignment = VerticalAlignment.Center;
        _event.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _event.ClipText = true;
        _event.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        _row.AddChild(_event);
        _money = Stat("Geld", Wood.Accent);
        _visitors = Stat("Besucher", Wood.Ink);
        _rate = Stat("Flaschen/min", Wood.Ink);
        _clock = Stat("", Wood.Ink);
        GameState.I.Changed += Refresh;
        Refresh();
    }

    public override void _ExitTree() => GameState.I.Changed -= Refresh;

    Label Stat(string caption, Color valueColor)
    {
        var box = new HBoxContainer();
        box.AddThemeConstantOverride("separation", 8);
        if (caption != "")
        {
            var cap = PfandTheme.Text(caption, 22, Wood.Muted);
            cap.VerticalAlignment = VerticalAlignment.Center;
            box.AddChild(cap);
        }
        var val = PfandTheme.Text("", 30, valueColor);
        val.VerticalAlignment = VerticalAlignment.Center;
        box.AddChild(val);
        _row.AddChild(box);
        return val;
    }

    public override void _Process(double delta)
    {
        _acc += delta;
        if (_acc < 0.25) return;
        _acc = 0;
        Refresh();
    }

    void Refresh()
    {
        var gs = GameState.I;
        _money.Text = GameState.Euro(gs.Money);
        _visitors.Text = Park == null ? "0" : Park.Sim.Visitors.Count.ToString();
        _rate.Text = Park == null ? "0" : Park.Sim.BottlesPerMinute.ToString();
        string phase = gs.Phase switch { DayPhase.Tag => "Tag", DayPhase.Abend => "Abend", _ => "Nacht" };
        _clock.Text = $"{gs.ClockText()} {phase}";
        _event.Text = EventText(gs);
    }

    static string EventText(GameState gs)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (gs.ActiveEvent != null && EventDefs.Get(gs.ActiveEvent) is { } ev)
            parts.Add($"{ev.Name} {GameState.Seconds(gs.EventTimeLeft)}");
        foreach (var kv in gs.ActiveBoosts)
            if (EventDefs.Get(kv.Key) is { } b) parts.Add($"{b.Name} {GameState.Seconds(kv.Value)}");
        double bonus = gs.EventValueBonus;
        if (bonus > 1.001) parts.Add($"Flaschen ×{bonus.ToString("0.##").Replace('.', ',')} wert");
        return string.Join("  ·  ", parts);
    }
}

/// <summary>Kleines Flaschen-Logo.</summary>
public partial class BottleIcon : Control
{
    public override void _Draw()
    {
        float p = 2;
        var o = new Vector2(-1, 0);
        void R(float x, float y, float w, float h, Color c) => DrawRect(new Rect2(o.X + x * p, o.Y + y * p, w * p, h * p), c);
        R(3, 0, 2, 1, Color.FromHtml("#c8553d"));
        R(3, 1, 2, 4, Palette.Leergut);
        R(2, 5, 4, 2, Palette.Leergut);
        R(1, 7, 6, 9, Palette.Leergut);
        R(1, 10, 6, 3, Palette.Panel);
    }
}
