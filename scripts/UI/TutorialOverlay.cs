using Godot;
using Pfand.Core;
using Pfand.Park;

namespace Pfand.UI;

/// <summary>
/// Einführung beim ersten Spielen: Kalle, ein alter Pfandsammler, erklärt in einer Sprechblase,
/// was passiert. Zwischendurch verschwindet er, damit man den ersten Gast beobachten kann.
/// </summary>
public partial class TutorialOverlay : Control
{
    public ParkView Park;
    public AutomatPanel Automat;
    public bool AutoAdvance;        // Debug: läuft von selbst durch
    public System.Func<bool> Busy;  // ein Fenster ist offen: Tipps warten

    static readonly string[] IntroPages =
    {
        "Hallo! Ich bin Kalle. Ich sammle hier im Park schon seit vielen Jahren Pfandflaschen.",
        "Das hier ist deine Wiese. Noch ist hier nichts los, aber das ändern wir zusammen.",
        "Die Leute kommen her, trinken was und lassen ihre leeren Flaschen oft einfach liegen. Für uns ist das bares Geld!",
        "Schau mal, da kommt die erste Person!",
    };
    const string ClickText = "Schau mal, eine Flasche! Die bringt dir Geld. Klick sie an, dann landet sie in deiner Tasche.";
    const string FeedText = "Sehr gut! Links in deiner Tasche ist sie jetzt. Drück auf EINWERFEN, dann zahlt dir der Automat das Pfand aus.";
    static readonly string[] OutroPages =
    {
        "Dein erstes Pfand! Viel ist es noch nicht, aber das wird schon.",
        "Gleich kommen mehr Leute. Mit dem Geld kaufst du rechts Upgrades. Fang mit einem Weg an, dann kommen noch mehr.",
    };

    ColorRect _dim;
    bool _hint;                     // Kalle gibt gerade einen Tipp (außerhalb der Einführung)
    Control _box;
    Label _text;
    Button _next;
    GuideFigure _guide;

