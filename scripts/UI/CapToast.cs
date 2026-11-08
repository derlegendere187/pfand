using System.Collections.Generic;
using Godot;
using Pfand.Core;
using Pfand.Park;

namespace Pfand.UI;

/// <summary>
/// Kurze Meldung oben über dem Spielfeld, wenn ein Kronkorken gefunden wurde:
/// der Deckel mit Motiv, sein Name und ob er neu fürs Album ist.
/// </summary>
public partial class CapToast : Control
{
    const float Width = 330, Height = 74, ShowTime = 2.6f, FadeTime = 0.3f;

    readonly Queue<(int cap, bool isNew)> _queue = new();
    (int cap, bool isNew)? _current;
    float _t;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        GameState.I.CapFound += OnCapFound;
    }

    public override void _ExitTree() => GameState.I.CapFound -= OnCapFound;

    void OnCapFound(int cap, bool isNew) => _queue.Enqueue((cap, isNew));

    public override void _Process(double delta)
    {
        if (_current == null)
        {
            if (_queue.Count == 0) return;
            _current = _queue.Dequeue();
            _t = 0;
        }
        _t += (float)delta;
        // Kommen mehrere schnell hintereinander, wird jede kürzer gezeigt
        float show = _queue.Count > 0 ? 1.2f : ShowTime;
        if (_t > show) _current = null;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_current is not { } c) return;
        var motif = CapData.All[c.cap];

        // Einblenden von oben, ausblenden am Ende
        float show = _queue.Count > 0 ? 1.2f : ShowTime;
        float a = Mathf.Min(1, Mathf.Min(_t / FadeTime, (show - _t) / FadeTime));
        float slide = (1 - Mathf.Min(1, _t / FadeTime)) * -20;
        var r = new Rect2(new Vector2(Mathf.Round((Size.X - Width) / 2 + 20), 112 + slide), Width, Height);

        DrawRect(r, new Color(Palette.Border, a));
        DrawRect(r.Grow(-4), new Color(Palette.PanelLight, a));
        if (a < 0.98f) return; // Pixelkunst nur voll deckend zeichnen

        // Deckel mit leichtem Wackeln
        float wobble = Mathf.Sin(_t * 10) * Mathf.Max(0, 1 - _t) * 3;
        Sprites.Cap(this, r.Position + new Vector2(14, 13 + wobble), 4, motif, true);

        var x = r.Position.X + 78;
        DrawString(PfandTheme.Pixel, new Vector2(x, r.Position.Y + 26), "KRONKORKEN!", HorizontalAlignment.Left, -1, 8, Palette.GoodText);
        string name = motif.Name[0] + motif.Name.Substring(1).ToLowerInvariant();
        DrawString(PfandTheme.Body, new Vector2(x, r.Position.Y + 54), name, HorizontalAlignment.Left, -1, 24, Palette.Ink);
        if (c.isNew)
            DrawString(PfandTheme.Pixel, new Vector2(r.End.X - 60, r.Position.Y + 26), "NEU!", HorizontalAlignment.Left, -1, 8, Palette.BadText);
        else
            DrawString(PfandTheme.Body, new Vector2(r.End.X - 92, r.Position.Y + 54), "+1 KK", HorizontalAlignment.Left, -1, 22, Palette.Muted);
    }
}
