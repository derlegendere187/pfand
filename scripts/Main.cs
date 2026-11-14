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
///   --event=konzert    Event sofort starten
///   --flyer=pfandaktion  nach 1 s einen Aktions-Flyer in den Park legen
///   --time=0.7         Tageszeit setzen (0..1)
///   --speed=4          Spiel schneller laufen lassen
///   --screenshot=PFAD  nach --shotdelay Sekunden Bildschirmfoto speichern und beenden
///   --shotdelay=3
///   --window=1920x1080 im Fenster mit dieser Größe starten (Seitenverhältnisse prüfen)
///   --layoutcheck      Überschneidungen fester Plätze ausgeben (mit --wiesn auch Oktoberfest) und beenden
///   --title            Startbildschirm zeigen (sonst springen Debug-Parameter direkt ins Spiel)
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
    AutomatPanel _automat;
    Label _albumCount, _skillCount, _statsCount;
    Button _tabUpgrades, _tabEvents, _albumButton, _skillButton;
    UpgradePanel _upgradePanel;
    ParkView _park;
    HeaderBar _header;
    EventPanel _eventPanel;

    bool _started;
    PauseMenu _pause;

    public override void _Ready()
    {
        GetTree().AutoAcceptQuit = false;
        _args = ParseArgs();
        Theme = PfandTheme.Build();
        // Debug: Fenstergröße erzwingen, um andere Seitenverhältnisse zu prüfen
        if (_args.TryGetValue("window", out var win) && win.Split('x') is { Length: 2 } wh && int.TryParse(wh[0], out var ww) && int.TryParse(wh[1], out var hh))
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
            DisplayServer.WindowSetSize(new Vector2I(ww, hh));
        }
        // Knöpfe nehmen keinen Tastatur-Fokus: sonst löst die Leertaste den zuletzt geklickten Knopf erneut aus
        GetTree().NodeAdded += n => { if (n is BaseButton b) b.FocusMode = FocusModeEnum.None; };

        // Mit Debug-Parametern geht es direkt ins Spiel (im Fenster, wie Godot es öffnet)
        if (_args.Count > 0 && !_args.ContainsKey("title"))
        {
            StartGame(true);
            return;
        }
        Settings.Load();
        if (_args.Count == 0) Settings.Apply();
        if (_args.TryGetValue("screenshot", out var shotT)) { _shotPath = shotT; _shotDelay = 2; _startMs = Time.GetTicksMsec(); }
        var bg = new ColorRect { Color = Wood.Bg };
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);
        var title = new TitleScreen();
        title.Start += cont =>
        {
            title.QueueFree();
            bg.QueueFree();
            StartGame(cont);
        };
        AddChild(title);
    }

    /// <summary>Spiel aufbauen. cont = Spielstand laden, sonst neu mit Einführung.</summary>
    void StartGame(bool cont)
    {
        var gs = GameState.I;
        _noSave = _args.ContainsKey("fresh");
        bool loaded = cont && !_noSave && SaveSystem.Load(gs);
        // Ganz neues Spiel: Kalle erklärt erst einmal alles (Debug-Startwerte überspringen das)
        if (!loaded) gs.Tutorial = TutorialStep.Intro;
        bool debugStart = false;
        foreach (var k in new[] { "notut", "money", "late", "imbiss", "upgrades", "allskills", "skills", "kk", "allcaps", "feedtest", "event", "flyer", "open" })
            if (_args.ContainsKey(k)) { gs.Tutorial = TutorialStep.Done; debugStart = true; }
        if (_args.TryGetValue("money", out var m) && double.TryParse(m, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var money)) gs.Money = money;
        if (_args.ContainsKey("late"))
        {
            gs.Upgrades["weg"] = 4; gs.Upgrades["tasche"] = 2; gs.Upgrades["muelleimer"] = 3;
            gs.Upgrades["bank"] = 3; gs.Upgrades["laterne"] = 2;
            gs.Skills["u_picknick"] = 2;
            gs.Caps[0] = 2; gs.Caps[2] = 1; gs.Caps[4] = 1; gs.Caps[7] = 1;
            gs.Kronkorken = 5;
            gs.BottlesFed = 100;
        }
        if (_args.ContainsKey("allskills"))
            foreach (var sk in SkillDefs.All) if (sk.MaxLevel > 0) gs.Skills[sk.Id] = sk.MaxLevel;
        if (_args.TryGetValue("skills", out var sks))
            foreach (var pair in sks.Split(','))
            {
                var kv = pair.Split(':');
                if (kv.Length == 2 && int.TryParse(kv[1], out var lvl)) gs.Skills[kv[0]] = lvl;
            }
        if (_args.TryGetValue("kk", out var kk) && int.TryParse(kk, out var kkn)) gs.Kronkorken = kkn;
        if (_args.ContainsKey("allcaps"))
            for (int i = 0; i < CapData.Count; i++) gs.Caps[i] = 1;
        if (_args.TryGetValue("event", out var evId) && EventDefs.Get(evId) is { } evDef)
        {
            gs.Skills[evDef.Skill] = 1;
            gs.TryStartEvent(evDef, free: true);
        }
        if (_args.TryGetValue("upgrades", out var ups))
            foreach (var pair in ups.Split(','))
            {
                var kv = pair.Split(':');
                if (kv.Length == 2 && int.TryParse(kv[1], out var lvl)) gs.Upgrades[kv[0]] = lvl;
        if (_args.ContainsKey("layoutcheck"))
        {
            // Prüfmodus: Überschneidungen fester Plätze ausgeben und beenden
            foreach (var line in Pfand.Park.ParkLayout.Check(Pfand.Park.ParkFrame.World, gs, _args.ContainsKey("wiesn")))
                GD.Print("LAYOUT ", line);
            GD.Print("LAYOUT fertig");
            GetTree().Quit();
        }
            }
        if (_args.ContainsKey("imbiss")) gs.Upgrades["imbiss"] = 1;
        if (_args.TryGetValue("time", out var t) && double.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var time)) gs.TimeOfDay = time;
        if (_args.TryGetValue("speed", out var s) && double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var speed)) { Engine.TimeScale = speed; CallDeferred(MethodName.UpdateSpeedLabel); }
        if (_args.TryGetValue("screenshot", out var shot))
        {
            _shotPath = shot;
            _shotDelay = _args.TryGetValue("shotdelay", out var d) && double.TryParse(d, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var dd) ? dd : 3;
        }
        _startMs = Time.GetTicksMsec();

        // Debug-Upgrades brauchen ihren Skill, sonst wären sie unsichtbar
        foreach (var u in UpgradeDefs.All)
            if (u.Skill != null && gs.Level(u.Id) > 0 && !gs.HasSkill(u.Skill)) gs.Skills[u.Skill] = 1;
        // Debug-Start: Kalle erklärt nichts, was schon da ist (--hint=ID zeigt einen Tipp gezielt)
        if (debugStart)
        {
            gs.CapIntroDone = gs.Kronkorken > 0 || gs.Caps.Count > 0 || gs.Skills.Count > 0;
            KalleHints.MarkCurrent(gs);
        }
        if (_args.TryGetValue("hint", out var hintId)) gs.SeenHints.Remove(hintId);

        BuildUi();
        _started = true;
        gs.Changed += RefreshAlbumButton;
        gs.Notify();
        if (_args.TryGetValue("open", out var open))
        {
            if (open == "skills") _skills.CallDeferred(SkillPopup.MethodName.Open);
            else if (open == "album") _album.CallDeferred(AlbumPopup.MethodName.Open);
            else if (open == "stats") _stats.CallDeferred(StatsPopup.MethodName.Open);
            else if (open == "events") ShowTab(true);
        }
        if (_args.TryGetValue("flyer", out var flyerId) && EventDefs.Get(flyerId) is { Short: true } flyerDef)
        {
            gs.Skills[flyerDef.Skill] = 1;
            GetTree().CreateTimer(1.0).Timeout += () => _park.Sim.SpawnFlyer(flyerId);
        }
        if (_args.ContainsKey("captoast")) GetTree().CreateTimer(1.0).Timeout += () => gs.DebugCapFound(0);
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
        var bg = new ColorRect { Color = Wood.Bg };
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach (var side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 16);
        AddChild(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 12);
        margin.AddChild(root);

        var header = _header = new HeaderBar();
        root.AddChild(header);

        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 12);
        root.AddChild(body);

        // Links: Automat
        var left = Column(214);
        _automat = new AutomatPanel();
        left.AddChild(_automat);
        body.AddChild(left);

        // Mitte: Spielfeld
        var middle = Column(0);
        middle.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        var frame = new PanelContainer();
        frame.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        frame.SizeFlagsVertical = SizeFlags.ExpandFill;
        var parkFrame = new Pfand.Park.ParkFrame();
        var park = _park = parkFrame.Park;
        frame.AddChild(parkFrame);
        middle.AddChild(frame);
        body.AddChild(middle);
        header.Park = park;

        // Rechts: Tabs Upgrades/Events, darunter Album + Skilltree
        var right = new VBoxContainer { CustomMinimumSize = new Vector2(276, 0) };
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
        _stats = new StatsPopup();
        AddChild(_stats);
        statsButton.Pressed += _stats.Open;

        // Kronkorken-Meldung steigt im Park neben dem Automaten auf
        AddChild(new CapToast { Anchor = () => park.ToScreen(new Vector2(14, 80)) });

        var tutorial = new TutorialOverlay
        {
            Park = park, Automat = _automat, AutoAdvance = _args.ContainsKey("autotut"),
            Busy = () => _skills.Visible || _album.Visible || _stats.Visible,
            FocusRect = FocusRect,
        };
        AddChild(tutorial);

        _pause = new PauseMenu { Busy = () => _skills.Visible || _album.Visible || _stats.Visible, SaveGame = Save };
        AddChild(_pause);
    }

    /// <summary>Worauf Kalle gerade zeigt, in Bildschirmkoordinaten. null: nichts Bestimmtes.</summary>
    Rect2? FocusRect(string key)
    {
        var gs = GameState.I;
        var sim = _park.Sim;
        var parkRect = _park.GetGlobalRect();
        Rect2 Around(Vector2 p, float r) => _park.ToScreen(new Rect2(p - new Vector2(r, r), new Vector2(2 * r, 2 * r))).Intersection(parkRect.Grow(-8));
        switch (key)
        {
            case "park": return _park.GetGlobalRect();
            case "money": return _header.MoneyRect;
            case "bag": return _automat.BagRect;
            case "feed": return _automat.FeedRect;
            case "bon": return _automat.BonRect;
            case "upgrades": return _upgradePanel.GetGlobalRect();
            case "laterne": return _upgradePanel.CardRect("laterne");
            case "skills": return _skillButton.Visible ? _skillButton.GetGlobalRect() : null;
            case "album": return _albumButton.Visible ? _albumButton.GetGlobalRect() : null;
            case "events": return _tabEvents.Visible ? _tabEvents.GetGlobalRect() : null;
            case "bottle": return sim.Bottles.Count > 0 ? Around(sim.Bottles[0].Pos, 24) : null;
            case "flyer": return sim.Flyer is { } f ? _park.ToScreen(f.Rect.Grow(14)) : null;
            case "collector": return sim.Collectors.Count > 0 ? Around(sim.Collectors[0].Pos, 26) : null;
            case "stranger":
                foreach (var s in sim.Strangers) if (s.Carrying) return Around(s.Pos, 28);
                return Around(gs.TakenAt, 28);
            case "bin":
                var bins = Pfand.Park.ParkLayout.Bins(_park.Size, gs);
                return bins.Count > 0 ? Around(bins[0], 26) : null;
            case "grill":
                var g = Pfand.Park.ParkLayout.Grill(_park.Size);
                var k = Pfand.Park.ParkLayout.KastenRect(_park.Size);
                return _park.ToScreen(k).Merge(Around(g, 44));
        }
        return null;
    }

    Button TabButton(string text)
    {
        var b = new Button { Text = text, ToggleMode = true, CustomMinimumSize = new Vector2(0, 40), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        b.AddThemeFontSizeOverride("font_size", 26);
        // Reiter: aktiver Reiter ist helles Holz, inaktive dunkler
        b.AddThemeStyleboxOverride("normal", PfandTheme.ButtonBox(Wood.Slot, Wood.Plank2, Wood.SlotEdge));
        b.AddThemeStyleboxOverride("hover", PfandTheme.ButtonBox(Wood.Plank2, Wood.GrainLight, Wood.GrainDark));
        b.AddThemeStyleboxOverride("pressed", PfandTheme.ButtonBox(Wood.Plank, Wood.GrainLight, Wood.GrainDark));
        b.AddThemeStyleboxOverride("hover_pressed", PfandTheme.ButtonBox(Wood.Plank, Wood.GrainLight, Wood.GrainDark));
        b.AddThemeColorOverride("font_color", Wood.Muted);
        b.AddThemeColorOverride("font_hover_color", Wood.Ink);
        b.AddThemeColorOverride("font_pressed_color", Wood.Ink);
        b.AddThemeColorOverride("font_hover_pressed_color", Wood.Ink);
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
        var button = new Button { CustomMinimumSize = new Vector2(0, 62), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        button.AddThemeStyleboxOverride("normal", PfandTheme.WoodPanel(6, false));
        button.AddThemeStyleboxOverride("hover", PfandTheme.ButtonBox(Wood.GrainLight, Wood.Nail, Wood.GrainDark));
        button.AddThemeStyleboxOverride("pressed", PfandTheme.ButtonBox(Wood.Plank2, Wood.GrainDark, Wood.Plank2));
        var col = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
        col.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        col.AddThemeConstantOverride("separation", 2);
        var t = PfandTheme.Text(title, 24, Wood.Ink);
        t.HorizontalAlignment = HorizontalAlignment.Center;
        count = PfandTheme.Text("", 22, Wood.Accent);
        count.HorizontalAlignment = HorizontalAlignment.Center;
        col.AddChild(t);
        col.AddChild(count);
        button.AddChild(col);
        return button;
    }

    VBoxContainer Column(float width)
    {
        var col = new VBoxContainer { CustomMinimumSize = new Vector2(width, 0) };
        col.AddThemeConstantOverride("separation", 8);
        return col;
    }

    void RefreshAlbumButton()
    {
        int found = GameState.I.Caps.Count(kv => kv.Value > 0);
        _albumCount.Text = $"{found}/{CapData.Count}";
        _skillCount.Text = $"{GameState.I.Kronkorken} KK";
        // Album und Skilltree erst mit dem Sammelalbum, Events erst nach dem ersten Event-Skill
        _albumButton.Visible = _skillButton.Visible = GameState.I.SkillsVisible;
        bool events = GameState.I.AnyEventUnlocked;
        if (!events && _eventPanel.Visible) ShowTab(false);
        _tabEvents.Visible = events;
    }

    public override void _Process(double delta)
    {
        if (_shotPath != null && (Time.GetTicksMsec() - _startMs) / 1000.0 >= _shotDelay)
        {
            var path = _shotPath;
            _shotPath = null;
            TakeScreenshot(path);
        }
        if (!_started) return;
        GameState.I.PlayTime += delta;
        _statsCount.Text = $"{(int)(GameState.I.PlayTime / 60)} min";
        _saveAcc += delta;
        if (_saveAcc >= Balance.AutosaveSeconds * Engine.TimeScale)
        {
            _saveAcc = 0;
            Save();
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
        if (_started && !_noSave) SaveSystem.Save(GameState.I);
    }

    // --- Nur zum Testen: Zeitraffer per Zifferntaste (gibt es im fertigen Spiel nicht) ---
    static readonly double[] DebugSpeeds = { 1, 2, 5, 10, 20 };
    Label _speedLabel;

    public override void _UnhandledInput(InputEvent e)
    {
        if (!OS.IsDebugBuild() || e is not InputEventKey { Pressed: true, Echo: false } key) return;
        int idx = key.Keycode switch
        {
            Key.Key1 => 0, Key.Key2 => 1, Key.Key3 => 2, Key.Key4 => 3, Key.Key5 => 4, _ => -1,
        };
        if (idx < 0) return;
        Engine.TimeScale = DebugSpeeds[idx];
        UpdateSpeedLabel();
    }

    void UpdateSpeedLabel()
    {
        if (_speedLabel == null)
        {
            _speedLabel = PfandTheme.Title("", 8, Palette.BadText);
            _speedLabel.Position = new Vector2(220, 8);
            _speedLabel.MouseFilter = MouseFilterEnum.Ignore;
            AddChild(_speedLabel);
        }
        _speedLabel.Text = $"TEST: ZEIT x{Engine.TimeScale:0}  (Tasten 1-5)";
        _speedLabel.Visible = Engine.TimeScale != 1;
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
