using System;
using System.Collections.Generic;
using Godot;
using Pfand.Core;
using Pfand.Park;

namespace Pfand.UI;

/// <summary>
/// Dezente Meldung beim Kronkorken-Fund, so wie „Tasche voll!“ im Park.
/// Bekannte Motive laufen in einem Zähler zusammen („+7 Kronkorken“), der bei jedem Fund wächst.
/// Neue Motive steigen einzeln mit ihrem Deckel auf.
/// </summary>
public partial class CapToast : Control
{
    public Func<Vector2> Anchor;    // wo die Meldung startet (im Park, gleich neben dem Automaten)

    class Item { public int Cap; public bool IsNew, Intro; public float Life, Max; public float Offset; }
    readonly List<Item> _items = new();
    const float Life = 1.8f, IntroLife = 3.2f;
    int _count;                     // Zähler für schon bekannte Motive
    float _countLife, _countPop;

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
        // Bekanntes Motiv: nur die Zahl wächst
        if (!isNew)
        {
            if (_countLife <= 0) _count = 0;
            _count++;
            _countLife = Life;
            _countPop = 0.15f;
            return;
        }
        // Neue Motive: höchstens ein paar gleichzeitig, jede in einer eigenen Zeile unter dem Zähler
        const int MaxShown = 5;
        while (_items.FindAll(i => !i.Intro).Count >= MaxShown)
            _items.Remove(_items.Find(i => !i.Intro));
        float baseOff = (_items.Exists(i => i.Intro) ? 60 : 0) + 30;
        int slot = 0;
        while (_items.Exists(i => !i.Intro && Mathf.IsEqualApprox(i.Offset, baseOff + slot * 26))) slot++;
        float offset = baseOff + slot * 26;
        _items.Add(new Item { Cap = cap, IsNew = isNew, Life = Life, Max = Life, Offset = offset });
    }

    public override void _Process(double delta)
    {
        if (_items.Count == 0 && _countLife <= 0) return;
        _countLife -= (float)delta;
        _countPop -= (float)delta;
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
        if (_countLife > 0)
        {
            float alpha = Mathf.Clamp(_countLife * 2, 0, 1);
            int size = _countPop > 0 ? 26 : 22;
            string text = $"+{_count} Kronkorken";
            var tp = (a + new Vector2(0, (_items.Exists(i => i.Intro) ? 60 : 0) - 2)).Round();
            DrawString(PfandTheme.Body, tp + new Vector2(1, 1), text, HorizontalAlignment.Left, -1, size, new Color(Palette.PanelLight, alpha));
            DrawString(PfandTheme.Body, tp, text, HorizontalAlignment.Left, -1, size, new Color(Palette.GoodText, alpha));
        }
        foreach (var i in _items)
        {
            if (i.Intro) { DrawIntro(i, a); continue; }
            float t = 1 - i.Life / i.Max;                      // 0 → 1
            float alpha = Mathf.Clamp(i.Life * 2, 0, 1);
            var pos = (a + new Vector2(0, i.Offset - t * 10)).Round();   // steigt kaum, damit es nicht in den Zähler läuft

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
