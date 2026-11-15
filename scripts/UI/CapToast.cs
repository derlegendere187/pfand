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
    public Func<bool> HoldFirst;    // solange true, bleibt die Meldung zum allerersten Kronkorken stehen (Kalle erklärt)

    class Item { public int Cap; public bool IsNew, Hold; public float Life, Max; public float Offset; }
    readonly List<Item> _items = new();
    const float Life = 1.8f;
    int _count;                     // Zähler für schon bekannte Motive
    float _countLife;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        GameState.I.CapFound += OnCapFound;
    }

    public override void _ExitTree() => GameState.I.CapFound -= OnCapFound;

    void OnCapFound(int cap, bool isNew)
    {
        var gs = GameState.I;
        bool first = !gs.CapIntroDone;
        if (first)
        {
            // Allererster Kronkorken: ganz normale Meldung, die stehen bleibt, bis Kalle sie erklärt hat
            gs.CapIntroDone = true;
            gs.Notify();
        }
        // Bekanntes Motiv: nur die Zahl wächst
        if (!isNew && !first)
        {
            if (_countLife <= 0) _count = 0;
            _count++;
            _countLife = Life;
            return;
        }
        // Neue Motive: höchstens ein paar gleichzeitig, jede in einer eigenen Zeile unter dem Zähler
        const int MaxShown = 5;
        while (_items.FindAll(i => !i.Hold).Count >= MaxShown)
            _items.Remove(_items.Find(i => !i.Hold));
        const float baseOff = 30;
        int slot = 0;
        while (_items.Exists(i => Mathf.IsEqualApprox(i.Offset, baseOff + slot * 26))) slot++;
        _items.Add(new Item { Cap = cap, IsNew = true, Hold = first, Life = Life, Max = Life, Offset = baseOff + slot * 26 });
    }

    /// <summary>Wo die stehende Meldung zum ersten Kronkorken ist (für Kalles Markierung). null = keine.</summary>
    public Rect2? HeldRect
    {
        get
        {
            var i = _items.Find(x => x.Hold);
            if (i == null || Anchor == null) return null;
            var pos = ItemPos(i, Anchor());
            float w = PfandTheme.Body.GetStringSize(Text(i), HorizontalAlignment.Left, -1, 22).X;
            return new Rect2(pos + new Vector2(-8, -26), new Vector2(30 + w + 18, 36));
        }
    }

    public override void _Process(double delta)
    {
        if (_items.Count == 0 && _countLife <= 0) return;
        _countLife -= (float)delta;
        foreach (var i in _items)
        {
            if (i.Hold && (HoldFirst?.Invoke() ?? false)) continue;
            i.Hold = false;
            i.Life -= (float)delta;
        }
        _items.RemoveAll(i => i.Life <= 0);
        QueueRedraw();
    }

    static string Text(Item i) => i.IsNew ? "Neuer Kronkorken!" : "+1 Kronkorken";

    // steigt kaum, damit es nicht in den Zähler läuft
    static Vector2 ItemPos(Item i, Vector2 a) => (a + new Vector2(0, i.Offset - (1 - i.Life / i.Max) * 10)).Round();

    public override void _Draw()
    {
        if (Anchor == null) return;
        var a = Anchor();
        if (_countLife > 0)
        {
            float alpha = Mathf.Clamp(_countLife * 2, 0, 1);
            const int size = 22;
            string text = $"+{_count} Kronkorken";
            var tp = (a + new Vector2(0, -2)).Round();
            DrawString(PfandTheme.Body, tp + new Vector2(1, 1), text, HorizontalAlignment.Left, -1, size, new Color(Palette.PanelLight, alpha));
            DrawString(PfandTheme.Body, tp, text, HorizontalAlignment.Left, -1, size, new Color(Palette.GoodText, alpha));
        }
        foreach (var i in _items)
        {
            float alpha = Mathf.Clamp(i.Life * 2, 0, 1);
            var pos = ItemPos(i, a);
            if (alpha > 0.5f) Sprites.Cap(this, pos + new Vector2(0, -18), 2, CapData.All[i.Cap], true);
            var tp = pos + new Vector2(30, -2);
            DrawString(PfandTheme.Body, tp + new Vector2(1, 1), Text(i), HorizontalAlignment.Left, -1, 22, new Color(Palette.PanelLight, alpha));
            DrawString(PfandTheme.Body, tp, Text(i), HorizontalAlignment.Left, -1, 22, new Color(Palette.GoodText, alpha));
        }
    }
}
