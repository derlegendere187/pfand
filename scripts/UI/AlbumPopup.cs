using Godot;
using Pfand.Core;
using Pfand.Park;

namespace Pfand.UI;

/// <summary>Das Kronkorken-Album als Overlay über dem Spiel.</summary>
public partial class AlbumPopup : Control
{
    Label _title;
    VBoxContainer _sets;

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

        var cover = PfandTheme.Panel(Palette.AlbumCover, null, 16);
        center.AddChild(cover);
        var page = PfandTheme.Panel(Palette.AlbumPage, null, 20);
        cover.AddChild(page);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 14);
        page.AddChild(col);

        var head = new HBoxContainer();
        head.AddChild(PfandTheme.Title("KRONKORKEN-ALBUM", 16));
        head.AddChild(PfandTheme.Spacer());
        _title = PfandTheme.Text("", 24, Palette.Muted);
        head.AddChild(_title);
        col.AddChild(head);

        _sets = new VBoxContainer();
        _sets.AddThemeConstantOverride("separation", 12);
        // Viele Sets: der Bereich scrollt, statt das Fenster zu sprengen
        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            CustomMinimumSize = new Vector2(0, 470),
        };
        scroll.AddChild(_sets);
        col.AddChild(scroll);

        var legend = new HBoxContainer();
        legend.AddThemeConstantOverride("separation", 16);
        legend.AddChild(PfandTheme.Text("Rahmen:", 20, Palette.Muted));
        for (int t = 0; t < CapData.TierNames.Length; t++)
            legend.AddChild(PfandTheme.Text("■ " + CapData.TierNames[t], 20, TierColor(t)));
        col.AddChild(legend);
        legend.AddChild(PfandTheme.Text("· spätere Sets seltener", 20, Palette.Muted));
        col.AddChild(PfandTheme.Text("Jeder Fund ist auch 1 Kronkorken für den Skilltree – das Album behält ihn trotzdem.", 20, Palette.Muted));

        var close = new Button { Text = "SCHLIESSEN", CustomMinimumSize = new Vector2(0, 44) };
        close.Pressed += Close;
        col.AddChild(close);
    }

    public void Open()
    {
        foreach (var c in _sets.GetChildren()) c.QueueFree();
        var gs = GameState.I;
        int foundTotal = 0;
        foreach (var set in CapData.Sets)
        {
            int inSet = 0, foundInSet = 0;
            var grid = new GridContainer { Columns = 5 };
            grid.AddThemeConstantOverride("h_separation", 10);
            grid.AddThemeConstantOverride("v_separation", 10);
            for (int i = 0; i < CapData.Count; i++)
            {
                if (CapData.All[i].Set != set) continue;
                inSet++;
                int n = gs.Caps.TryGetValue(i, out var cnt) ? cnt : 0;
                if (n > 0) { foundInSet++; foundTotal++; }
                grid.AddChild(Cell(i, n));
            }
            var head = new HBoxContainer();
            head.AddChild(PfandTheme.Title("SET: " + set, 8, Palette.InkSoft));
            head.AddChild(PfandTheme.Spacer());
            head.AddChild(PfandTheme.Text($"{foundInSet}/{inSet}", 22, Palette.Muted));
            _sets.AddChild(head);
            _sets.AddChild(grid);
        }
        _title.Text = $"{foundTotal} / {CapData.Count}";
        Visible = true;
    }

    /// <summary>Rahmenfarbe je Seltenheit: häufig, selten, sehr selten, legendär.</summary>
    static Color TierColor(int tier) => tier switch
    {
        1 => Color.FromHtml("#2f6fae"),
        2 => Color.FromHtml("#7a4fb0"),
        3 => Color.FromHtml("#c99a1e"),
        _ => Palette.Ink,
    };

    static Control Cell(int i, int n)
    {
        int tier = CapData.Tier(i);
        var frame = TierColor(tier);
        var cell = PfandTheme.Panel(Color.FromHtml("#e4d6b6"), n > 0 ? frame : new Color(frame, 0.45f), 6);
        cell.CustomMinimumSize = new Vector2(118, 0);
        var v = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        v.AddThemeConstantOverride("separation", 4);
        v.AddChild(new CapIcon { Index = i, Found = n > 0, CustomMinimumSize = new Vector2(72, 72), SizeFlagsHorizontal = SizeFlags.ShrinkCenter });
        var name = PfandTheme.Title(CapData.All[i].Name, 8, n > 0 ? Palette.Ink : Palette.Muted);
        name.HorizontalAlignment = HorizontalAlignment.Center;
        name.ClipText = true;
        v.AddChild(name);
        // Doppelte zählen; Seltenheit steht in der Rahmenfarbe
        var count = PfandTheme.Text(n > 1 ? $"×{n}" : " ", 20, tier > 0 ? frame : Palette.Muted);
        count.HorizontalAlignment = HorizontalAlignment.Center;
        v.AddChild(count);
        cell.AddChild(v);
        return cell;
    }

    public void Close() => Visible = false;

    public override void _UnhandledInput(InputEvent e)
    {
        if (Visible && e is InputEventKey k && k.Pressed && k.Keycode == Key.Escape) { Close(); GetViewport().SetInputAsHandled(); }
    }
}

public partial class CapIcon : Control
{
    public int Index;
    public bool Found;

    public override void _Draw()
        => Sprites.Cap(this, Vector2.Zero, Mathf.Floor(Size.X / 24f) * 2, CapData.All[Index], Found);
}