    string[] _pages;
    int _page;
    TutorialStep _shownFor = (TutorialStep)(-1);
    double _typed, _wait, _autoT, _hintPause = 1;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        _dim = new ColorRect { Color = new Color(Palette.Ink, 0.35f), MouseFilter = MouseFilterEnum.Stop };
        _dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_dim);

        // Sprechblase unten mittig über dem Spielfeld
        var margin = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_bottom", 40);
        AddChild(margin);
        var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        margin.AddChild(col);
        col.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });
        var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
        col.AddChild(center);

        var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 0);
        center.AddChild(row);
        _box = row;

        _guide = new GuideFigure { CustomMinimumSize = new Vector2(120, 150), SizeFlagsVertical = SizeFlags.ShrinkEnd, MouseFilter = MouseFilterEnum.Ignore };
        row.AddChild(_guide);

        var bubble = PfandTheme.Panel(Palette.PanelLight, Palette.Border, 14);
        bubble.CustomMinimumSize = new Vector2(500, 0);
        row.AddChild(bubble);
        var bcol = new VBoxContainer();
        bcol.AddThemeConstantOverride("separation", 8);
        bubble.AddChild(bcol);
        bcol.AddChild(PfandTheme.Title("KALLE", 8, Palette.Muted));
        _text = PfandTheme.Text("", 24);
        _text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _text.CustomMinimumSize = new Vector2(470, 84);
        bcol.AddChild(_text);
        var brow = new HBoxContainer();
        brow.AddChild(PfandTheme.Spacer());
        _next = new Button { CustomMinimumSize = new Vector2(170, 40) };
        _next.AddThemeFontSizeOverride("font_size", 22);
        _next.Pressed += OnNext;
        brow.AddChild(_next);
        bcol.AddChild(brow);

        Hide();
    }

    public override void _Process(double delta)
    {
        var gs = GameState.I;

        // Fortschritt, den der Spieler selbst macht
        if (gs.Tutorial == TutorialStep.ClickBottle && gs.Bag.Count > 0) gs.Tutorial = TutorialStep.Feed;
        if (gs.Tutorial == TutorialStep.Feed && gs.BottlesFed > 0 && gs.Bag.Count == 0) gs.Tutorial = TutorialStep.Outro;

        if (gs.Tutorial != _shownFor) Enter(gs.Tutorial);

        // Nach der Einführung erklärt Kalle neue Funktionen, sobald sie das erste Mal wichtig werden
        if (!gs.TutorialActive && !Visible && _wait <= 0 && !(Busy?.Invoke() ?? false))
        {
            _hintPause -= delta;
            if (_hintPause <= 0) TryHint(gs);
        }
        gs.ClockPaused = Visible && _dim.Visible;

        if (_wait > 0)
        {
            _wait -= delta;
            if (_wait <= 0) ShowPage();
            return;
        }
        if (!Visible) return;

        // Schreibmaschinen-Effekt
        _typed += delta * 45;
        _text.VisibleCharacters = (int)_typed;
        _guide.Talking = _typed < _text.Text.Length;

        if (AutoAdvance)
        {
            _autoT += delta;
            if (_autoT > 1.5)
            {
                _autoT = 0;
                if (_next.Visible) OnNext();
                else if (gs.Tutorial == TutorialStep.ClickBottle && Park.Sim.Bottles.Count > 0)
                {
                    gs.TryAddToBag(Park.Sim.Bottles[0].Type);
                    Park.Sim.Bottles.RemoveAt(0);
                    gs.Notify();
                }
                else if (gs.Tutorial == TutorialStep.Feed) Automat.StartFeeding();
            }
        }
    }

    void TryHint(GameState gs)
    {
        foreach (var h in KalleHints.All)
        {
            if (gs.SeenHints.Contains(h.Id) || !h.When(gs)) continue;
            gs.SeenHints.Add(h.Id);
            gs.Notify();
            if (h.Skip?.Invoke(gs) ?? false) continue;
            _hint = true;
            _pages = h.Pages;
            _page = 0;
            _wait = 0.6;
            return;
        }
    }

    void Enter(TutorialStep step)
    {
        _shownFor = step;
        _page = 0;
        _wait = 0;
        switch (step)
        {
            case TutorialStep.Intro: _pages = IntroPages; _wait = 0.6; Hide(); break;
            case TutorialStep.ClickBottle: _pages = new[] { ClickText }; _wait = 1.2; Hide(); break;
            case TutorialStep.Feed: _pages = new[] { FeedText }; ShowPage(); break;
            case TutorialStep.Outro: _pages = OutroPages; _wait = 1.0; Hide(); break;
            default: _pages = null; Hide(); break;
        }
    }

    /// <summary>Seiten mit Knopf blenden das Spiel ab; bei Aufgaben (Flasche klicken, einwerfen) bleibt alles bedienbar.</summary>
    void ShowPage()
    {
        if (_pages == null) return;
        var step = _shownFor;
        bool task = step == TutorialStep.ClickBottle || step == TutorialStep.Feed;
        _text.Text = _pages[_page];
        _typed = 0;
        _text.VisibleCharacters = 0;
        _autoT = 0;
        _dim.Visible = !task;
        _next.Visible = !task;
        bool last = _page == _pages.Length - 1;
        _next.Text = !last ? "WEITER"
            : _hint ? "ALLES KLAR"
            : step == TutorialStep.Intro ? "ZUSCHAUEN"
            : "LOS GEHT'S";
        Show();
    }

    void OnNext()
    {
        // Erster Klick zeigt den ganzen Text, der zweite blättert weiter
        if (_typed < _text.Text.Length) { _typed = _text.Text.Length; return; }
        if (_page < _pages.Length - 1) { _page++; ShowPage(); return; }

        var gs = GameState.I;
        if (_hint) { _hint = false; _hintPause = 2; Hide(); return; }
        if (_shownFor == TutorialStep.Intro) gs.Tutorial = TutorialStep.WatchGuest;
        else if (_shownFor == TutorialStep.Outro) gs.Tutorial = TutorialStep.Done;
        gs.Notify();
    }
}

