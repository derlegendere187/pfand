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
    readonly List<(Vector2 f, Color c, bool flower)> _decor = new();
    float _time;
    Vector2 _mouse = new(-100, -100);

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        ClipContents = true;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;

        var rng = new Random(7);
        for (int i = 0; i < 200; i++)
            _decor.Add((new Vector2((float)rng.NextDouble(), (float)rng.NextDouble()), rng.Next(2) == 0 ? Palette.TuftDark : Palette.TuftLight, false));
        for (int i = 0; i < 40; i++)
            _decor.Add((new Vector2((float)rng.NextDouble(), (float)rng.NextDouble()), Palette.Flowers[i % 4], true));
    }

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

    void Float(Vector2 pos, string text, Color c)
        => _floats.Add(new FloatText { Pos = pos + new Vector2(0, -14), Text = text, Color = c, Life = 0.9f });

    public override void _Draw()
    {
        var gs = GameState.I;
        var size = Size;
        DrawRect(new Rect2(Vector2.Zero, size), Palette.Grass);
        foreach (var d in _decor)
        {
            var p = (d.f * size).Floor();
            p = new Vector2(p.X - p.X % 4, p.Y - p.Y % 4);
            DrawRect(d.flower ? new Rect2(p, 4, 4) : new Rect2(p, 8, 4), d.c);
        }

        foreach (var r in ParkLayout.Paths(size, gs))
        {
            DrawRect(r, Palette.PathEdge);
            bool horizontal = r.Size.X > r.Size.Y;
            DrawRect(horizontal ? r.Grow(0).GrowIndividual(0, -4, 0, -4) : r.GrowIndividual(-4, 0, -4, 0), Palette.Path);
        }

        bool wiesn = gs.ActiveEvent == "oktoberfest";
        if (wiesn) DrawWiesnGround(size);

        foreach (var v in Sim.Visitors)
            if (v.HasBlanket && v.BlanketOpen > 0)
                Sprites.BlanketOpening(this, v.BlanketPos, v.BlanketColor, v.BlanketOpen);

        if (gs.Level("imbiss") > 0)
        {
            var ir = ParkLayout.ImbissRect(size);
            Sprites.Imbiss(this, ir);
            DrawString(PfandTheme.Pixel, new Vector2(ir.Position.X, ir.Position.Y + 32) + new Vector2(2, 2), "IMBISS",
                HorizontalAlignment.Center, ir.Size.X, 16, Palette.PanelLight);
            DrawString(PfandTheme.Pixel, new Vector2(ir.Position.X, ir.Position.Y + 32), "IMBISS",
                HorizontalAlignment.Center, ir.Size.X, 16, Palette.Ink);
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
            if (binLvl >= 2) Sprites.Pfandring(this, bins[i]);
            Sprites.Bin(this, bins[i], fill, binLvl >= 1 ? 5 : 4);
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

        if (wiesn) DrawWiesnCrowd(gs, size);

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
                Sprites.RolledBlanket(this, pos.Round() + new Vector2(14, 4), v.BlanketColor);
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
        StatusLines(gs, size);


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
        // Füllung
        DrawRect(new Rect2(tip.X - 2 * p, tip.Y - 8 * p, 4 * p, 5 * p), Palette.Gold);
        for (int i = 0; i < 3; i++)
            DrawRect(new Rect2(tip.X - (4 - i) * p + p, tip.Y - (4 - i) * p + p, (8 - 2 * i) * p - 2 * p, p), Palette.Gold);
    }

    // --- Oktoberfest: der ganze Park ist voll ---
    static readonly Vector2[] WiesnTables = { new(0.22f, 0.64f), new(0.64f, 0.74f), new(0.92f, 0.40f), new(0.30f, 0.92f), new(0.27f, 0.09f) };
    static readonly (Vector2 pos, string sign)[] WiesnBuden =
    {
        (new(0.07f, 0.08f), "brezel"), (new(0.93f, 0.08f), "herz"), (new(0.93f, 0.62f), "brezel"),
        (new(0.05f, 0.36f), "herz"), (new(0.56f, 0.64f), "brezel"), (new(0.07f, 0.94f), "herz"),
    };

    /// <summary>Bierbänke voller Leute, Buden und ein Kettenkarussell.</summary>
    void DrawWiesnGround(Vector2 size)
    {
        foreach (var (p, sign) in WiesnBuden) Sprites.Bude(this, p * size, sign, _time);
        Sprites.Kettenkarussell(this, (new Vector2(0.38f, 0.78f) * size).Round(), _time);
        for (int t = 0; t < WiesnTables.Length; t++)
            Sprites.Biertisch(this, (WiesnTables[t] * size).Round(), _time, t);
    }

    /// <summary>Festbesucher drängen sich auf allen Wegen, viele mit Maßkrug.</summary>
    void DrawWiesnCrowd(GameState gs, Vector2 size)
    {
        var paths = ParkLayout.Paths(size, gs);
        if (paths.Count == 0) paths = new List<Rect2> { new(0, size.Y * 0.45f, size.X, 32), new(size.X * 0.45f, 0, 32, size.Y) };
        const int n = 64;
        for (int i = 0; i < n; i++)
        {
            var r = paths[i % paths.Count];
            bool horizontal = r.Size.X > r.Size.Y;
            float h1 = Hash(i * 3 + 1), h2 = Hash(i * 3 + 2), h3 = Hash(i * 3 + 3);
            float speed = 0.015f + h1 * 0.03f;
            float dir = i % 2 == 0 ? 1 : -1;
            float t = ((_time * speed * dir + h2) % 1f + 1f) % 1f;
            float across = 0.2f + 0.6f * h3;
            var pos = horizontal
                ? new Vector2(r.Position.X + t * r.Size.X, r.Position.Y + across * r.Size.Y)
                : new Vector2(r.Position.X + across * r.Size.X, r.Position.Y + t * r.Size.Y);
            pos.Y += Mathf.Round(Mathf.Sin(_time * 9 + i) * 1.5f);
            Sprites.Person(this, pos.Round(), Palette.Shirts[i % Palette.Shirts.Length], Palette.Hair[(i / 3) % Palette.Hair.Length]);
            if (i % 3 == 0) // Maßkrug in der Hand
            {
                DrawRect(new Rect2(pos.Round() + new Vector2(10, 0), 5, 6), Palette.Gold);
                DrawRect(new Rect2(pos.Round() + new Vector2(10, -1), 5, 2), Palette.PanelLight);
            }
        }
    }

    static float Hash(int i)
    {
        uint x = (uint)i * 2654435761u;
        x ^= x >> 13; x *= 0x5bd1e995; x ^= x >> 15;
        return (x & 0xffff) / 65535f;
    }

    void StatusLines(GameState gs, Vector2 size)
    {
        var lines = new List<string>();
        if (gs.ActiveEvent != null && EventDefs.Get(gs.ActiveEvent) is { } ev)
            lines.Add($"{ev.Name} · {GameState.Seconds(gs.EventTimeLeft)}");
        foreach (var kv in gs.ActiveBoosts)
            if (EventDefs.Get(kv.Key) is { } b) lines.Add($"{b.Name} · {GameState.Seconds(kv.Value)}");
        double bonus = gs.EventValueBonus;
        if (bonus > 1.001) lines.Add($"Flaschen ×{bonus.ToString("0.##").Replace('.', ',')} wert");
        var font = PfandTheme.Body;
        float y = size.Y - 14 - (lines.Count - 1) * 22; // unten rechts, da verdeckt es keine Event-Bühne
        foreach (var l in lines)
        {
            var w = font.GetStringSize(l, HorizontalAlignment.Left, -1, 22).X;
            var p = new Vector2(size.X - w - 12, y);
            DrawString(font, p + new Vector2(1, 1), l, HorizontalAlignment.Left, -1, 22, Palette.Ink);
            DrawString(font, p, l, HorizontalAlignment.Left, -1, 22, Palette.PanelLight);
            y += 22;
        }
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
