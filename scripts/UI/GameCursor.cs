using Godot;
using Pfand.Park;

namespace Pfand.UI;

/// <summary>
/// Eigener Mauszeiger im Pixelstil statt des Windows-Zeigers: eine Hand, die beim Drücken mit dem Finger tippt
/// und so bleibt, solange gedrückt bzw. gezogen wird. Über Klickbarem bleibt die Hand gleich (die Knöpfe werden dunkler).
/// Die Controls wählen die Form wie bisher über MouseDefaultCursorShape (Arrow / PointingHand / Drag).
/// </summary>
public partial class GameCursor : Node
{
    // o = Rand, f = Füllung (rechts und unten am Rand automatisch schattiert)
    static readonly string[] Hand =
    {
        "......oo........",
        ".....offo.......",
        ".....offo.......",
        ".....offo.......",
        ".....offooo.....",
        ".....offoffooo..",
        "..oo.offoffoffo.",
        ".offooffffffffo.",
        ".offofffffffffo.",
        "..offfffffffffo.",
        "..offffffffffo..",
        "...offfffffffo..",
        "...offfffffffo..",
        "....offffffo....",
        "....oooooooo....",
    };

    // Beim Tippen: derselbe Umriss, nur der Zeigefinger ist zwei Pixel eingeknickt
    static readonly string[] Tap =
    {
        "......oo........",
        ".....offo.......",
        ".....offooo.....",
        ".....offoffooo..",
        "..oo.offoffoffo.",
        ".offooffffffffo.",
        ".offofffffffffo.",
        "..offfffffffffo.",
        "..offffffffffo..",
        "...offfffffffo..",
        "...offfffffffo..",
        "....offffffo....",
        "....oooooooo....",
    };

    // Alle Bilder haben dieselbe Fläche, damit der Zeiger beim Wechsel nicht springt
    const int W = 20, H = 19;
    static readonly Vector2I HandOffset = new(3, 3), TapOffset = new(3, 5);
    static readonly Vector2 HandHotspot = new(10, 3);

    static GameCursor _instance;
    int _scale;
    bool _down;
    ImageTexture _hand, _tap;

    /// <summary>Zeiger setzen; skaliert bei Größenänderung des Fensters neu und tippt beim Drücken.</summary>
    public static void Install(Window root)
    {
        if (_instance != null) return;
        _instance = new GameCursor();
        root.CallDeferred(Node.MethodName.AddChild, _instance);
        _instance.Rebuild(root);
        root.SizeChanged += () => _instance?.Rebuild(root);
    }

    /// <summary>Beim Beenden die Zeiger-Bilder wieder freigeben (sonst meldet Godot Texturen-Lecks).</summary>
    public static void Uninstall()
    {
        foreach (var shape in new[] { Input.CursorShape.Arrow, Input.CursorShape.PointingHand, Input.CursorShape.Drag })
            Input.SetCustomMouseCursor(null, shape);
        if (_instance != null) { _instance._hand = _instance._tap = null; _instance._scale = 0; }
    }

    public override void _ExitTree() { if (_instance == this) _instance = null; }

    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left } mb && mb.Pressed != _down)
        {
            _down = mb.Pressed;
            SetHands();
        }
    }

    void Rebuild(Window root)
    {
        // Ein Zeiger-Pixel: 2 bei Full HD, 3 bei 1440p, 4 bei 4K (Windows skaliert eigene Zeiger nicht mit)
        int scale = Mathf.Clamp(Mathf.RoundToInt(root.Size.Y / 540f), 2, 6);
        if (scale == _scale) return;
        _scale = scale;
        _hand = Bake(Hand, HandOffset, scale);
        _tap = Bake(Tap, TapOffset, scale);
        // Ziehen (Skilltree) geht nur mit gedrückter Taste: dafür immer die tippende Hand
        Input.SetCustomMouseCursor(_tap, Input.CursorShape.Drag, HandHotspot * scale);
        SetHands();
    }

    void SetHands()
    {
        if (_hand == null) return;
        var tex = _down ? _tap : _hand;
        Input.SetCustomMouseCursor(tex, Input.CursorShape.Arrow, HandHotspot * _scale);
        Input.SetCustomMouseCursor(tex, Input.CursorShape.PointingHand, HandHotspot * _scale);
    }

    static ImageTexture Bake(string[] rows, Vector2I off, int s)
    {
        var g = new char[H, W];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int ry = y - off.Y, rx = x - off.X;
                g[y, x] = ry >= 0 && ry < rows.Length && rx >= 0 && rx < rows[ry].Length ? rows[ry][rx] : '.';
            }

        var img = Image.CreateEmpty(W * s, H * s, false, Image.Format.Rgba8);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                char c = g[y, x];
                if (c == '.') continue;
                bool shade = c == 'f' && (x + 1 < W && g[y, x + 1] == 'o' || y + 1 < H && g[y + 1, x] == 'o');
                var col = c == 'o' ? Palette.Ink : shade ? Palette.PanelShade : Palette.Paper;
                img.FillRect(new Rect2I(x * s, y * s, s, s), col);
            }
        return ImageTexture.CreateFromImage(img);
    }
}
