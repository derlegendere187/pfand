using System;
using System.Collections.Generic;
using Godot;

namespace Pfand.Park;

/// <summary>
/// Zeichnet oft wiederholte Pixel-Figuren einmal in eine Textur (in Bildschirmauflösung) und gibt sie danach
/// nur noch als ein Bild aus. Spart pro Figur dutzende Einzel-Rechtecke. Bis die Textur fertig ist (ein Frame),
/// zeichnet der Aufrufer wie bisher direkt.
/// </summary>
public partial class SpriteBaker : Node
{
    public static SpriteBaker I;
    /// <summary>Wie groß ein Park-Pixel gerade auf dem Bildschirm ist (setzt ParkView jedes Frame).</summary>
    public static float ScreenScale = 1;

    readonly Dictionary<(string, uint, uint, int, int), Texture2D> _done = new();
    readonly HashSet<(string, uint, uint, int, int)> _pending = new();

    public override void _EnterTree() => I = this;
    public override void _ExitTree() { if (I == this) I = null; }

    /// <summary>Fertige Textur oder null (dann wird sie gebaut). size in Park-Pixeln, draw zeichnet ab (0, 0).</summary>
    public static Texture2D Get(string kind, uint a, uint b, int c, Vector2 size, Action<CanvasItem> draw)
    {
        if (I == null) return null;
        var key = (kind, a, b, c, Mathf.RoundToInt(ScreenScale * 100));
        if (I._done.TryGetValue(key, out var t)) return t;
        if (I._pending.Add(key))
        {
            if (I._queue.Count == 0) I.CallDeferred(MethodName.Start);
            I._queue.Add((key, size, ScreenScale, draw));
        }
        return null;
    }

    /// <summary>Alle fertigen Bilder einer Sorte wegwerfen (z. B. die alte feste Park-Ebene nach einem Ausbau).</summary>
    public static void Drop(string kind)
    {
        if (I == null) return;
        var old = new List<(string, uint, uint, int, int)>();
        foreach (var k in I._done.Keys) if (k.Item1 == kind) old.Add(k);
        foreach (var k in old) I._done.Remove(k);
    }

    readonly List<((string, uint, uint, int, int) key, Vector2 size, float scale, Action<CanvasItem> draw)> _queue = new();

    void Start()
    {
        foreach (var job in _queue) Bake(job.key, job.size, job.scale, job.draw);
        _queue.Clear();
    }

    async void Bake((string, uint, uint, int, int) key, Vector2 size, float scale, Action<CanvasItem> draw)
    {
        var px = new Vector2I(Mathf.CeilToInt(size.X * scale), Mathf.CeilToInt(size.Y * scale));
        var vp = new SubViewport
        {
            Size = px, TransparentBg = true, Disable3D = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Once,
            CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest,
        };
        vp.AddChild(new BakeCanvas { Paint = draw, Scale = Vector2.One * scale });
        AddChild(vp);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var img = vp.GetTexture().GetImage();
        _done[key] = ImageTexture.CreateFromImage(img);
        _pending.Remove(key);
        vp.QueueFree();
    }

    partial class BakeCanvas : Node2D
    {
        public Action<CanvasItem> Paint;
        public override void _Draw() => Paint(this);
    }
}
