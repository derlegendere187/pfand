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
        for (int i = _floats.Count - 1; i >= 0; i--)
        {
            _floats[i].Life -= (float)delta;
            _floats[i].Pos += new Vector2(0, -24 * (float)delta);
            if (_floats[i].Life <= 0) _floats.RemoveAt(i);
        }
        QueueRedraw();
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
            bool over = Sim.BottleAt(mm.Position) != null || Sim.BinAt(mm.Position, ParkLayout.Bins(Size, GameState.I)) >= 0;
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
            var content = Sim.BinContents[bin];
            int taken = 0;
            while (content.Count > 0 && gs.TryAddToBag(content[0])) { content.RemoveAt(0); taken++; }
            if (taken > 0) Float(pos, $"+{taken}", Palette.GoodText);
            else if (content.Count > 0) Float(pos, "Tasche voll!", Palette.BadText);
            else Float(pos, "leer", Palette.Muted);
            gs.Notify();
            return;
        }

        var b = Sim.BottleAt(pos);
        if (b == null) return;
        if (gs.TryAddToBag(b.Type))
        {
            Sim.Bottles.Remove(b);
            Float(b.Pos, "+1", Palette.GoodText);
            gs.Notify();
        }
        else if (isClick)
        {
            Float(b.Pos, "Tasche voll!", Palette.BadText);
        }
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
            bool walking = v.State == VisitorState.Kommt || v.State == VisitorState.Geht;
            if (walking) pos.Y += Mathf.Round(Mathf.Sin(v.WalkPhase) * 1.5f);
            Sprites.Person(this, pos.Round(), v.Shirt, v.Hair);
            // Unterwegs trägt man die Decke zusammengerollt
            if (v.HasBlanket && v.BlanketOpen <= 0 && v.State != VisitorState.Sitzt)
                Sprites.RolledBlanket(this, pos.Round() + new Vector2(14, 4), v.BlanketColor);
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

        foreach (var f in _floats)
        {
            var c = new Color(f.Color, Mathf.Clamp(f.Life * 2, 0, 1));
            DrawString(font, f.Pos + new Vector2(1, 1), f.Text, HorizontalAlignment.Left, -1, 22, new Color(Palette.PanelLight, c.A));
            DrawString(font, f.Pos, f.Text, HorizontalAlignment.Left, -1, 22, c);
        }

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
