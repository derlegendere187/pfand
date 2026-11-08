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
            bool over = Sim.BottlesAround(mm.Position, GameState.I.ClickRadius).Count > 0 || Sim.BinAt(mm.Position, ParkLayout.Bins(Size, GameState.I)) >= 0
                || OverKasten(mm.Position);
            MouseDefaultCursorShape = over ? CursorShape.PointingHand : CursorShape.Arrow;
            // Mit gedrückter Maustaste drüberwischen sammelt auch
            if ((mm.ButtonMask & MouseButtonMask.Left) != 0) TryCollect(mm.Position, false);
        }
    }

    void TryCollect(Vector2 pos, bool isClick)
    {
        var gs = GameState.I;
        var bins = ParkLayout.Bins(Size, gs);
        int bin = Sim.BinAt(pos, bins);
        if (bin >= 0 && isClick)
        {
            EmptyInto(Sim.BinContents[bin], pos);
            return;
        }

        if (OverKasten(pos) && isClick)
        {
            if (gs.HasSkill("lastenrad")) DeliverKasten(pos);
            else EmptyInto(Sim.Kasten, pos);
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

    bool OverKasten(Vector2 pos)
        => GameState.I.Level("grill") > 0 && ParkLayout.KastenRect(Size).Grow(4).HasPoint(pos);

    /// <summary>Eimer oder Kasten in die Tasche leeren, so weit Platz ist.</summary>
    void EmptyInto(List<BottleType> content, Vector2 pos)
    {
        var gs = GameState.I;
        int taken = 0;
        while (content.Count > 0 && gs.TryAddToBag(content[0])) { content.RemoveAt(0); taken++; }
        if (taken > 0) Float(pos, $"+{taken}", Palette.GoodText);
        else if (content.Count > 0) Float(pos, "Tasche voll!", Palette.BadText);
        else Float(pos, "leer", Palette.Muted);
        gs.Notify();
    }

    /// <summary>Lastenrad: der ganze Kasten geht direkt zum Automaten, volle Kästen bringen Kastenpfand.</summary>
    void DeliverKasten(Vector2 pos)
    {
        var gs = GameState.I;
        if (Sim.Kasten.Count == 0) { Float(pos, "leer", Palette.Muted); return; }
        bool full = Sim.Kasten.Count >= Balance.KastenCapacity;
        double sum = 0;
        var rng = new Random();
        foreach (var t in Sim.Kasten) sum += gs.FeedDirect(t, rng).value;
        Sim.Kasten.Clear();
        if (full)
        {
            gs.Money += Balance.KastenPfand;
            gs.TotalEarned += Balance.KastenPfand;
            sum += Balance.KastenPfand;
            gs.KaestenAbgegeben++;
        }
        Float(pos, "+" + GameState.Euro(sum).Replace("€ ", "") + (full ? " KASTEN!" : ""), Palette.MoneyText);
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

        foreach (var b in ParkLayout.Benches(size, gs)) Sprites.Bench(this, b);

        bool grill = gs.Level("grill") > 0;
        bool grillBusy = grill && Sim.GrillBusy;
        if (grill)
        {
            Sprites.Grill(this, ParkLayout.Grill(size), _time, grillBusy);
            Sprites.Kasten(this, ParkLayout.KastenRect(size), Sim.Kasten.Count);
        }

        var tables = ParkLayout.Tables(size, gs);
        foreach (var t in tables) Sprites.Table(this, t);

        if (gs.ActiveEvent != null && EventDefs.Get(gs.ActiveEvent) is { } ev)
            Sprites.EventProps(this, ev.Kind, Sim.EventCenter, _time, Sim.Bierball?.BottleDown ?? false, Sim.Bierball?.Wobble ?? 0);

        if (gs.Level("sammler") > 0) Sprites.Depot(this, ParkLayout.Depot(size));

        var bins = ParkLayout.Bins(size, gs);
        for (int i = 0; i < bins.Count; i++)
        {
            float fill = i < Sim.BinContents.Count ? Sim.BinContents[i].Count / (float)Balance.BinCapacity : 0;
            Sprites.Bin(this, bins[i], fill);
        }

        foreach (var b in Sim.Bottles)
        {
            float pop = Mathf.Min(1f, b.Age * 5f);
            Sprites.Bottle(this, b.Pos, b.Type, b.Rot, b.ColorIdx, 0.6f + 0.4f * pop);
        }

        foreach (var v in Sim.Visitors)
        {
            var pos = v.Pos;
            bool walking = v.State == VisitorState.Kommt || v.State == VisitorState.Geht
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

        if (gs.BottlesFed == 0 && gs.Bag.Count == 0)
            Hint("Klicke auf Flaschen, um sie einzusammeln.");
        else if (gs.BottlesFed == 0 && gs.Bag.Count >= 3)
            Hint("Wirf die Flaschen links im Automaten ein.");
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
