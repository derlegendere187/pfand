using System.Collections.Generic;
using Godot;
using Pfand.Core;
using Pfand.Park;

namespace Pfand.UI;

/// <summary>Liste der großen Events: starten (kostet Kronkorken), Laufzeit und gemeinsame Pause anzeigen.</summary>
public partial class EventPanel : PanelContainer
{
    class Card
    {
        public EventDef Def;
        public PanelContainer Root;
        public Label Name, Desc, Info;
        public Button Start;
    }

    readonly List<Card> _cards = new();
    double _acc;

    public override void _Ready()
    {
        SizeFlagsVertical = SizeFlags.ExpandFill;
        AddThemeStyleboxOverride("panel", PfandTheme.Box(Palette.Panel, Palette.Border, 4, 8));

        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        AddChild(scroll);
        var list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        list.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(list);

        foreach (var def in EventDefs.All)
        {
            if (def.Short) continue; // Aktionen kommen per Flyer im Park
            var c = new Card { Def = def, Root = new PanelContainer() };
            var col = new VBoxContainer();
            col.AddThemeConstantOverride("separation", 2);
            c.Root.AddChild(col);
            c.Name = PfandTheme.Title(def.Name, 8);
            col.AddChild(c.Name);
            c.Desc = PfandTheme.Text(def.Desc, 20);
            c.Desc.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            col.AddChild(c.Desc);
            c.Info = PfandTheme.Text("", 20, Palette.Muted);
            c.Info.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            col.AddChild(c.Info);
            c.Start = new Button { CustomMinimumSize = new Vector2(0, 36), ClipText = true };
            c.Start.AddThemeFontSizeOverride("font_size", 22);
            c.Start.Pressed += () => GameState.I.TryStartEvent(def);
            col.AddChild(c.Start);
            _cards.Add(c);
            list.AddChild(c.Root);
        }

        var hint = PfandTheme.Text("Neue Events schaltest du im Skilltree frei. Nach jedem Event haben alle Events eine gemeinsame Pause.", 20, Palette.Muted);
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        list.AddChild(hint);

        GameState.I.Changed += Refresh;
        Refresh();
    }

    public override void _ExitTree() => GameState.I.Changed -= Refresh;

    public override void _Process(double delta)
    {
        _acc += delta;
        if (_acc < 0.2 || !IsVisibleInTree()) return;
        _acc = 0;
        Refresh();
    }

    void Refresh()
    {
        var gs = GameState.I;
        foreach (var c in _cards)
        {
            bool unlocked = gs.EventUnlocked(c.Def);
            bool running = gs.EventRunning(c.Def);
            double pause = gs.EventPauseLeft;

            c.Root.Visible = unlocked;
            if (!unlocked) continue;
            c.Root.AddThemeStyleboxOverride("panel", PfandTheme.Box(
                running ? Color.FromHtml("#fff3c4") : unlocked ? Palette.PanelLight : Palette.Bg,
                unlocked ? Palette.Border : Palette.MachineDark, 4, 8));
            c.Name.AddThemeColorOverride("font_color", unlocked ? Palette.Ink : Palette.Muted);
            c.Desc.AddThemeColorOverride("font_color", unlocked ? Palette.InkSoft : Palette.Muted);
            int cost = gs.EventCost(c.Def);
            double bonus = gs.EventBonus(c.Def);
            c.Info.Text = $"Dauer {GameState.Seconds(gs.EventDuration(c.Def))}"
                + (bonus > 1 ? $" · Flaschen ×{bonus.ToString("0.##").Replace('.', ',')}" : "");

            if (!unlocked) { c.Start.Text = "IM SKILLTREE"; c.Start.Disabled = true; }
            else if (running)
            {
                c.Start.Text = $"LÄUFT · {GameState.Seconds(gs.EventTimeLeft)}"; c.Start.Disabled = true;
            }
            else if (gs.ActiveEvent != null) { c.Start.Text = "ANDERES LÄUFT"; c.Start.Disabled = true; }
            else if (pause > 0) { c.Start.Text = $"PAUSE · {GameState.Seconds(pause)}"; c.Start.Disabled = true; }
            else { c.Start.Text = $"STARTEN · {cost} KK"; c.Start.Disabled = gs.Kronkorken < cost; }
        }
    }
}
