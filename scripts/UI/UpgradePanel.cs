using System.Collections.Generic;
using Godot;
using Pfand.Core;
using Pfand.Park;

namespace Pfand.UI;

/// <summary>Liste der Park-Upgrades mit Kaufen-Knöpfen.</summary>
public partial class UpgradePanel : PanelContainer
{
    class Card
    {
        public UpgradeDef Def;
        public PanelContainer Root;
        public Label Name, Owned, Effect;
        public Button Buy;
    }

    readonly List<Card> _cards = new();

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

        foreach (var def in UpgradeDefs.All)
        {
            var card = BuildCard(def);
            _cards.Add(card);
            list.AddChild(card.Root);
        }

        GameState.I.Changed += Refresh;
        Refresh();
    }

    public override void _ExitTree() => GameState.I.Changed -= Refresh;

    Card BuildCard(UpgradeDef def)
    {
        var c = new Card { Def = def, Root = new PanelContainer() };
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        c.Root.AddChild(row);

        row.AddChild(new UpgradeIcon { Def = def, CustomMinimumSize = new Vector2(40, 40), SizeFlagsVertical = SizeFlags.ShrinkBegin });

        var col = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        col.AddThemeConstantOverride("separation", 2);
        row.AddChild(col);

        var top = new HBoxContainer();
        c.Name = PfandTheme.Title(def.Name, 8);
        c.Owned = PfandTheme.Title("", 8, Palette.GoodText);
        top.AddChild(c.Name);
        top.AddChild(PfandTheme.Spacer());
        top.AddChild(c.Owned);
        col.AddChild(top);

        c.Effect = PfandTheme.Text(def.Effect, 20);
        c.Effect.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        col.AddChild(c.Effect);

        c.Buy = new Button { CustomMinimumSize = new Vector2(0, 36) };
        c.Buy.AddThemeFontSizeOverride("font_size", 22);
        c.Buy.Pressed += () => GameState.I.TryBuy(def);
        col.AddChild(c.Buy);
        return c;
    }

    void Refresh()
    {
        var gs = GameState.I;
        foreach (var c in _cards)
        {
            int level = gs.Level(c.Def.Id);
            bool unlocked = c.Def.Unlocked(gs);
            bool maxed = level >= c.Def.Max;
            c.Owned.Text = level > 0 ? $"×{level}" : "";

            bool dim = !unlocked;
            c.Root.AddThemeStyleboxOverride("panel", PfandTheme.Box(dim ? Palette.Bg : Palette.PanelLight, dim ? Palette.MachineDark : Palette.Border, 4, 8));
            c.Name.AddThemeColorOverride("font_color", dim ? Palette.Muted : Palette.Ink);
            c.Effect.AddThemeColorOverride("font_color", dim ? Palette.Muted : Palette.InkSoft);

            if (!unlocked) { c.Buy.Text = c.Def.LockedTextFn?.Invoke(gs) ?? c.Def.LockedText; c.Buy.Disabled = true; }
            else if (maxed) { c.Buy.Text = "MAXIMUM"; c.Buy.Disabled = true; }
            else { c.Buy.Text = GameState.Euro(c.Def.Price(level)); c.Buy.Disabled = !gs.CanBuy(c.Def); }
        }
    }
}

public partial class UpgradeIcon : Control
{
    public UpgradeDef Def;

    public override void _Draw()
    {
        var r = new Rect2(Vector2.Zero, Size);
        DrawRect(r, Palette.Border);
        DrawRect(r.Grow(-2), Palette.Bg);
        float p = Size.X / 8f;
        DrawRect(new Rect2(p, p, 6 * p, 6 * p), Def.C1);
        DrawRect(new Rect2(p, p, 6 * p, p), Def.C2);
        DrawRect(new Rect2(3 * p, 3 * p, 2 * p, 2 * p), Def.C2);
    }
}
