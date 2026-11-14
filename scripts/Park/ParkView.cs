using System;
using System.Collections.Generic;
using Godot;
using Pfand.Core;
using Pfand.UI;

namespace Pfand.Park;

/// <summary>Das Spielfeld: zeichnet den Park von oben und nimmt Klicks auf Flaschen und Eimer an.</summary>
public partial class ParkView : Control
{
    public readonly VisitorSim Sim = new();


    class FloatText { public Vector2 Pos; public string Text; public Color Color; public float Life; }
    readonly List<FloatText> _floats = new();
    ImageTexture _ground;
    string _groundKey = "";
    float _time;
    Vector2 _mouse = new(-100, -100);

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        ClipContents = true;
    }

    /// <summary>Parkkoordinate in Bildschirmkoordinate (der Park ist skaliert).</summary>
    public Vector2 ToScreen(Vector2 p) => GetGlobalTransform() * p;

    public Rect2 ToScreen(Rect2 r) => new(ToScreen(r.Position), r.Size * GetGlobalTransform().Scale);

    public override void _Process(double delta)
    {
        _time += (float)delta;
        Sim.Size = Size;
        Sim.Update(delta, GameState.I);
        foreach (var f in Sim.PendingFloats) Float(f.pos, f.text, f.color);
        Sim.PendingFloats.Clear();
        for (int i = _floats.Count - 1; i >= 0; i--)
        {
            _floats[i].Life -= (float)delta;
            _floats[i].Pos += new Vector2(0, -24 * (float)delta);
            if (_floats[i].Life <= 0) _floats.RemoveAt(i);
        }
        QueueRedraw();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationMouseExit) _mouse = new Vector2(-100, -100);
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && mb.Pressed)
        {
            TryCollect(mb.Position, true);
            AcceptEvent();
        }
        else if (e is InputEventMouseMotion mm)
        {
            _mouse = mm.Position;
            bool overBottle = Sim.BottlesAround(mm.Position, GameState.I.ClickRadius).Count > 0;
            bool over = overBottle || OverKasten(mm.Position) || CrateAt(mm.Position) != null
                || Sim.BinAt(mm.Position, ParkLayout.Bins(Size, GameState.I)) >= 0 || (Sim.Flyer?.Rect.HasPoint(mm.Position) ?? false);
            MouseDefaultCursorShape = over ? CursorShape.PointingHand : CursorShape.Arrow;
            // Mit gedrückter Maustaste drüberwischen sammelt auch
            if ((mm.ButtonMask & MouseButtonMask.Left) != 0) TryCollect(mm.Position, false);
        }
    }

    void TryCollect(Vector2 pos, bool isClick)
    {
        var gs = GameState.I;

        // Flyer: nur ein echter Klick direkt drauf startet die Aktion (Wischen und Greifzange zählen nicht)
        if (isClick && Sim.Flyer is { } flyer && flyer.Rect.Grow(2).HasPoint(pos))
        {
            var ev = EventDefs.Get(flyer.EventId);
            Sim.Flyer = null;
            gs.TryStartEvent(ev, free: true);
            Float(pos, ev.Name + "!", Palette.MoneyText);
            return;
        }

        // Solange der Automat zählt, passt nichts Neues in die Tasche
        if (gs.Feeding)
        {
            if (isClick) Float(pos, "Automat zählt …", Palette.Muted);
            return;
        }

        var bins = ParkLayout.Bins(Size, gs);
        int bin = Sim.BinAt(pos, bins);
        if (bin >= 0 && (isClick || Sim.BinContents[bin].Count > 0))
        {
            EmptyInto(Sim.BinContents[bin], pos, isClick);
            return;
        }

        // Grill-Bierkasten: Inhalt in die Tasche. Mit Lastenrad bringt ein voller Kasten danach Kastenpfand
        if (OverKasten(pos) && (isClick || Sim.Kasten.Count > 0))
        {
            bool full = Sim.Kasten.Count >= Balance.KastenCapacity;
            EmptyInto(Sim.Kasten, pos, isClick);
            if (full && Sim.Kasten.Count == 0 && gs.HasSkill("lastenrad")) KastenPfand(pos);
            return;
        }

        // Kasten einer Freundesgruppe: erst leeren, wenn die Gruppe weg ist
        if (CrateAt(pos) is { } crate)
        {
            if (crate.Owner != null)
            {
                if (isClick) Float(pos, "Gehört noch wem!", Palette.BadText);
                return;
            }
            if (isClick || crate.Bottles.Count > 0) EmptyInto(crate.Bottles, pos, isClick);
            if (crate.Bottles.Count == 0)
            {
                // Mit Lastenrad nimmt man den leeren Kasten mit
                if (gs.HasSkill("lastenrad")) { Sim.Crates.Remove(crate); KastenPfand(pos + new Vector2(0, -20)); }
                Sim.CleanupCrates();
            }
            return;
        }

        // Greifzange: alle Flaschen im Umkreis, die nächsten zuerst
        var hits = Sim.BottlesAround(pos, gs.ClickRadius);
        if (hits.Count == 0) return;
        int got = 0;
        foreach (var b in hits)
        {
            if (!gs.TryAddToBag(b.Type)) break;
            Sim.Bottles.Remove(b);
            got++;
        }
        gs.CollectedByHand += got;
        if (got > 0)
        {
            Float(hits[0].Pos, $"+{got}", Palette.GoodText);
            gs.Notify();
        }
        else if (isClick)
        {
            Float(hits[0].Pos, "Tasche voll!", Palette.BadText);
        }
    }

    GroupCrate CrateAt(Vector2 pos)
    {
        foreach (var c in Sim.Crates)
            if (c.Visible && CrateRect(c).Grow(4).HasPoint(pos)) return c;
        return null;
    }

    static Rect2 CrateRect(GroupCrate c) => new(c.Pos, new Vector2(26, 32));

    bool OverKasten(Vector2 pos)
        => GameState.I.Level("grill") > 0 && ParkLayout.KastenRect(Size).Grow(4).HasPoint(pos);

    /// <summary>Eimer oder Kasten in die Tasche leeren, so weit Platz ist.</summary>
    void EmptyInto(List<BottleType> content, Vector2 pos, bool loud = true)
    {
        var gs = GameState.I;
        int taken = 0;
        while (content.Count > 0 && gs.TryAddToBag(content[0])) { content.RemoveAt(0); taken++; }
        gs.CollectedByHand += taken;
        if (taken > 0) Float(pos, $"+{taken}", Palette.GoodText);
        else if (!loud) return;
        else if (content.Count > 0) Float(pos, "Tasche voll!", Palette.BadText);
        else Float(pos, "leer", Palette.Muted);
        gs.Notify();
    }

    /// <summary>Lastenrad: den leeren Kasten zurückbringen.</summary>
    void KastenPfand(Vector2 pos)
    {
        var gs = GameState.I;
        gs.Money += Balance.KastenPfand;
        gs.TotalEarned += Balance.KastenPfand;
        gs.KaestenAbgegeben++;
        Float(pos, "+" + GameState.Euro(Balance.KastenPfand).Replace("€ ", "") + " KASTEN!", Palette.MoneyText);
        gs.Notify();
    }

    void EnsureGround(Vector2 size, List<Rect2> paths)
    {
        int w = (int)size.X, h = (int)size.Y;
        if (w < 8 || h < 8) return;
        string key = $"{w}x{h}:{paths.Count}";
        if (key == _groundKey && _ground != null) return;
        _groundKey = key;
        _ground = GroundArt.Build(size, paths);
    }

    void Float(Vector2 pos, string text, Color c)
        => _floats.Add(new FloatText { Pos = pos + new Vector2(0, -14), Text = text, Color = c, Life = 0.9f });

    public override void _Draw()
    {
        var gs = GameState.I;
        var size = Size;
        var paths = ParkLayout.Paths(size, gs);
        EnsureGround(size, paths);
        if (_ground != null) DrawTexture(_ground, Vector2.Zero);

        bool wiesn = gs.ActiveEvent == "oktoberfest";
        if (wiesn) DrawWiesnGround(size);

        foreach (var v in Sim.Visitors)
            if (v.HasBlanket && v.BlanketOpen > 0)
                Sprites.BlanketOpening(this, v.BlanketPos, v.BlanketColor, v.BlanketStyle, v.BlanketOpen);

        if (gs.Level("imbiss") > 0)
        {
            var ir = ParkLayout.ImbissRect(size);
            Sprites.Imbiss(this, ir);
            DrawString(PfandTheme.Pixel, new Vector2(ir.Position.X, ir.Position.Y + 26), "IMBISS",
                HorizontalAlignment.Center, ir.Size.X, 28, Color.FromHtml("#b8432f"));
        }

        var benchList = ParkLayout.Benches(size, gs);
        for (int i = 0; i < benchList.Count; i++)
        {
            int lvl = gs.AusbauLevel("bank", i);
            Sprites.Bench(this, benchList[i]);
            if (lvl >= 1) Sprites.PicnicTable(this, benchList[i] + ParkLayout.BenchOpposite(i, true));
            if (lvl >= 2) Sprites.Bench(this, benchList[i] + ParkLayout.BenchOpposite(i));
        }

        bool grill = gs.Level("grill") > 0;
        bool grillBusy = grill && Sim.GrillBusy;
        if (grill)
        {
            Sprites.Grill(this, ParkLayout.Grill(size), _time, grillBusy);
            Sprites.Kasten(this, ParkLayout.KastenRect(size), Sim.Kasten.Count);
        }
        foreach (var c in Sim.Crates)
            if (c.Visible) Sprites.Kasten(this, CrateRect(c), c.Bottles.Count);

        var tables = ParkLayout.Tables(size, gs);
        foreach (var t in tables) Sprites.Table(this, t);

        if (gs.ActiveEvent != null && EventDefs.Get(gs.ActiveEvent) is { } ev)
            Sprites.EventProps(this, ev.Kind, Sim.EventCenter, _time, Sim.Bierball?.BottleDown ?? false, Sim.Bierball?.Wobble ?? 0);

        if (gs.Level("sammler") > 0) Sprites.Depot(this, ParkLayout.Depot(size));

        var bins = ParkLayout.Bins(size, gs);
        for (int i = 0; i < bins.Count; i++)
        {
            float fill = i < Sim.BinContents.Count ? Sim.BinContents[i].Count / (float)gs.BinCapacity(i) : 0;
            int binLvl = gs.AusbauLevel("muelleimer", i);
            Sprites.Bin(this, bins[i], fill, binLvl >= 1 ? 5 : 4, binLvl >= 2);
        }

        foreach (var b in Sim.Bottles)
        {
            float pop = Mathf.Min(1f, b.Age * 5f);
            Sprites.Bottle(this, b.Pos, b.Type, b.Rot, b.ColorIdx, (0.6f + 0.4f * pop) * 1.3f, true);
        }
        // Jede Flasche funkelt alle paar Sekunden kurz auf (zeitversetzt)
        foreach (var b in Sim.Bottles)
        {
            float phase = (_time * 0.5f + (b.Pos.X * 0.013f + b.Pos.Y * 0.007f)) % 1f;
            if (phase < 0.12f) Sprites.Sparkle(this, (b.Pos + new Vector2(6, -10)).Round(), Mathf.Sin(phase / 0.12f * Mathf.Pi));
        }

        if (wiesn)
        {
            UpdateSoeder(gs, size);
            DrawWiesnCrowd(gs, size);
        }
        else _sLast = -1;

        foreach (var v in Sim.Visitors)
        {
            var pos = v.Pos;
            if (v.Wagon) Sprites.Bollerwagen(this, (v.Pos + new Vector2(-22, 10)).Round(), v.PauseTimer <= 0 ? _time : 0);
            bool walking = v.State == VisitorState.Kommt || v.State == VisitorState.Geht || v.State == VisitorState.Wandert
                || (Sim.Bierball != null && v == Sim.Bierball.Runner);
            if (walking) pos.Y += Mathf.Round(Mathf.Sin(v.WalkPhase) * 1.5f);
            if (v.Team >= 0)
            {
                // Bierball: zur Mitte hin ausholen, eigene Flasche vor den Füßen
                float dir = v.Team == 0 ? 1 : -1;
                pos.X += dir * v.Lunge * 8;
                if (v.State == VisitorState.Sitzt && v.DrinkAnim < 0.5f)
                    Sprites.Bottle(this, (v.Target + new Vector2(dir * 16, 8)).Round(), BottleType.Bier, 0, 0, 0.8f);
            }
            Sprites.Person(this, pos.Round(), v.Shirt, v.Hair);
            if (v.DrinkAnim >= 0.5f)
            {
                // Flasche am Mund, schräg nach oben
                float side = v.Team == 1 ? -1 : 1;
                Sprites.Bottle(this, (pos + new Vector2(side * 9, -6)).Round(), BottleType.Bier, side * -2.4f, 0, 0.8f);
            }
            // Unterwegs trägt man die Decke zusammengerollt
            if (v.HasBlanket && v.BlanketOpen <= 0 && v.State != VisitorState.Sitzt)
                Sprites.RolledBlanket(this, pos.Round() + new Vector2(14, 4), v.BlanketColor, v.BlanketStyle);
        }

        // Tischtennisball fliegt hin und her, wenn beide Spieler da sind
        for (int i = 0; i < tables.Count; i++)
        {
            if (!Sim.TableActive(i)) continue;
            float t = (Mathf.Sin(_time * 4 + i) + 1) / 2;
            var half = ParkLayout.TableSize.Y / 2;
            Sprites.Ball(this, tables[i] + new Vector2(Mathf.Sin(_time * 7 + i) * 8, Mathf.Lerp(-half, half, t)).Round());
        }

        if (grillBusy) Sprites.Smoke(this, ParkLayout.Grill(size), _time);

        // Feiertag: Wimpelkette über den Park
        if (gs.ActiveEvent == "feiertag") Sprites.Garland(this, size.X, _time);
        if (gs.ActiveEvent == "oktoberfest")
        {
            // Der ganze Park ist Wiesn: blau-weiße Wimpel quer drüber, Buden in den Ecken
            Sprites.Garland(this, size.X, _time, 10, true);
            Sprites.Garland(this, size.X, _time + 0.5f, size.Y * 0.47f, true);
            Sprites.Garland(this, size.X, _time + 0.25f, size.Y * 0.76f, true);
        }
        if (Sim.Bierball is { Phase: BierballGame.Step.Fliegt } bb)
        {
            float t = 1 - (float)(bb.Timer / BierballGame.FlyTime);
            var fp = bb.FlyFrom.Lerp(bb.FlyTo, t) + new Vector2(0, -Mathf.Sin(t * Mathf.Pi) * 36);
            Sprites.Bottle(this, fp.Round(), BottleType.Bier, t * 12, 0, 0.9f);
        }

        if (Sim.Flyer is { } fl) DrawFlyer(fl);

        // Laufende Events und Aktionen unten rechts


        foreach (var s in Sim.Strangers)
        {
            var pos = s.Pos + new Vector2(0, Mathf.Round(Mathf.Sin(s.WalkPhase) * 1.5f));
            Sprites.Stranger(this, pos.Round(), s.Carrying);
        }

        foreach (var c in Sim.Collectors)
        {
            var pos = c.Pos;
            if (c.State != CollectorState.Wartet) pos.Y += Mathf.Round(Mathf.Sin(c.WalkPhase) * 1.5f);
            Sprites.Collector(this, pos.Round(), c.Carry.Count);
        }

        // Abend und Nacht abdunkeln
        float dark = Darkness(gs.TimeOfDay);
        if (dark > 0) DrawRect(new Rect2(Vector2.Zero, size), new Color(Palette.Night, dark));

        foreach (var l in ParkLayout.Lamps(size, gs))
        {
            if (dark > 0.05f) Sprites.Glow(this, l, Mathf.Clamp(dark * 2.5f, 0, 1));
            Sprites.Lamp(this, l);
        }

        if (wiesn) DrawSoeder(gs, size);

        // Eimer-Füllstand als Zahl
        var font = PfandTheme.Body;
        for (int i = 0; i < bins.Count && i < Sim.BinContents.Count; i++)
        {
            int n = Sim.BinContents[i].Count;
            if (n > 0) DrawString(font, bins[i] + new Vector2(12, -10), n.ToString(), HorizontalAlignment.Left, -1, 20, Palette.PanelLight);
        }
        foreach (var c in Sim.Crates)
            if (c.Visible && c.Bottles.Count > 0)
                DrawString(font, c.Pos + new Vector2(30, 12), c.Bottles.Count.ToString(), HorizontalAlignment.Left, -1, 20, Palette.PanelLight);
        if (grill && Sim.Kasten.Count > 0)
        {
            var kr = ParkLayout.KastenRect(size);
            DrawString(font, kr.Position + new Vector2(kr.Size.X + 4, 12), Sim.Kasten.Count.ToString(), HorizontalAlignment.Left, -1, 20, Palette.PanelLight);
        }

        foreach (var f in _floats)
        {
            var c = new Color(f.Color, Mathf.Clamp(f.Life * 2, 0, 1));
            DrawString(font, f.Pos + new Vector2(1, 1), f.Text, HorizontalAlignment.Left, -1, 22, new Color(Palette.PanelLight, c.A));
            DrawString(font, f.Pos, f.Text, HorizontalAlignment.Left, -1, 22, c);
        }

        if (gs.SkillLevel("aoe") > 0 && new Rect2(Vector2.Zero, size).HasPoint(_mouse))
            DrawArc(_mouse, gs.ClickRadius, 0, Mathf.Tau, 32, new Color(Palette.PanelLight, 0.6f), 2);

        // Einführung: hüpfender Pfeil über der ersten Flasche
        if (gs.Tutorial == TutorialStep.ClickBottle)
            foreach (var b in Sim.Bottles) Arrow(b.Pos + new Vector2(0, -30 - Mathf.Abs(Mathf.Sin(_time * 5)) * 8));

        if (gs.TutorialActive || gs.ClockPaused) { }
        else if (gs.BottlesFed == 0 && gs.Bag.Count == 0)
            Hint("Klicke auf Flaschen, um sie einzusammeln.");
        else if (gs.BottlesFed == 0 && gs.Bag.Count >= 3)
            Hint("Wirf die Flaschen links im Automaten ein.");
    }

    /// <summary>Aktions-Flyer: ein Zettel mit Symbol, wippt im Wind und blinkt, bevor er wegweht.</summary>
    void DrawFlyer(Flyer f)
    {
        if (f.Life < 2.5 && Mathf.Sin(_time * 18) < -0.3f) return;
        var r = f.Rect;
        r.Position += new Vector2(0, Mathf.Round(Mathf.Sin(_time * 3) * 2));
        var ev = EventDefs.Get(f.EventId);
        var accent = f.EventId switch
        {
            "happyhour" => Palette.BadText,
            "gluecksstraehne" => Palette.GoodText,
            _ => Palette.Gold,
        };
        // Leuchtender Rand, damit man ihn sieht
        float glow = 0.5f + 0.5f * Mathf.Sin(_time * 6);
        DrawRect(r.Grow(5), new Color(Palette.Gold, 0.35f + 0.35f * glow));
        DrawRect(r.Grow(2), Palette.Ink);
        DrawRect(r, Palette.Paper);
        DrawRect(new Rect2(r.Position, r.Size.X, 6), accent);
        string sign = f.EventId switch { "happyhour" => "2x", "gluecksstraehne" => "KK", _ => "€" };
        DrawString(PfandTheme.Body, new Vector2(r.Position.X, r.End.Y - 4), sign, HorizontalAlignment.Center, r.Size.X, 20, Palette.Ink);
        var label = ev?.Name ?? "";
        var font = PfandTheme.Body;
        float w = font.GetStringSize(label, HorizontalAlignment.Left, -1, 18).X;
        var lp = new Vector2(r.GetCenter().X - w / 2, r.Position.Y - 8).Round();
        DrawString(font, lp + new Vector2(1, 1), label, HorizontalAlignment.Left, -1, 18, Palette.Ink);
        DrawString(font, lp, label, HorizontalAlignment.Left, -1, 18, Palette.PanelLight);
    }

    /// <summary>Pixelpfeil nach unten, Spitze bei tip.</summary>
    void Arrow(Vector2 tip)
    {
        tip = tip.Round();
        const float p = 3;
        // Umriss
        DrawRect(new Rect2(tip.X - 3 * p, tip.Y - 9 * p, 6 * p, 6 * p), Palette.Ink);
        for (int i = 0; i < 4; i++)
            DrawRect(new Rect2(tip.X - (5 - i) * p, tip.Y - (4 - i) * p, (10 - 2 * i) * p, p), Palette.Ink);
        DrawRect(new Rect2(tip.X - p, tip.Y, 2 * p, p), Palette.Ink); // Spitze unten zu
        // Füllung
        DrawRect(new Rect2(tip.X - 2 * p, tip.Y - 8 * p, 4 * p, 5 * p), Palette.Gold);
        for (int i = 0; i < 3; i++)
            DrawRect(new Rect2(tip.X - (4 - i) * p + p, tip.Y - (4 - i) * p + p, (8 - 2 * i) * p - 2 * p, p), Palette.Gold);
    }

    // --- Oktoberfest: der ganze Park ist voll ---
    /// <summary>Bierbänke voller Leute, Buden und ein Kettenkarussell.</summary>
    void DrawWiesnGround(Vector2 size)
    {
        foreach (var (p, sign) in ParkLayout.WiesnBuden) Sprites.Bude(this, ParkLayout.At(p, size), sign, _time);
        Sprites.Kettenkarussell(this, ParkLayout.At(ParkLayout.KarussellAt, size), _time);
        Sprites.Maibaum(this, ParkLayout.At(ParkLayout.MaibaumAt, size), _time);
        for (int t = 0; t < ParkLayout.WiesnTables.Length; t++)
            Sprites.Biertisch(this, ParkLayout.At(ParkLayout.WiesnTables[t], size), _time, t);
    }

    // --- Oktoberfest: Markus Söder schlendert über den Hauptweg und redet ---
    static readonly string[] SoederLines =
    {
        "Servus mitanand, i bin da Markus!",
        "Bayern is einfach des schönste Land.",
        "Erst amoi a Brezn.",
        "O'zapft is!",
        "A Maß geht immer.",
        "Des is Heimat, Leit!",
        "Pfand sammeln? Vorbildlich!",
        "Schaut's euch des Zelt o. Sauber!",
        "Mia san mia.",
    };

    enum SoederAct { Walk, Talk, Drink, Photo, Flip }

    /// <summary>Was er nach jedem Laufstück macht; der Salto kommt selten.</summary>
    static readonly SoederAct[] SoederPlan =
    {
        SoederAct.Talk, SoederAct.Drink, SoederAct.Talk, SoederAct.Photo, SoederAct.Talk,
        SoederAct.Flip, SoederAct.Talk, SoederAct.Drink, SoederAct.Photo, SoederAct.Talk,
    };

    const float SoederSpeed = 30f;
    // Salto-Ablauf in Sekunden: rufen, Traube bildet sich, Sprung, Jubel, alle gehen weiter
    const float FlipShout = 1.2f, FlipGather = 2.3f, FlipJump = 3.2f, FlipCheer = 4.6f, FlipEnd = 6f;

    SoederAct _sAct = SoederAct.Walk;
    float _sT, _sX = -1, _sLast = -1, _crowdTime;
    int _sDir = 1, _sCount;
    Vector2 _sPos;

    // Festbesucher: jeder läuft seinen Weg; wer bei Söder mitmacht, geht zu einem Platz bei ihm und danach weiter
    const int CrowdN = 64;
    const float CrowdWalk = 60f;              // so schnell gehen sie zu Söder und zurück (px/s)
    const float PhotoFlash = 2.4f, PhotoRelease = 3.3f;
    readonly float[] _lag = new float[CrowdN], _pull = new float[CrowdN];
    readonly bool[] _member = new bool[CrowdN];
    readonly Vector2[] _slot = new Vector2[CrowdN], _crowdPos = new Vector2[CrowdN];
    int _fan = -1;

    static float SoederDuration(SoederAct a) => a switch
    {
        SoederAct.Walk => 4f, SoederAct.Talk => 3.5f, SoederAct.Drink => 2.4f,
        SoederAct.Photo => 4f, _ => FlipEnd,
    };

    /// <summary>Söder bewegt sich auf dem Hauptweg hin und her und wechselt zwischen Laufen und Aktionen.</summary>
    void UpdateSoeder(GameState gs, Vector2 size)
    {
        float dt = _sLast < 0 ? 0 : Mathf.Clamp(_time - _sLast, 0, 0.1f);
        _sLast = _time;
        var paths = ParkLayout.Paths(size, gs);
        Rect2 track = paths.Count > 0 && paths[0].Size.X > paths[0].Size.Y ? paths[0] : new Rect2(0, size.Y * 0.45f, size.X, 32);
        if (_sX < 0) _sX = size.X * 0.3f;
        _sT += dt;
        // Beim Salto bleibt die Menge stehen und schaut zu, statt durch die Traube zu laufen
        float crowdDt = dt * (1 - FlipClear());
        _crowdTime += crowdDt;
        if (_sAct == SoederAct.Walk)
        {
            _sX += _sDir * SoederSpeed * dt;
            if (_sX > size.X - 60) { _sX = size.X - 60; _sDir = -1; }
            if (_sX < 60) { _sX = 60; _sDir = 1; }
        }
        if (_sT >= SoederDuration(_sAct))
        {
            _sT = 0;
            if (_sAct == SoederAct.Walk)
            {
                _sAct = SoederPlan[_sCount % SoederPlan.Length];
                _sPos = new Vector2(_sX, track.GetCenter().Y + 2).Round();
                if (_sAct == SoederAct.Photo && !PickFan(gs, size)) _sAct = SoederAct.Talk;
                if (_sAct == SoederAct.Flip) PickRing(gs, size);
            }
            else { _sAct = SoederAct.Walk; _sCount++; }
        }
        _sPos = new Vector2(_sX, track.GetCenter().Y + 2).Round();

        // Mitmacher wieder loslassen
        if (_sAct == SoederAct.Photo && _sT > PhotoRelease && _fan >= 0) _member[_fan] = false;
        if (_sAct == SoederAct.Flip && _sT > FlipCheer + 0.2f) Array.Clear(_member);
        if (_sAct == SoederAct.Walk) { Array.Clear(_member); _fan = -1; }

        var cpaths = CrowdPaths(gs, size);
        for (int i = 0; i < CrowdN; i++)
        {
            float goal = _member[i] ? 1 : 0;
            if (_pull[i] == goal) continue;
            float dist = (_slot[i] - CrowdBase(i, cpaths)).Length();
            _pull[i] = Mathf.MoveToward(_pull[i], goal, CrowdWalk * dt / Mathf.Max(dist, 8));
        }
        // Wer mitmacht, bleibt auf seinem Weg stehen und läuft danach von dort weiter
        for (int i = 0; i < CrowdN; i++) _lag[i] += crowdDt * _pull[i];
    }

    /// <summary>Wie stark die Leute beim Salto Platz machen (0 bis 1).</summary>
    float FlipClear()
    {
        if (_sAct != SoederAct.Flip) return 0;
        float up = Mathf.SmoothStep(FlipShout - 0.5f, FlipShout + 0.5f, _sT);
        float down = 1 - Mathf.SmoothStep(FlipCheer + 0.3f, FlipEnd, _sT);
        return up * down;
    }

    List<Rect2> CrowdPaths(GameState gs, Vector2 size)
    {
        var paths = ParkLayout.Paths(size, gs);
        if (paths.Count == 0) paths = new List<Rect2> { new(0, size.Y * 0.45f, size.X, 32), new(size.X * 0.45f, 0, 32, size.Y) };
        return paths;
    }

    /// <summary>Wo Festbesucher i gerade auf seinem Weg ist.</summary>
    Vector2 CrowdBase(int i, List<Rect2> paths)
    {
        var r = paths[i % paths.Count];
        bool horizontal = r.Size.X > r.Size.Y;
        float h1 = Hash(i * 3 + 1), h2 = Hash(i * 3 + 2), h3 = Hash(i * 3 + 3);
        float speed = 0.015f + h1 * 0.03f;
        float dir = i % 2 == 0 ? 1 : -1;
        float t = (((_crowdTime - _lag[i]) * speed * dir + h2) % 1f + 1f) % 1f;
        float across = 0.2f + 0.6f * h3;
        return horizontal
            ? new Vector2(r.Position.X + t * r.Size.X, r.Position.Y + across * r.Size.Y)
            : new Vector2(r.Position.X + across * r.Size.X, r.Position.Y + t * r.Size.Y);
    }

    /// <summary>Der nächste Festbesucher kommt fürs Foto neben Söder.</summary>
    bool PickFan(GameState gs, Vector2 size)
    {
        var paths = CrowdPaths(gs, size);
        int best = -1; float bestD = 140;
        for (int i = 0; i < CrowdN; i++)
        {
            float d = CrowdBase(i, paths).DistanceTo(_sPos);
            if (d < bestD && d > 4) { bestD = d; best = i; }
        }
        if (best < 0) return false;
        float side = CrowdBase(best, paths).X < _sPos.X ? -1 : 1;
        _fan = best;
        _member[best] = true;
        _slot[best] = _sPos + new Vector2(side * 26, -2);
        return true;
    }

    /// <summary>Die Leute in der Nähe bilden eine Traube um ihn, jeder auf seiner Seite.</summary>
    void PickRing(GameState gs, Vector2 size)
    {
        var paths = CrowdPaths(gs, size);
        var near = new List<(int i, float d, float a)>();
        for (int i = 0; i < CrowdN; i++)
        {
            var d = CrowdBase(i, paths) - _sPos;
            if (d.Length() < 210) near.Add((i, d.Length(), Mathf.Atan2(d.Y / 0.6f, d.X)));
        }
        near.Sort((x, y) => x.d.CompareTo(y.d));
        if (near.Count > 12) near.RemoveRange(12, near.Count - 12);
        near.Sort((x, y) => x.a.CompareTo(y.a));
        // Winkel auseinanderziehen, damit keiner auf dem anderen steht
        var ang = new float[near.Count];
        float gap = Mathf.Tau / 13;
        for (int k = 0; k < near.Count; k++)
            ang[k] = k == 0 ? near[k].a : Mathf.Max(near[k].a, ang[k - 1] + gap);
        var c = _sPos + new Vector2(0, 6);
        for (int k = 0; k < near.Count; k++)
        {
            int i = near[k].i;
            _member[i] = true;
            _slot[i] = (c + new Vector2(Mathf.Cos(ang[k]) * 58, Mathf.Sin(ang[k]) * 34)).Round();
        }
    }

    void DrawSoeder(GameState gs, Vector2 size)
    {
        var pos = _sPos;
        float t = _sT;
        bool walking = _sAct == SoederAct.Walk;
        if (walking) pos.Y += Mathf.Round(Mathf.Sin(_time * 9) * 1.5f);
        var feet = pos + new Vector2(0, 10);

        switch (_sAct)
        {
            case SoederAct.Walk:
                Sprites.Soeder(this, feet, _time, true);
                break;
            case SoederAct.Talk:
                Sprites.Soeder(this, feet, _time, false);
                SpeechBubble(pos + new Vector2(0, -37), SoederLines[_sCount % SoederLines.Length], size);
                break;
            case SoederAct.Drink:
            {
                bool up = t > 0.3f && t < SoederDuration(SoederAct.Drink) - 0.3f;
                Sprites.Soeder(this, feet, _time, false, up ? 1 : 0);
                if (t < 0.9f) SpeechBubble(pos + new Vector2(0, -37), "Prost!", size);
                break;
            }
            case SoederAct.Photo:
                DrawSoederSelfie(feet, t, size);
                break;
            case SoederAct.Flip:
                DrawSoederFlip(feet, t, size);
                break;
        }
    }

    /// <summary>Ein Besucher aus der Menge kommt dazu, hält das Handy hoch, Blitz, geht weiter.</summary>
    void DrawSoederSelfie(Vector2 feet, float t, Vector2 size)
    {
        Sprites.Soeder(this, feet, _time, false);
        bool ready = _fan >= 0 && _pull[_fan] > 0.95f && _member[_fan];
        if (ready)
        {
            var fan = _crowdPos[_fan];
            float side = fan.X < feet.X ? -1 : 1;
            var shirt = Sprites.Tracht[_fan % Sprites.Tracht.Length];
            // Arm hoch, Handy vor beiden
            var hand = fan + new Vector2(-side * 8, -26);
            DrawRect(new Rect2(fan + new Vector2(-side * 8 - 2, -16), 4, 10), Palette.Ink);
            DrawRect(new Rect2(fan + new Vector2(-side * 8 - 1, -15), 2, 9), shirt);
            DrawRect(new Rect2(hand - new Vector2(4, 6), 8, 12), Palette.Ink);
            DrawRect(new Rect2(hand - new Vector2(3, 5), 6, 10), Color.FromHtml("#3a4250"));
            if (t > PhotoFlash && t < PhotoFlash + 0.18f)
                DrawCircle(hand, 26, new Color(1, 1, 1, 0.75f));
        }
        string line = t < PhotoFlash ? "Foto? Freilich, kommt's her!" : "Sauber!";
        SpeechBubble(feet + new Vector2(0, -47), line, size);
    }

    /// <summary>Salto: er ruft, alle machen Platz, die Leute in der Nähe stellen sich drumherum, Sprung, Jubel.</summary>
    void DrawSoederFlip(Vector2 feet, float t, Vector2 size)
    {
        if (t > FlipGather && t < FlipJump)
        {
            float k = (t - FlipGather) / (FlipJump - FlipGather);
            float h = 4 * 38 * k * (1 - k);
            float angle = -Mathf.Tau * Mathf.SmoothStep(0.05f, 0.95f, k);
            // Schatten bleibt am Boden
            DrawRect(new Rect2(feet + new Vector2(-9 + 3 * k * (1 - k), -1), 18 - 12 * k * (1 - k), 3), new Color(0, 0, 0, 0.18f));
            var center = (feet + new Vector2(0, -23 - h)).Round();
            DrawSetTransform(center, angle, Vector2.One);
            Sprites.Soeder(this, new Vector2(0, 23), _time, false, 0, false);
            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
            // Ein paar Bierspritzer
            for (int i = 0; i < 4; i++)
            {
                var d = center + new Vector2((i - 1.5f) * 9, -8 - k * 26 - i % 2 * 5);
                DrawRect(new Rect2(d.Round(), 2, 2), new Color(Color.FromHtml("#e8b040"), 1 - k * 0.6f));
            }
        }
        else
        {
            // kurz in die Knie vor dem Sprung
            bool crouch = t > FlipGather - 0.25f && t <= FlipGather;
            Sprites.Soeder(this, feet + new Vector2(0, crouch ? 2 : 0), _time, false);
        }

        string line = t < FlipGather ? "Platz da, Leit! Geht's amoi weg!" : t > FlipJump + 0.1f && t < FlipCheer + 0.6f ? "Zack! So geht des in Bayern." : null;
        if (line != null) SpeechBubble(feet + new Vector2(0, -47), line, size);
    }

    /// <summary>Pixel-Sprechblase mit Spitze nach unten zu anchor, bleibt im Park.</summary>
    void SpeechBubble(Vector2 anchor, string text, Vector2 size)
    {
        var font = PfandTheme.Body;
        const int fs = 20;
        const float maxW = 190, pad = 7;
        var ts = font.GetMultilineStringSize(text, HorizontalAlignment.Left, maxW, fs);
        var box = new Rect2(anchor.X - ts.X / 2 - pad, anchor.Y - 10 - ts.Y - 2 * pad, ts.X + 2 * pad, ts.Y + 2 * pad);
        box.Position = new Vector2(Mathf.Clamp(box.Position.X, 4, size.X - box.Size.X - 4), Mathf.Max(4, box.Position.Y)).Round();
        DrawRect(new Rect2(box.Position + new Vector2(3, 3), box.Size), new Color(0, 0, 0, 0.25f));
        DrawRect(box.Grow(2), Palette.Ink);
        DrawRect(box, Palette.PanelLight);
        // Spitze
        float tx = Mathf.Clamp(anchor.X, box.Position.X + 8, box.End.X - 14);
        DrawRect(new Rect2(tx - 2, box.End.Y, 10, 2), Palette.Ink);
        DrawRect(new Rect2(tx, box.End.Y, 6, 2), Palette.PanelLight);
        DrawRect(new Rect2(tx, box.End.Y + 2, 6, 2), Palette.Ink);
        DrawRect(new Rect2(tx + 2, box.End.Y + 2, 2, 2), Palette.PanelLight);
        DrawRect(new Rect2(tx + 2, box.End.Y + 4, 2, 2), Palette.Ink);
        DrawMultilineString(font, box.Position + new Vector2(pad, pad + fs * 0.75f), text, HorizontalAlignment.Left, maxW, fs, -1, Palette.Ink);
    }

    /// <summary>Festbesucher drängen sich auf allen Wegen, viele mit Maßkrug.</summary>
    void DrawWiesnCrowd(GameState gs, Vector2 size)
    {
        var paths = CrowdPaths(gs, size);
        float clear = FlipClear();
        bool cheer = _sAct == SoederAct.Flip && _sT > FlipJump && _sT < FlipCheer + 0.4f;
        for (int i = 0; i < CrowdN; i++)
        {
            var pos = CrowdBase(i, paths);
            if (_pull[i] > 0)
            {
                pos = pos.Lerp(_slot[i], Mathf.SmoothStep(0, 1, _pull[i]));
                if (cheer && _pull[i] > 0.9f) pos.Y -= Mathf.Round(Mathf.Abs(Mathf.Sin(_time * 10 + i)) * 4);
            }
            else if (clear > 0)
            {
                // Beim Salto: wer nicht zuschaut, macht Platz
                var d = pos - _sPos;
                float dist = d.Length();
                const float room = 110;
                if (dist < room)
                {
                    var dirOut = dist < 0.5f ? new Vector2(i % 2 == 0 ? 1 : -1, 0) : d / dist;
                    pos = _sPos + dirOut * Mathf.Lerp(dist, room, clear);
                }
            }
            if (_pull[i] < 0.95f) pos.Y += Mathf.Round(Mathf.Sin(_time * 9 + i) * 1.5f);
            pos = pos.Round();
            _crowdPos[i] = pos;
            Sprites.Person(this, pos, Sprites.Tracht[i % Sprites.Tracht.Length], Palette.Hair[(i / 3) % Palette.Hair.Length]);
            if (i % 4 == 1) Sprites.Hat(this, pos);
            if (i % 3 == 0) // Maßkrug in der Hand
            {
                var m = pos + new Vector2(10, -2);
                DrawRect(new Rect2(m - Vector2.One, 8, 10), Palette.Ink);
                DrawRect(new Rect2(m, 6, 8), Color.FromHtml("#e8b040"));
                DrawRect(new Rect2(m, 6, 2), Palette.PanelLight);
            }
        }
    }

    static float Hash(int i)
    {
        uint x = (uint)i * 2654435761u;
        x ^= x >> 13; x *= 0x5bd1e995; x ^= x >> 15;
        return (x & 0xffff) / 65535f;
    }

    void Hint(string text)
    {
        var font = PfandTheme.Body;
        var ts = font.GetStringSize(text, HorizontalAlignment.Left, -1, 22);
        var r = new Rect2(12, Size.Y - 12 - 36, ts.X + 24, 36);
        DrawRect(r, Palette.Border);
        DrawRect(r.Grow(-4), Palette.Panel);
        DrawString(font, new Vector2(r.Position.X + 12, r.Position.Y + 25), text, HorizontalAlignment.Left, -1, 22, Palette.Ink);
    }

    static float Darkness(double t)
    {
        if (t < Balance.EveningStart) return t < 0.04 ? (float)((0.04 - t) / 0.04 * 0.5) : 0f;
        if (t < Balance.NightStart) return (float)((t - Balance.EveningStart) / (Balance.NightStart - Balance.EveningStart) * 0.35);
        double nightPos = (t - Balance.NightStart) / (1 - Balance.NightStart);
        return (float)(0.35 + 0.15 * Math.Min(1, nightPos * 3));
    }
}
