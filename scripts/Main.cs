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
///   --fresh            Spielstand nicht laden und nicht speichern (startet mit der Einführung)
///   --notut            Einführung überspringen
///   --autotut          Einführung läuft von selbst durch (zum Testen)
///   --hint=muelleimer  einen Tipp von Kalle erneut zeigen, sobald er zutrifft
///   --money=50         Startgeld setzen
///   --late             Park mit einigen Upgrades ausstatten
///   --imbiss           Imbissbude freischalten und bauen
///   --upgrades=a:1,b:2 Upgrade-Stufen setzen (z. B. sammler:2,tischtennis:1)
///   --allskills        alle Skills voll lernen
///   --skills=wert:2,aoe:1  einzelne Skill-Stufen setzen
///   --kk=20            Kronkorken setzen
///   --feedtest         Tasche füllen und automatisch einwerfen
///   --ausbau=bank:0:2,muelleimer:1:2  Ausbaustufen setzen (Sorte:Nummer:Stufe)
///   --event=konzert    Event sofort starten
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
    SkillPopup _skills;
    StatsPopup _stats;
    AusbauPopup _ausbau;
    AutomatPanel _automat;
    Label _albumCount, _skillCount, _statsCount;
    Button _tabUpgrades, _tabEvents, _albumButton, _skillButton;
    UpgradePanel _upgradePanel;
    EventPanel _eventPanel;

    public override void _Ready()
    {
        GetTree().AutoAcceptQuit = false;
        _args = ParseArgs();
        var gs = GameState.I;

        _noSave = _args.ContainsKey("fresh");
        bool loaded = !_noSave && SaveSystem.Load(gs);
        // Ganz neues Spiel: Kalle erklärt erst einmal alles (Debug-Startwerte überspringen das)
        if (!loaded) gs.Tutorial = TutorialStep.Intro;
        bool debugStart = false;
        foreach (var k in new[] { "notut", "money", "late", "imbiss", "upgrades", "allskills", "skills", "kk", "feedtest", "ausbau", "event", "open" })
            if (_args.ContainsKey(k)) { gs.Tutorial = TutorialStep.Done; debugStart = true; }
        if (_args.TryGetValue("money", out var m) && double.TryParse(m, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var money)) gs.Money = money;
        if (_args.ContainsKey("late"))
        {
            gs.Upgrades["weg"] = 4; gs.Upgrades["tasche"] = 2; gs.Upgrades["muelleimer"] = 3;
            gs.Upgrades["bank"] = 3; gs.Upgrades["laterne"] = 2;
            gs.Upgrades["picknick"] = 2; gs.Upgrades["album"] = 1;
            gs.Caps[0] = 2; gs.Caps[2] = 1; gs.Caps[4] = 1; gs.Caps[7] = 1;
            gs.Kronkorken = 5;
            gs.BottlesFed = 100;
        }
        if (_args.ContainsKey("allskills") || _args.ContainsKey("kk") || _args.ContainsKey("skills") || _args.ContainsKey("event"))
            gs.Upgrades["album"] = 1;
        if (_args.ContainsKey("allskills"))
            foreach (var sk in SkillDefs.All) if (sk.MaxLevel > 0) gs.Skills[sk.Id] = sk.MaxLevel;
        if (_args.TryGetValue("skills", out var sks))
            foreach (var pair in sks.Split(','))
            {
                var kv = pair.Split(':');
                if (kv.Length == 2 && int.TryParse(kv[1], out var lvl)) gs.Skills[kv[0]] = lvl;
            }
        if (_args.TryGetValue("kk", out var kk) && int.TryParse(kk, out var kkn)) gs.Kronkorken = kkn;
        if (_args.TryGetValue("event", out var evId) && EventDefs.Get(evId) is { } evDef)
        {
            gs.Skills[evDef.Skill] = 1;
            gs.TryStartEvent(evDef, free: true);
        }
        if (_args.TryGetValue("ausbau", out var aus))
            foreach (var pair in aus.Split(','))
            {
                var kv = pair.Split(':');
                if (kv.Length == 3 && int.TryParse(kv[2], out var lvl)) gs.Ausbau[kv[0] + ":" + kv[1]] = lvl;
            }
        if (_args.TryGetValue("upgrades", out var ups))
            foreach (var pair in ups.Split(','))
            {
                var kv = pair.Split(':');
                if (kv.Length == 2 && int.TryParse(kv[1], out var lvl)) gs.Upgrades[kv[0]] = lvl;
            }
        if (_args.ContainsKey("imbiss")) { gs.Upgrades["imbiss"] = 1; gs.PeakVisitors = Balance.ImbissUnlockVisitors; }
        if (_args.TryGetValue("time", out var t) && double.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var time)) gs.TimeOfDay = time;
        if (_args.TryGetValue("speed", out var s) && double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var speed)) Engine.TimeScale = speed;
        if (_args.TryGetValue("screenshot", out var shot))
        {
            _shotPath = shot;
            _shotDelay = _args.TryGetValue("shotdelay", out var d) && double.TryParse(d, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var dd) ? dd : 3;
        }
        _startMs = Time.GetTicksMsec();

        // Debug-Start: Kalle erklärt nichts, was schon da ist (--hint=ID zeigt einen Tipp gezielt)
        if (debugStart) KalleHints.MarkCurrent(gs);
        if (_args.TryGetValue("hint", out var hintId)) gs.SeenHints.Remove(hintId);

        Theme = PfandTheme.Build();
        BuildUi();
        gs.Changed += RefreshAlbumButton;
        gs.Notify();
        if (_args.TryGetValue("open", out var open))
        {
            if (open == "skills") _skills.CallDeferred(SkillPopup.MethodName.Open);
            else if (open == "album") _album.CallDeferred(AlbumPopup.MethodName.Open);
            else if (open == "stats") _stats.CallDeferred(StatsPopup.MethodName.Open);
            else if (open.StartsWith("ausbau:") && open.Split(':') is { Length: 3 } a && int.TryParse(a[2], out var ai))
                _ausbau.CallDeferred(AusbauPopup.MethodName.Open, a[1], ai);
            else if (open == "events") ShowTab(true);
        }
        if (_args.ContainsKey("feedtest"))
        {
            // Tasche mit gemischten Flaschen füllen und nach 1 s einwerfen (zum Prüfen des Pfandbons)
            var types = new[] { BottleType.Bier, BottleType.Dose, BottleType.Plastik, BottleType.Wasser };
            for (int i = 0; i < gs.BagCapacity; i++) gs.Bag.Add(types[i % types.Length]);
            GetTree().CreateTimer(1.0).Timeout += _automat.StartFeeding;
        }
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
        _automat = new AutomatPanel();
        left.AddChild(_automat);
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

        // Rechts: Tabs Upgrades/Events, darunter Album + Skilltree
        var right = new VBoxContainer { CustomMinimumSize = new Vector2(270, 0) };
        right.AddThemeConstantOverride("separation", 8);
        var tabs = new HBoxContainer();
        tabs.AddThemeConstantOverride("separation", 6);
        _tabUpgrades = TabButton("UPGRADES");
        _tabEvents = TabButton("EVENTS");
        tabs.AddChild(_tabUpgrades);
        tabs.AddChild(_tabEvents);
        right.AddChild(tabs);
        _upgradePanel = new UpgradePanel();
        _eventPanel = new EventPanel { Visible = false };
        right.AddChild(_upgradePanel);
        right.AddChild(_eventPanel);
        _tabUpgrades.Pressed += () => ShowTab(false);
        _tabEvents.Pressed += () => ShowTab(true);
        ShowTab(false);

        var bottom = new HBoxContainer();
        bottom.AddThemeConstantOverride("separation", 8);
        var albumButton = BrownButton("ALBUM", out _albumCount);
        var skillButton = BrownButton("SKILLS", out _skillCount);
        var statsButton = BrownButton("STATS", out _statsCount);
        bottom.AddChild(albumButton);
        bottom.AddChild(skillButton);
        _albumButton = albumButton;
        _skillButton = skillButton;
        bottom.AddChild(statsButton);
        right.AddChild(bottom);
        body.AddChild(right);

        _album = new AlbumPopup();
        AddChild(_album);
        albumButton.Pressed += _album.Open;
        _skills = new SkillPopup();
        AddChild(_skills);
        skillButton.Pressed += _skills.Open;
        _ausbau = new AusbauPopup();
        AddChild(_ausbau);
        park.ObjectClicked += _ausbau.Open;
        _stats = new StatsPopup();
        AddChild(_stats);
        statsButton.Pressed += _stats.Open;

        var tutorial = new TutorialOverlay
        {
            Park = park, Automat = _automat, AutoAdvance = _args.ContainsKey("autotut"),
            Busy = () => _skills.Visible || _album.Visible || _stats.Visible || _ausbau.Visible,
        };
        AddChild(tutorial);
    }

    Button TabButton(string text)
    {
        var b = new Button { Text = text, ToggleMode = true, CustomMinimumSize = new Vector2(0, 40), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        b.AddThemeFontOverride("font", PfandTheme.Pixel);
        b.AddThemeFontSizeOverride("font_size", 8);
        b.AddThemeStyleboxOverride("normal", PfandTheme.Box(Palette.PanelShade, Palette.Border, 4, 6));
        b.AddThemeStyleboxOverride("hover", PfandTheme.Box(Palette.Panel, Palette.Border, 4, 6));
        b.AddThemeStyleboxOverride("pressed", PfandTheme.Box(Palette.PanelLight, Palette.Border, 4, 6));
        b.AddThemeStyleboxOverride("hover_pressed", PfandTheme.Box(Palette.PanelLight, Palette.Border, 4, 6));
        return b;
    }

    void ShowTab(bool events)
    {
        _upgradePanel.Visible = !events;
        _eventPanel.Visible = events;
        _tabUpgrades.SetPressedNoSignal(!events);
        _tabEvents.SetPressedNoSignal(events);
    }

    Button BrownButton(string title, out Label count)
    {
        var button = new Button { CustomMinimumSize = new Vector2(0, 64), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        button.AddThemeStyleboxOverride("normal", PfandTheme.Box(Palette.AlbumCover, Palette.Border, 4, 6));
        button.AddThemeStyleboxOverride("hover", PfandTheme.Box(Color.FromHtml("#7d4b33"), Palette.Border, 4, 6));
        button.AddThemeStyleboxOverride("pressed", PfandTheme.Box(Color.FromHtml("#4a2a1c"), Palette.Border, 4, 6));
        var col = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
        col.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        col.AddThemeConstantOverride("separation", 2);
        var t = PfandTheme.Title(title, 8, Palette.AlbumPage);
        t.HorizontalAlignment = HorizontalAlignment.Center;
        count = PfandTheme.Text("", 24, Palette.AlbumPage);
        count.HorizontalAlignment = HorizontalAlignment.Center;
        col.AddChild(t);
        col.AddChild(count);
        button.AddChild(col);
        return button;
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
        _skillCount.Text = $"{GameState.I.Kronkorken} KK";
        // Album und Skilltree erst mit dem Sammelalbum, Events erst nach dem ersten Event-Skill
        _albumButton.Visible = _skillButton.Visible = GameState.I.CapsUnlocked;
        bool events = GameState.I.AnyEventUnlocked;
        if (!events && _eventPanel.Visible) ShowTab(false);
        _tabEvents.Visible = events;
    }

    public override void _Process(double delta)
    {
        GameState.I.PlayTime += delta;
        _statsCount.Text = $"{(int)(GameState.I.PlayTime / 60)} min";
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
