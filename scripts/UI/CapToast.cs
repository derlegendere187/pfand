using System;
using System.Collections.Generic;
using Godot;
using Pfand.Core;
using Pfand.Park;

namespace Pfand.UI;

/// <summary>
/// Dezente Meldung beim Kronkorken-Fund, so wie „Tasche voll!“ im Park:
/// kleiner Deckel mit Text, der im Park neben dem Automaten aufsteigt und verblasst.
/// </summary>
public partial class CapToast : Control
{
    public Func<Vector2> Anchor;    // wo die Meldung startet (im Park, gleich neben dem Automaten)

    class Item { public int Cap; public bool IsNew, Intro; public float Life, Max; public float Offset; }
    readonly List<Item> _items = new();
    const float Life = 1.8f, IntroLife = 3.2f;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        GameState.I.CapFound += OnCapFound;
    }

    public override void _ExitTree() => GameState.I.CapFound -= OnCapFound;

    void OnCapFound(int cap, bool isNew)
    {
        // Mehrere Funde kurz nacheinander leicht versetzt, damit sie sich nicht überdecken
        var gs = GameState.I;
        // Der allererste Kronkorken wird groß gezeigt, erst danach erklärt Kalle
        if (!gs.CapIntroDone && !_items.Exists(i => i.Intro))
        {
            _items.Add(new Item { Cap = cap, IsNew = isNew, Intro = true, Life = IntroLife, Max = IntroLife });
            return;
        }
        float offset = 0;
        foreach (var i in _items) if (i.Life > i.Max - 0.4f) offset += i.Intro ? 60 : 26;
        _items.Add(new Item { Cap = cap, IsNew = isNew, Life = Life, Max = Life, Offset = offset });
    }

    public override void _Process(double delta)
    {
        if (_items.Count == 0) return;
        foreach (var i in _items) i.Life -= (float)delta;
        if (_items.Exists(i => i.Intro && i.Life <= 0))
        {
            GameState.I.CapIntroDone = true;
            GameState.I.Notify();
        }
        _items.RemoveAll(i => i.Life <= 0);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Anchor == null) return;
        var a = Anchor();
        foreach (var i in _items)
        {
            if (i.Intro) { DrawIntro(i, a); continue; }
            float t = 1 - i.Life / i.Max;                      // 0 → 1
            float alpha = Mathf.Clamp(i.Life * 2, 0, 1);
            var pos = (a + new Vector2(0, i.Offset - t * 34)).Round();

            if (alpha > 0.5f) Sprites.Cap(this, pos + new Vector2(0, -18), 2, CapData.All[i.Cap], true);
            string text = i.IsNew ? "Neuer Kronkorken!" : "+1 Kronkorken";
            var tp = pos + new Vector2(30, -2);
            DrawString(PfandTheme.Body, tp + new Vector2(1, 1), text, HorizontalAlignment.Left, -1, 22, new Color(Palette.PanelLight, alpha));
            DrawString(PfandTheme.Body, tp, text, HorizontalAlignment.Left, -1, 22, new Color(Palette.GoodText, alpha));
        }
    }

    /// <summary>Erster Kronkorken: großer Deckel mit leuchtendem Schild, steigt kaum und pulsiert.</summary>
    void DrawIntro(Item i, Vector2 a)
    {
        float t = 1 - i.Life / i.Max;
        float alpha = Mathf.Clamp(i.Life * 2, 0, 1) * Mathf.Clamp(t * 8, 0, 1);
        var pos = (a + new Vector2(0, -t * 12)).Round();
        var font = PfandTheme.Body;
        const string text = "+1 KRONKORKEN!";
        float tw = font.GetStringSize(text, HorizontalAlignment.Left, -1, 32).X;
        var box = new Rect2(pos + new Vector2(-8, -34), new Vector2(64 + tw, 56));
        float glow = 0.5f + 0.5f * Mathf.Sin(t * 30);
        DrawRect(box.Grow(6 + glow * 4), new Color(Palette.Gold, 0.35f * alpha));
        DrawRect(box.Grow(4), new Color(Palette.Ink, alpha));
        DrawRect(box.Grow(2), new Color(Palette.Gold, alpha));
        DrawRect(box, new Color(Color.FromHtml("#3a2a1e"), alpha));
        if (alpha > 0.5f) Sprites.Cap(this, box.Position + new Vector2(6, 4), 4, CapData.All[i.Cap], true);
        var tp = box.Position + new Vector2(60, 38);
        DrawString(font, tp + new Vector2(2, 2), text, HorizontalAlignment.Left, -1, 32, new Color(Palette.Ink, alpha));
        DrawString(font, tp, text, HorizontalAlignment.Left, -1, 32, new Color(Palette.Gold, alpha));
    }
}
