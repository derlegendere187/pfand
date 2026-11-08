using System.Collections.Generic;
using Godot;
using Pfand.Core;
using Pfand.Park;

namespace Pfand.UI;

/// <summary>Der Skilltree als Overlay. Skills kosten Kronkorken.</summary>
public partial class SkillPopup : Control
{
    class Node
    {
        public SkillDef Def;
        public PanelContainer Root;
        public Label Name, Desc;
        public Button Buy;
    }

    readonly List<Node> _nodes = new();
    Label _balance;

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

        var head = new HBoxContainer();
        head.AddChild(PfandTheme.Title("SKILLTREE", 16));
        head.AddChild(PfandTheme.Spacer());
        head.AddChild(new CapIcon { Index = 0, Found = true, CustomMinimumSize = new Vector2(24, 24), SizeFlagsVertical = SizeFlags.ShrinkCenter });
        _balance = PfandTheme.Text("", 28, Palette.MoneyText);
        head.AddChild(_balance);
        col.AddChild(head);

        var branches = new HBoxContainer();
        branches.AddThemeConstantOverride("separation", 16);
        col.AddChild(branches);

        foreach (var branch in SkillDefs.Branches)
        {
            var bcol = new VBoxContainer { CustomMinimumSize = new Vector2(250, 0) };
            bcol.AddThemeConstantOverride("separation", 6);
            var title = PfandTheme.Title(branch, 8, Palette.Muted);
            title.HorizontalAlignment = HorizontalAlignment.Center;
            bcol.AddChild(title);
            bool first = true;
            foreach (var s in SkillDefs.All.FindAll(x => x.Branch == branch))
            {
                if (!first)
                {
                    var arrow = PfandTheme.Text("▼", 20, Palette.Muted);
                    arrow.HorizontalAlignment = HorizontalAlignment.Center;
                    bcol.AddChild(arrow);
                }
                first = false;
                var n = BuildNode(s);
                _nodes.Add(n);
                bcol.AddChild(n.Root);
            }
            branches.AddChild(bcol);
        }

        col.AddChild(PfandTheme.Text("Kronkorken findest du beim Einwerfen von Flaschen (nicht bei Dosen).", 20, Palette.Muted));
        var close = new Button { Text = "SCHLIESSEN", CustomMinimumSize = new Vector2(0, 44) };
        close.Pressed += Close;
        col.AddChild(close);

        GameState.I.Changed += Refresh;
    }

    public override void _ExitTree() => GameState.I.Changed -= Refresh;

    Node BuildNode(SkillDef s)
    {
        var n = new Node { Def = s, Root = new PanelContainer() };
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 2);
        n.Root.AddChild(col);
        n.Name = PfandTheme.Title(s.Name, 8);
        col.AddChild(n.Name);
        n.Desc = PfandTheme.Text(s.Desc, 20);
        n.Desc.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        col.AddChild(n.Desc);
        n.Buy = new Button { CustomMinimumSize = new Vector2(0, 36) };
        n.Buy.AddThemeFontSizeOverride("font_size", 22);
        n.Buy.Pressed += () => GameState.I.TryBuySkill(s);
        col.AddChild(n.Buy);
        return n;
    }

    void Refresh()
    {
        if (!Visible) return;
        var gs = GameState.I;
        _balance.Text = $"{gs.Kronkorken} Kronkorken";
        foreach (var n in _nodes)
        {
            bool owned = gs.HasSkill(n.Def.Id);
            bool available = gs.SkillAvailable(n.Def);
            n.Root.AddThemeStyleboxOverride("panel", PfandTheme.Box(
                owned ? Color.FromHtml("#e4f0d4") : available ? Palette.PanelLight : Palette.Bg,
                owned ? Palette.GoodText : available ? Palette.Border : Palette.MachineDark, 4, 8));
            n.Name.AddThemeColorOverride("font_color", available || owned ? Palette.Ink : Palette.Muted);
            n.Desc.AddThemeColorOverride("font_color", available || owned ? Palette.InkSoft : Palette.Muted);
            if (owned) { n.Buy.Text = "GELERNT"; n.Buy.Disabled = true; }
            else if (!available) { n.Buy.Text = "ERST: " + SkillDefs.Get(n.Def.Requires).Name; n.Buy.Disabled = true; }
            else { n.Buy.Text = $"{n.Def.Cost} KRONKORKEN"; n.Buy.Disabled = !gs.CanBuySkill(n.Def); }
        }
    }

    public void Open()
    {
        Visible = true;
        Refresh();
    }

    public void Close() => Visible = false;

    public override void _UnhandledInput(InputEvent e)
    {
        if (Visible && e is InputEventKey k && k.Pressed && k.Keycode == Key.Escape) { Close(); GetViewport().SetInputAsHandled(); }
    }
}
