using System;
using System.Collections.Generic;
using Godot;
using Pfand.Core;
using Pfand.Park;

namespace Pfand.UI;

/// <summary>Bausteine für Menüs im Holz-Stil.</summary>
public static class MenuKit
{
    /// <summary>Großer Menüknopf. Primär = grün, sonst helles Holz.</summary>
    public static Button Button(string text, bool primary = false)
    {
        var b = new Button { Text = text, CustomMinimumSize = new Vector2(340, 54) };
        b.AddThemeFontSizeOverride("font_size", 32);
        if (!primary)
        {
            b.AddThemeStyleboxOverride("normal", PfandTheme.ButtonBox(Wood.Plank2, Wood.GrainLight, Wood.GrainDark));
            b.AddThemeStyleboxOverride("hover", PfandTheme.ButtonBox(Wood.GrainLight, Wood.Nail, Wood.GrainDark));
            b.AddThemeStyleboxOverride("pressed", PfandTheme.ButtonBox(Wood.GrainDark, Wood.Plank2, Wood.GrainDark));
            b.AddThemeColorOverride("font_color", Wood.Ink);
            b.AddThemeColorOverride("font_hover_color", Wood.Ink);
            b.AddThemeColorOverride("font_pressed_color", Wood.Ink);
        }
        return b;
    }

    public static Button Arrow(string text)
    {
        var b = new Button { Text = text, CustomMinimumSize = new Vector2(44, 40) };
        b.AddThemeFontSizeOverride("font_size", 30);
        b.AddThemeStyleboxOverride("normal", PfandTheme.ButtonBox(Wood.Plank2, Wood.GrainLight, Wood.GrainDark, 2));
        b.AddThemeStyleboxOverride("hover", PfandTheme.ButtonBox(Wood.GrainLight, Wood.Nail, Wood.GrainDark, 2));
        b.AddThemeStyleboxOverride("pressed", PfandTheme.ButtonBox(Wood.GrainDark, Wood.Plank2, Wood.GrainDark, 2));
        b.AddThemeColorOverride("font_color", Wood.Ink);
        b.AddThemeColorOverride("font_hover_color", Wood.Ink);
        return b;
    }

    public static PanelContainer Board(float margin = 24)
    {
        var p = new PanelContainer();
        p.AddThemeStyleboxOverride("panel", PfandTheme.WoodPanel(margin));
        return p;
    }

    public static Label Heading(string text, int size = 44)
    {
        var l = PfandTheme.Text(text, size, Wood.Ink);
        l.HorizontalAlignment = HorizontalAlignment.Center;
        return l;
    }
}

/// <summary>Optionen: Anzeigemodus, Auflösung, VSync. Änderungen gelten sofort und werden gespeichert.</summary>
public partial class OptionsPanel : PanelContainer
{
    public event Action Back;
    Label _mode, _res, _vsync;
    Button _resLeft, _resRight;

    public override void _Ready()
    {
        AddThemeStyleboxOverride("panel", PfandTheme.WoodPanel(24));
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 14);
        AddChild(col);
        col.AddChild(MenuKit.Heading("Optionen"));

        _mode = Row(col, "Anzeige", () => Change(() => Settings.Fullscreen = !Settings.Fullscreen), () => Change(() => Settings.Fullscreen = !Settings.Fullscreen), out _, out _);
        _res = Row(col, "Auflösung", () => StepRes(-1), () => StepRes(1), out _resLeft, out _resRight);
        _vsync = Row(col, "VSync", () => Change(() => Settings.VSync = !Settings.VSync), () => Change(() => Settings.VSync = !Settings.VSync), out _, out _);

        var back = MenuKit.Button("ZURÜCK", true);
        back.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        back.Pressed += () => Back?.Invoke();
        col.AddChild(back);
        Refresh();
    }

    Label Row(VBoxContainer col, string name, Action left, Action right, out Button l, out Button r)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        var cap = PfandTheme.Text(name, 30, Wood.Ink);
        cap.CustomMinimumSize = new Vector2(170, 0);
        cap.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(cap);
        l = MenuKit.Arrow("<");
        l.Pressed += left;
        row.AddChild(l);
        var val = PfandTheme.Text("", 30, Wood.Accent);
        val.CustomMinimumSize = new Vector2(280, 0);
        val.HorizontalAlignment = HorizontalAlignment.Center;
        val.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(val);
        r = MenuKit.Arrow(">");
        r.Pressed += right;
        row.AddChild(r);
        col.AddChild(row);
        return val;
    }

    void Change(Action a)
    {
        a();
        Settings.Apply();
        Settings.Save();
        Refresh();
    }

    void StepRes(int dir)
    {
        if (Settings.Fullscreen) return;
        var list = Settings.Resolutions();
        int i = list.IndexOf(Settings.WindowSize);
        if (i < 0)
        {
            // Nächstgrößere bzw. -kleinere Stufe zur aktuellen Größe suchen
            i = 0;
            while (i < list.Count - 1 && list[i].X * list[i].Y < Settings.WindowSize.X * Settings.WindowSize.Y) i++;
            if (dir > 0) i--;
        }
        i = Mathf.Clamp(i + dir, 0, list.Count - 1);
        Change(() => Settings.WindowSize = list[i]);
    }

    void Refresh()
    {
        _mode.Text = Settings.Fullscreen ? "Vollbild" : "Fenster";
        var r = Settings.Fullscreen ? Settings.ScreenSize : Settings.WindowSize;
        _res.Text = $"{r.X} × {r.Y}  ({Settings.Aspect(r)})";
        _res.AddThemeColorOverride("font_color", Settings.Fullscreen ? Wood.Muted : Wood.Accent);
        _resLeft.Disabled = _resRight.Disabled = Settings.Fullscreen;
        _vsync.Text = Settings.VSync ? "An" : "Aus";
    }
}

