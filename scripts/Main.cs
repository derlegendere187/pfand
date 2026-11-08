using System.Collections.Generic;
using System.Linq;
using Godot;
using Pfand.Core;
using Pfand.Park;
using Pfand.UI;

namespace Pfand;

/// <summary>
/// Baut die Oberfläche (Layout nach Skizze), lädt/speichert den Spielstand
/// und kümmert sich um Debug-Startparameter.
///
/// Debug-Parameter (nach "--" an Godot übergeben):
///   --fresh            Spielstand nicht laden und nicht speichern
///   --money=50         Startgeld setzen
///   --late             Park mit einigen Upgrades ausstatten
///   --time=0.7         Tageszeit setzen (0..1)
///   --speed=4          Spiel schneller laufen lassen
///   --screenshot=PFAD  nach --shotdelay Sekunden Bildschirmfoto speichern und beenden
///   --shotdelay=3
/// </summary>
public partial class Main : Control
{
    Dictionary<string, string> _args;
    bool _noSave;
    double _saveAcc;
    string _shotPath;
    double _shotDelay;
    ulong _startMs;
    AlbumPopup _album;
    Label _albumCount;

    public override void _Ready()
    {
        GetTree().AutoAcceptQuit = false;
        _args = ParseArgs();
        var gs = GameState.I;

        _noSave = _args.ContainsKey("fresh");
        if (!_noSave) SaveSystem.Load(gs);
        if (_args.TryGetValue("money", out var m) && double.TryParse(m, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var money)) gs.Money = money;
        if (_args.ContainsKey("late"))
        {
            gs.Upgrades["weg"] = 4; gs.Upgrades["tasche"] = 2; gs.Upgrades["muelleimer"] = 3;
            gs.Upgrades["bank"] = 3; gs.Upgrades["laterne"] = 2;
            gs.Caps[0] = 2; gs.Caps[2] = 1; gs.Caps[4] = 1;
            gs.BottlesFed = 100;
        }
        if (_args.TryGetValue("time", out var t) && double.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var time)) gs.TimeOfDay = time;
        if (_args.TryGetValue("speed", out var s) && double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var speed)) Engine.TimeScale = speed;
        if (_args.TryGetValue("screenshot", out var shot))
        {
            _shotPath = shot;
            _shotDelay = _args.TryGetValue("shotdelay", out var d) && double.TryParse(d, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var dd) ? dd : 3;
        }
        _startMs = Time.GetTicksMsec();

        Theme = PfandTheme.Build();
        BuildUi();
        gs.Changed += RefreshAlbumButton;
        gs.Notify();
    }

    void BuildUi()
    {
        var bg = new ColorRect { Color = Palette.Bg };
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach (var side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 20);
        AddChild(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 14);
        margin.AddChild(root);

        var header = new HeaderBar();
        root.AddChild(header);

        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 20);
        root.AddChild(body);

        // Links: Automat
        var left = Column("AUTOMAT", 200);
        left.AddChild(new AutomatPanel());
        body.AddChild(left);

        // Mitte: Spielfeld
        var middle = Column("SPIELFELD · STADTPARK", 0);
        middle.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        var frame = PfandTheme.Panel(Palette.Border, null, 0);
        frame.AddThemeStyleboxOverride("panel", PfandTheme.Box(Palette.Grass, Palette.Border, 4, 4));
        frame.SizeFlagsVertical = SizeFlags.ExpandFill;
        var park = new ParkView();
        frame.AddChild(park);
        middle.AddChild(frame);
        body.AddChild(middle);
        header.Park = park;

        // Rechts: Upgrades + Album
        var right = Column("UPGRADES", 270);
        right.AddChild(new UpgradePanel());
        var albumButton = new Button { CustomMinimumSize = new Vector2(0, 64) };
        albumButton.AddThemeStyleboxOverride("normal", PfandTheme.Box(Palette.AlbumCover, Palette.Border, 4, 8));
        albumButton.AddThemeStyleboxOverride("hover", PfandTheme.Box(Color.FromHtml("#7d4b33"), Palette.Border, 4, 8));
        albumButton.AddThemeStyleboxOverride("pressed", PfandTheme.Box(Color.FromHtml("#4a2a1c"), Palette.Border, 4, 8));
        var albumRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
        albumRow.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        albumRow.AddThemeConstantOverride("separation", 14);
        var albumTitle = PfandTheme.Title("ALBUM", 16, Palette.AlbumPage);
        albumTitle.VerticalAlignment = VerticalAlignment.Center;
        _albumCount = PfandTheme.Text("0/6", 26, Palette.AlbumPage);
        _albumCount.VerticalAlignment = VerticalAlignment.Center;
        albumRow.AddChild(albumTitle);
        albumRow.AddChild(_albumCount);
        albumButton.AddChild(albumRow);
        right.AddChild(albumButton);
        body.AddChild(right);

        _album = new AlbumPopup();
        AddChild(_album);
        albumButton.Pressed += _album.Open;
    }

    VBoxContainer Column(string title, float width)
    {
        var col = new VBoxContainer { CustomMinimumSize = new Vector2(width, 0) };
        col.AddThemeConstantOverride("separation", 8);
        var l = PfandTheme.Title(title, 8, Palette.Muted);
        l.HorizontalAlignment = HorizontalAlignment.Center;
        col.AddChild(l);
        return col;
    }

    void RefreshAlbumButton()
    {
        int found = GameState.I.Caps.Count(kv => kv.Value > 0);
        _albumCount.Text = $"{found}/{CapData.Count}";
    }

    public override void _Process(double delta)
    {
        _saveAcc += delta;
        if (_saveAcc >= Balance.AutosaveSeconds * Engine.TimeScale)
        {
            _saveAcc = 0;
            Save();
        }

        if (_shotPath != null && (Time.GetTicksMsec() - _startMs) / 1000.0 >= _shotDelay)
        {
            var path = _shotPath;
            _shotPath = null;
            TakeScreenshot(path);
        }
    }

    async void TakeScreenshot(string path)
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var img = GetViewport().GetTexture().GetImage();
        img.SavePng(path);
        GD.Print($"Screenshot gespeichert: {path}");
        GetTree().Quit();
    }

    void Save()
    {
        if (!_noSave) SaveSystem.Save(GameState.I);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            Save();
            GetTree().Quit();
        }
    }

    static Dictionary<string, string> ParseArgs()
    {
        var dict = new Dictionary<string, string>();
        foreach (var a in OS.GetCmdlineUserArgs())
        {
            var s = a.TrimStart('-');
            int eq = s.IndexOf('=');
            if (eq >= 0) dict[s[..eq]] = s[(eq + 1)..];
            else dict[s] = "";
        }
        return dict;
    }
}
