using Godot;
using Pfand.Core;
using Pfand.Park;

namespace Pfand.UI;

/// <summary>Das Deckel-Album als Overlay über dem Spiel.</summary>
public partial class AlbumPopup : Control
{
    Label _title;
    GridContainer _grid;

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
        col.AddThemeConstantOverride("separation", 16);
        page.AddChild(col);

        var head = new HBoxContainer();
        head.AddChild(PfandTheme.Title("DECKEL-ALBUM", 16));
        head.AddChild(PfandTheme.Spacer());
        _title = PfandTheme.Text("", 24, Palette.Muted);
        head.AddChild(_title);
        col.AddChild(head);

        col.AddChild(PfandTheme.Title("SET: TIERE", 8, Palette.InkSoft));

        _grid = new GridContainer { Columns = 3 };
        _grid.AddThemeConstantOverride("h_separation", 12);
        _grid.AddThemeConstantOverride("v_separation", 12);
        col.AddChild(_grid);

        col.AddChild(PfandTheme.Text("Deckel findest du beim Einwerfen von Flaschen (nicht bei Dosen).", 20, Palette.Muted));

        var close = new Button { Text = "SCHLIESSEN", CustomMinimumSize = new Vector2(0, 44) };
        close.Pressed += Close;
        col.AddChild(close);
    }

    public void Open()
    {
        foreach (var c in _grid.GetChildren()) c.QueueFree();
        var gs = GameState.I;
        int found = 0;
        for (int i = 0; i < CapData.Count; i++)
        {
            int n = gs.Caps.TryGetValue(i, out var cnt) ? cnt : 0;
            if (n > 0) found++;
            var cell = PfandTheme.Panel(Color.FromHtml("#e4d6b6"), n > 0 ? Palette.Ink : Palette.Muted, 8);
            cell.CustomMinimumSize = new Vector2(150, 0);
            var v = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
            v.AddThemeConstantOverride("separation", 6);
            var icon = new CapIcon { Index = i, Found = n > 0, CustomMinimumSize = new Vector2(72, 72), SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
            v.AddChild(icon);
            var name = PfandTheme.Title(n > 0 ? CapData.All[i].Name : "???", 8, n > 0 ? Palette.Ink : Palette.Muted);
            name.HorizontalAlignment = HorizontalAlignment.Center;
            v.AddChild(name);
            var count = PfandTheme.Text(n > 1 ? $"×{n}" : n == 1 ? "gefunden" : "fehlt noch", 20, Palette.Muted);
            count.HorizontalAlignment = HorizontalAlignment.Center;
            v.AddChild(count);
            cell.AddChild(v);
            _grid.AddChild(cell);
        }
        _title.Text = $"{found} / {CapData.Count}";
        Visible = true;
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
        => Sprites.Cap(this, Vector2.Zero, Mathf.Floor(Size.X / 12f), CapData.All[Index], Found);
}