/// <summary>Startbildschirm: Weiterspielen, Neues Spiel, Optionen, Spiel verlassen. Dahinter liegt eine ruhige Parkszene.</summary>
public partial class TitleScreen : Control
{
    /// <summary>true = Spielstand laden, false = neu anfangen.</summary>
    public event Action<bool> Start;

    Control _menu, _confirm;
    OptionsPanel _options;
    ImageTexture _ground;
    Vector2 _groundSize;
    float _time;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);
        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", 18);
        center.AddChild(stack);

        // Schild mit dem Spielnamen
        var sign = MenuKit.Board(18);
        sign.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        var signRow = new HBoxContainer();
        signRow.AddThemeConstantOverride("separation", 18);
        signRow.AddChild(new BigBottle { CustomMinimumSize = new Vector2(36, 96), SizeFlagsVertical = SizeFlags.ShrinkCenter });
        var name = PfandTheme.Text("Pfand", 110, Wood.Ink);
        name.AddThemeColorOverride("font_shadow_color", Wood.Shadow);
        name.AddThemeConstantOverride("shadow_offset_x", 5);
        name.AddThemeConstantOverride("shadow_offset_y", 5);
        signRow.AddChild(name);
        sign.AddChild(signRow);
        stack.AddChild(sign);

        var holder = new CenterContainer();
        stack.AddChild(holder);

        bool hasSave = FileAccess.FileExists("user://save.json");
        var menu = MenuKit.Board(24);
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 12);
        menu.AddChild(col);
        var cont = MenuKit.Button("WEITERSPIELEN", true);
        cont.Disabled = !hasSave;
        cont.Pressed += () => Start?.Invoke(true);
        col.AddChild(cont);
        var neu = MenuKit.Button("NEUES SPIEL", !hasSave);
        neu.Pressed += () => { if (hasSave) ShowOnly(_confirm); else Start?.Invoke(false); };
        col.AddChild(neu);
        var opt = MenuKit.Button("OPTIONEN");
        opt.Pressed += () => ShowOnly(_options);
        col.AddChild(opt);
        var quit = MenuKit.Button("SPIEL VERLASSEN");
        quit.Pressed += () => GetTree().Quit();
        col.AddChild(quit);
        _menu = menu;
        holder.AddChild(menu);

        _options = new OptionsPanel { Visible = false };
        _options.Back += () => ShowOnly(_menu);
        holder.AddChild(_options);

        // Sicherheitsfrage vor dem Überschreiben
        var confirm = MenuKit.Board(24);
        confirm.Visible = false;
        var ccol = new VBoxContainer();
        ccol.AddThemeConstantOverride("separation", 14);
        confirm.AddChild(ccol);
        ccol.AddChild(MenuKit.Heading("Neues Spiel?", 40));
        var warn = PfandTheme.Text("Alter Spielstand wird gelöscht.", 26, Wood.Muted);
        warn.HorizontalAlignment = HorizontalAlignment.Center;
        ccol.AddChild(warn);
        var crow = new HBoxContainer();
        crow.AddThemeConstantOverride("separation", 12);
        var no = MenuKit.Button("ABBRECHEN");
        no.CustomMinimumSize = new Vector2(220, 54);
        no.Pressed += () => ShowOnly(_menu);
        var yes = MenuKit.Button("NEU STARTEN", true);
        yes.CustomMinimumSize = new Vector2(220, 54);
        yes.Pressed += () => { SaveSystem.Delete(); Start?.Invoke(false); };
        crow.AddChild(no);
        crow.AddChild(yes);
        ccol.AddChild(crow);
        _confirm = confirm;
        holder.AddChild(confirm);

    }

    void ShowOnly(Control c)
    {
        _menu.Visible = c == _menu;
        _options.Visible = c == _options;
        _confirm.Visible = c == _confirm;
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Size.X < 8) return;
        // Ruhige Parkszene: zwei Wege, Bank, Mülleimer, Laterne, ein paar Flaschen
        var s = Size;
        var paths = new List<Rect2>
        {
            new(0, Mathf.Round(s.Y * 0.72f), s.X, 40),
            new(Mathf.Round(s.X * 0.18f), 0, 40, s.Y),
            new(Mathf.Round(s.X * 0.80f), Mathf.Round(s.Y * 0.72f), 40, s.Y),
        };
        if (_ground == null || _groundSize != s) { _ground = GroundArt.Build(s, paths, 11); _groundSize = s; }
        DrawTexture(_ground, Vector2.Zero);

        float py = s.Y * 0.72f;
        Sprites.Bench(this, new Vector2(s.X * 0.32f, py - 34).Round());
        Sprites.Bench(this, new Vector2(s.X * 0.62f, py + 74).Round());
        Sprites.Bin(this, new Vector2(s.X * 0.13f, py - 30).Round(), 0.6f);
        Sprites.Lamp(this, new Vector2(s.X * 0.26f, py - 40).Round());
        Sprites.Bottle(this, new Vector2(s.X * 0.40f, py + 30).Round(), BottleType.Bier, 1.2f, 0, 1.3f, true);
        Sprites.Bottle(this, new Vector2(s.X * 0.70f, py - 20).Round(), BottleType.Wasser, -0.6f, 1, 1.3f, true);
        Sprites.Bottle(this, new Vector2(s.X * 0.88f, s.Y * 0.30f).Round(), BottleType.Dose, 0.3f, 2, 1.3f, true);
        Sprites.Person(this, new Vector2(s.X * 0.34f, py - 48).Round(), Palette.Shirts[0], Palette.Hair[0]);
        Sprites.Person(this, new Vector2(s.X * 0.37f, py - 48).Round(), Palette.Shirts[1], Palette.Hair[1]);

        // Leicht abdunkeln, damit das Menü vorne steht
        DrawRect(new Rect2(Vector2.Zero, s), new Color(Wood.Shadow, 0.45f));
    }
}

