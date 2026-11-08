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

    class Item { public int Cap; public bool IsNew; public float Life; public float Offset; }
    readonly List<Item> _items = new();
    const float Life = 1.8f;

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
        float offset = 0;
        foreach (var i in _items) if (i.Life > Life - 0.4f) offset += 26;
        _items.Add(new Item { Cap = cap, IsNew = isNew, Life = Life, Offset = offset });
    }

    public override void _Process(double delta)
    {
        if (_items.Count == 0) return;
        foreach (var i in _items) i.Life -= (float)delta;
        _items.RemoveAll(i => i.Life <= 0);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Anchor == null) return;
        var a = Anchor();
        foreach (var i in _items)
        {
            float t = 1 - i.Life / Life;                       // 0 → 1
            float alpha = Mathf.Clamp(i.Life * 2, 0, 1);
            var pos = (a + new Vector2(0, i.Offset - t * 34)).Round();

            if (alpha > 0.5f) Sprites.Cap(this, pos + new Vector2(0, -18), 2, CapData.All[i.Cap], true);
            string text = i.IsNew ? "Neuer Kronkorken!" : "+1 Kronkorken";
            var tp = pos + new Vector2(30, -2);
            DrawString(PfandTheme.Body, tp + new Vector2(1, 1), text, HorizontalAlignment.Left, -1, 22, new Color(Palette.PanelLight, alpha));
            DrawString(PfandTheme.Body, tp, text, HorizontalAlignment.Left, -1, 22, new Color(Palette.GoodText, alpha));
        }
    }
}
