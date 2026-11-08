using Godot;
using Pfand.Core;
using Pfand.Park;

namespace Pfand.UI;

/// <summary>Kopfzeile: Spielname links, Kennzahlen rechts.</summary>
public partial class HeaderBar : HBoxContainer
{
    public ParkView Park;
    Label _money, _visitors, _rate, _clock;
    double _acc;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(0, 52);
        AddThemeConstantOverride("separation", 12);

        var logo = PfandTheme.Panel(Palette.Panel, null, 10);
        var logoRow = new HBoxContainer();
        logoRow.AddThemeConstantOverride("separation", 12);
        logoRow.AddChild(new BottleIcon { CustomMinimumSize = new Vector2(14, 32) });
        var name = PfandTheme.Title("PFAND", 24);
        name.VerticalAlignment = VerticalAlignment.Center;
        logoRow.AddChild(name);
        logo.AddChild(logoRow);
        AddChild(logo);

        AddChild(PfandTheme.Spacer());
        _money = Stat("GELD", Palette.MoneyText);
        _visitors = Stat("BESUCHER", Palette.Ink);
        _rate = Stat("FLASCHEN/MIN", Palette.GoodText);
        _clock = Stat("UHRZEIT", Palette.Ink);
        GameState.I.Changed += Refresh;
        Refresh();
    }

    public override void _ExitTree() => GameState.I.Changed -= Refresh;

    Label Stat(string caption, Color valueColor)
    {
        var box = PfandTheme.Panel(Palette.Panel, null, 6);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        var cap = PfandTheme.Text(caption, 22, Palette.Muted);
        cap.VerticalAlignment = VerticalAlignment.Center;
        var val = PfandTheme.Text("", 28, valueColor);
        val.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(cap);
        row.AddChild(val);
        box.AddChild(row);
        AddChild(box);
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