/// <summary>Große Flasche fürs Titelschild.</summary>
public partial class BigBottle : Control
{
    public override void _Draw()
    {
        float p = 4;
        var o = new Vector2(Mathf.Round((Size.X - 9 * p) / 2), Mathf.Round((Size.Y - 24 * p) / 2));
        void R(float x, float y, float w, float h, Color c) => DrawRect(new Rect2(o.X + x * p, o.Y + y * p, w * p, h * p), c);
        var g0 = Color.FromHtml("#2e5a2e"); var g1 = Color.FromHtml("#3f7a3a"); var g2 = Color.FromHtml("#6aa05a");
        R(3, 0, 3, 2, Color.FromHtml("#c8a040"));
        R(3, 2, 3, 5, g1); R(3, 2, 1, 5, g2);
        R(2, 7, 5, 2, g1);
        R(1, 9, 7, 15, g1); R(1, 9, 1, 15, g2); R(7, 9, 1, 15, g0);
        R(1, 13, 7, 5, Color.FromHtml("#e8dcc0")); R(2, 15, 5, 1, Color.FromHtml("#8a5a10"));
        R(2, 10, 1, 2, new Color(1, 1, 1, 0.5f));
    }
}

/// <summary>Pause mit Esc: Weiterspielen, Optionen, Spiel verlassen (speichert vorher).</summary>
public partial class PauseMenu : Control
{
    public Func<bool> Busy;
    public Action SaveGame;
    Control _menu;
    OptionsPanel _options;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;

        var dim = new ColorRect { Color = new Color(Wood.Shadow, 0.6f) };
        dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(dim);
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);
        var holder = new VBoxContainer();
        center.AddChild(holder);

        var menu = MenuKit.Board(24);
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 12);
        menu.AddChild(col);
        col.AddChild(MenuKit.Heading("Pause"));
        var cont = MenuKit.Button("WEITERSPIELEN", true);
        cont.Pressed += Close;
        col.AddChild(cont);
        var opt = MenuKit.Button("OPTIONEN");
        opt.Pressed += () => { _menu.Visible = false; _options.Visible = true; };
        col.AddChild(opt);
        var quit = MenuKit.Button("SPEICHERN UND BEENDEN");
        quit.Pressed += () => { SaveGame?.Invoke(); GetTree().Quit(); };
        col.AddChild(quit);
        _menu = menu;
        holder.AddChild(menu);

        _options = new OptionsPanel { Visible = false };
        _options.Back += () => { _options.Visible = false; _menu.Visible = true; };
        holder.AddChild(_options);
    }

    public void Open()
    {
        Visible = true;
        _menu.Visible = true;
        _options.Visible = false;
        GetTree().Paused = true;
    }

    public void Close()
    {
        Visible = false;
        GetTree().Paused = false;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape }) return;
        if (Visible)
        {
            if (_options.Visible) { _options.Visible = false; _menu.Visible = true; }
            else Close();
        }
        else if (Busy?.Invoke() ?? false) return;
        else Open();
        GetViewport().SetInputAsHandled();
    }
}