/// <summary>Kalle von vorn: Schiebermütze, Schnauzer, orange Weste, Flasche in der Hand.</summary>
public partial class GuideFigure : Control
{
    public bool Talking;
    float _t;

    public override void _Process(double delta)
    {
        _t += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        const float p = 7;
        float bob = Mathf.Round(Mathf.Sin(_t * 2.5f) * 1.5f);
        var o = new Vector2(Size.X / 2 - 6 * p, Size.Y - 18 * p + bob);
        void R(float x, float y, float w, float h, Color c) => DrawRect(new Rect2(o.X + x * p, o.Y + y * p, w * p, h * p), c);

        var cap = Color.FromHtml("#4f7a5a");
        var capDark = Color.FromHtml("#2f4a38");
        var vest = Color.FromHtml("#e08a4a");
        var vestDark = Color.FromHtml("#b0612e");
        var shirt = Color.FromHtml("#f4ead2");
        var pants = Color.FromHtml("#5a6a7a");
        var hair = Color.FromHtml("#9a9a9a");

        // Schatten
        DrawRect(new Rect2(o.X + 2 * p, Size.Y - p * 0.8f, 8 * p, p * 0.8f), new Color(0, 0, 0, 0.18f));
        // Beine und Schuhe
        R(3, 14, 2, 3, pants); R(7, 14, 2, 3, pants);
        R(2.5f, 16.6f, 3, 0.8f, Palette.Ink); R(6.5f, 16.6f, 3, 0.8f, Palette.Ink);
        // Körper: Hemd, Weste mit Streifen
        R(2, 8, 8, 6, shirt);
        R(2, 8, 3, 6, vest); R(7, 8, 3, 6, vest);
        R(2, 11, 3, 0.6f, Palette.Gold); R(7, 11, 3, 0.6f, Palette.Gold);
        R(2, 13.4f, 8, 0.6f, vestDark);
        // Arme
        R(1, 8.5f, 1, 4, shirt); R(10, 8.5f, 1, 4, shirt);
        R(1, 12.5f, 1, 1, Palette.Skin); R(10, 12.5f, 1, 1, Palette.Skin);
        // Flasche in der rechten Hand (vom Betrachter aus rechts)
        R(10.2f, 10.2f, 0.6f, 1, Color.FromHtml("#2f6a3a"));
        R(9.9f, 11.2f, 1.2f, 2.6f, Color.FromHtml("#4f8a4a"));
        R(10.1f, 11.8f, 0.8f, 0.8f, Palette.PanelLight);
        // Kopf
        R(3, 3, 6, 5, Palette.Skin);
        R(2.5f, 4.5f, 0.5f, 1.5f, Palette.Skin); R(9, 4.5f, 0.5f, 1.5f, Palette.Skin);
        R(3, 6.6f, 1, 1.4f, hair); R(8, 6.6f, 1, 1.4f, hair);
        // Augen (blinzeln ab und zu)
        bool blink = _t % 3.2f < 0.12f;
        R(4, 4.6f, 1, blink ? 0.3f : 1, Palette.Ink); R(7, 4.6f, 1, blink ? 0.3f : 1, Palette.Ink);
        // Schnauzer und Mund
        R(4, 6, 4, 0.7f, hair);
        bool open = Talking && (int)(_t * 8) % 2 == 0;
        R(5, 6.8f, 2, open ? 0.8f : 0.3f, Palette.Ink);
        // Schiebermütze
        R(2.5f, 1.5f, 7, 2, cap);
        R(2.5f, 3, 8.5f, 0.8f, capDark);
        R(3.5f, 1, 5, 0.6f, cap);
    }
}
