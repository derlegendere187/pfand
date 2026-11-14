using System.Collections.Generic;
using Godot;
using Pfand.Core;
using Pfand.Park;

namespace Pfand.UI;

/// <summary>Park-Upgrades als Kachelraster: Bild, Name, Preis. Ein Klick kauft. Kein Scrollen.</summary>
public partial class UpgradePanel : PanelContainer
{
    readonly List<UpgradeTile> _tiles = new();

    public override void _Ready()
    {
        SizeFlagsVertical = SizeFlags.ExpandFill;
        AddThemeStyleboxOverride("panel", PfandTheme.WoodPanel(10));

        var grid = new GridContainer { Columns = 2, SizeFlagsVertical = SizeFlags.ShrinkBegin };
        grid.AddThemeConstantOverride("h_separation", 6);
        grid.AddThemeConstantOverride("v_separation", 6);
        AddChild(grid);

        foreach (var def in UpgradeDefs.All)
        {
            var tile = new UpgradeTile { Def = def };
            _tiles.Add(tile);
            grid.AddChild(tile);
        }

        GameState.I.Changed += Refresh;
        Refresh();
    }

    public override void _ExitTree() => GameState.I.Changed -= Refresh;

    /// <summary>Bildschirmfläche der Kachel eines Upgrades (für Kalles Hervorhebung).</summary>
    public Rect2? CardRect(string id)
    {
        foreach (var t in _tiles)
            if (t.Def.Id == id && t.IsVisibleInTree()) return t.GetGlobalRect();
        return null;
    }

    void Refresh()
    {
        var gs = GameState.I;
        foreach (var t in _tiles)
        {
            t.Visible = UpgradeDefs.Visible(gs, t.Def);
            t.QueueRedraw();
        }
    }
}

/// <summary>Eine Upgrade-Kachel. Die Beschreibung steht im Tooltip und im Skilltree.</summary>
public partial class UpgradeTile : Control
{
    public UpgradeDef Def;
    bool _hover, _down;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(0, 84);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        MouseEntered += () => { _hover = true; QueueRedraw(); };
        MouseExited += () => { _hover = false; _down = false; QueueRedraw(); };
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is not InputEventMouseButton mb || mb.ButtonIndex != MouseButton.Left) return;
        if (mb.Pressed) { _down = true; QueueRedraw(); }
        else if (_down)
        {
            _down = false;
            GameState.I.TryBuy(Def);
            QueueRedraw();
        }
        AcceptEvent();
    }

    static string Capital(string s) => s.Length == 0 ? s : s[0] + s.Substring(1).ToLowerInvariant();

    public override void _Draw()
    {
        var gs = GameState.I;
        const float p = PixelBox.P;
        int level = gs.Level(Def.Id);
        bool unlocked = gs.UpgradeUnlocked(Def);
        bool maxed = level >= gs.MaxLevel(Def);
        bool affordable = unlocked && !maxed && gs.CanBuy(Def);

        var r = new Rect2(Vector2.Zero, Size);
        DrawRect(r, Wood.SlotEdge);
        var inner = r.Grow(-p);
        DrawRect(inner, _hover && affordable ? Wood.Plank2 : Wood.Slot);
        DrawRect(new Rect2(inner.Position.X, inner.End.Y - p, inner.Size.X, p), Wood.SlotLow);

        // Bild
        var iconRect = new Rect2(Mathf.Round(Size.X / 2 - 21), 4, 42, 42);
        TooltipText = unlocked || Def.LockedText == "" ? Def.Effect : Def.Effect + "\n" + Capital(Def.LockedText);
        if (!unlocked) Modulate = new Color(1, 1, 1, 0.55f);
        else Modulate = Colors.White;
        UpgradeArt.Draw(this, Def.Id, iconRect);

        if (level > 0)
            DrawString(PfandTheme.Body, new Vector2(inner.End.X - 6, 22), $"×{level}", HorizontalAlignment.Right, -1, 22, Wood.Accent);

        // Name
        DrawString(PfandTheme.Body, new Vector2(0, 60), Def.Name, HorizontalAlignment.Center, Size.X, 20, Wood.Ink);

        // Preisleiste
        var bar = new Rect2(inner.Position.X + p, Size.Y - 23, inner.Size.X - 2 * p, 18);
        string label;
        Color fill, ink;
        if (!unlocked) { label = "gesperrt"; fill = Wood.SlotEdge; ink = Wood.OffInk; }
        else if (maxed) { label = "MAX"; fill = Wood.SlotEdge; ink = Wood.Muted; }
        else
        {
            label = GameState.Euro(Def.Price(level));
            fill = affordable ? (_down ? Wood.BtnDown : _hover ? Wood.BtnHover : Wood.Btn) : Wood.Off;
            ink = affordable ? Wood.BtnInk : Wood.OffInk;
        }
        DrawRect(bar, fill);
        if (affordable) DrawRect(new Rect2(bar.Position, bar.Size.X, 2), Wood.BtnHi);
        DrawString(PfandTheme.Body, new Vector2(bar.Position.X, bar.End.Y - 3), label, HorizontalAlignment.Center, bar.Size.X, 20, ink);
    }
}

/// <summary>Kleine Pixelbilder der Upgrades (14×14 Kunstpixel).</summary>
public static class UpgradeArt
{
    static Color H(string hex) => Color.FromHtml(hex);

    public static void Draw(CanvasItem ci, string id, Rect2 r)
    {
        float p = Mathf.Floor(r.Size.X / 14);
        var o = r.Position;
        void R(float x, float y, float w, float h, Color c) => ci.DrawRect(new Rect2(o.X + x * p, o.Y + y * p, w * p, h * p), c);
        var shadow = new Color(0, 0, 0, 0.3f);

        switch (id)
        {
            case "weg":
            {
                R(0, 3, 14, 8, H("#5f8f44"));
                R(1, 4, 12, 6, H("#c4a676"));
                R(1, 4, 12, 1, H("#a88a5e"));
                R(1, 9, 12, 1, H("#d8be8e"));
                R(3, 6, 1, 1, H("#e8d4a8")); R(8, 7, 1, 1, H("#a88a5e")); R(10, 5, 1, 1, H("#e8d4a8"));
                R(0, 3, 1, 1, H("#74a650")); R(13, 10, 1, 1, H("#4d7a3a"));
                break;
            }
            case "laterne":
            {
                // Lichtschein
                var glow = new Color(1f, 0.85f, 0.5f, 0.22f);
                R(3, 1, 8, 7, glow);
                R(2, 2, 10, 5, glow);
                // Mast, Fuß
                R(6, 7, 2, 6, H("#3a3a42"));
                R(6, 7, 1, 6, H("#5a5a66"));
                R(5, 12, 4, 1, H("#2a2a30"));
                R(4, 13, 6, 1, shadow);
                // Laternenkopf: Dach, Glas mit Sprossen, Boden
                R(6, 0, 2, 1, H("#2a2a30"));
                R(5, 1, 4, 1, H("#3a3a42"));
                R(4, 2, 6, 1, H("#2a2a30"));
                R(4, 3, 1, 3, H("#2a2a30")); R(9, 3, 1, 3, H("#2a2a30"));
                R(5, 3, 4, 3, H("#ffe2a0"));
                R(5, 3, 1, 1, H("#fff6d8"));
                R(7, 3, 1, 3, H("#e8b860"));
                R(4, 6, 6, 1, H("#2a2a30"));
                break;
            }
            case "tasche":
            {
                R(4, 2, 6, 1, H("#5a3a22")); R(3, 3, 1, 2, H("#5a3a22")); R(10, 3, 1, 2, H("#5a3a22"));
                R(2, 5, 10, 8, H("#8a5a32"));
                R(2, 5, 10, 1, H("#a87446"));
                R(2, 5, 10, 3, H("#9a6a3c"));
                R(6, 7, 2, 2, H("#e0b860"));
                R(2, 12, 10, 1, H("#5a3a22"));
                R(3, 13, 9, 1, shadow);
                break;
            }
            case "picknick":
            {
                R(2, 3, 11, 10, shadow);
                R(1, 2, 11, 10, H("#c8553d"));
                for (int y = 0; y < 5; y++)
                    for (int x = 0; x < 5; x++)
                        if ((x + y) % 2 == 0) R(1 + x * 2 + 0.5f, 2 + y * 2 + 0.5f, 1.5f, 1.5f, H("#f4ead2"));
                R(1, 2, 11, 1, H("#d9735a"));
                break;
            }
            case "muelleimer":
            {
                // Holzeimer wie im Park
                R(3, 13, 10, 1, shadow);
                R(2, 1, 10, 12, H("#2a1d14"));
                R(3, 2, 8, 3, H("#c8935a"));
                R(4, 3, 6, 1, H("#1e1e22"));
                R(3, 5, 8, 7, H("#a8743e"));
                R(5, 5, 1, 7, H("#4a3020")); R(8, 5, 1, 7, H("#4a3020"));
                R(3, 6, 8, 1, H("#4a4a54")); R(3, 10, 8, 1, H("#4a4a54"));
                R(5, 2, 1, 1, H("#8a5a2e")); R(5, 1, 1, 1, H("#c8553d"));
                break;
            }
            case "bank":
            {
                R(1, 12, 13, 1, shadow);
                R(1, 3, 12, 2, H("#8e6238")); R(1, 3, 12, 1, H("#ac7c48"));
                R(1, 6, 12, 2, H("#8e6238")); R(1, 6, 12, 1, H("#ac7c48"));
                R(1, 9, 12, 2, H("#8e6238")); R(1, 9, 12, 1, H("#ac7c48"));
                R(2, 5, 1, 7, H("#3a3a42")); R(11, 5, 1, 7, H("#3a3a42"));
                break;
            }
            case "gruppen":
                Sprites.Person(ci, o + new Vector2(14, 20), H("#c86a52"), H("#4a3020"), 2.5f);
                Sprites.Person(ci, o + new Vector2(28, 22), H("#5a86b0"), H("#d8b870"), 2.5f);
                break;
            case "sammler":
                Sprites.Collector(ci, o + new Vector2(21, 22), 1);
                break;
            case "tischtennis":
            {
                R(1, 11, 13, 1, shadow);
                R(1, 3, 12, 8, H("#2f6a4a"));
                R(1, 3, 12, 1, H("#3f8a5e"));
                R(6.5f, 3, 1, 8, H("#f4f0e4"));
                R(1, 3, 1, 8, H("#f4f0e4")); R(12, 3, 1, 8, H("#f4f0e4"));
                R(9, 6, 2, 2, H("#f4f0e4"));
                break;
            }
            case "imbiss":
            {
                R(1, 13, 13, 1, shadow);
                R(2, 6, 10, 7, H("#b07a4a")); R(2, 6, 10, 1, H("#c89060"));
                R(3, 8, 8, 3, H("#3a2a1c"));
                for (int x = 0; x < 6; x++) R(1 + x * 2, 2, 2, 3, x % 2 == 0 ? H("#c8553d") : H("#f4ead2"));
                R(1, 5, 12, 1, H("#8a3a2a"));
                break;
            }
            case "grill":
            {
                R(3, 13, 9, 1, shadow);
                R(3, 4, 8, 4, H("#2a2a30"));
                R(4, 8, 6, 1, H("#2a2a30"));
                R(3, 4, 8, 1, H("#4a4a54"));
                R(4, 5, 1, 1, H("#e0703a")); R(7, 5, 1, 1, H("#f0a050")); R(9, 6, 1, 1, H("#e0703a"));
                R(4, 9, 1, 4, H("#3a3a42")); R(9, 9, 1, 4, H("#3a3a42"));
                R(6, 1, 1, 2, new Color(0.85f, 0.85f, 0.85f, 0.5f)); R(8, 0, 1, 2, new Color(0.85f, 0.85f, 0.85f, 0.4f));
                break;
            }
            default:
                R(3, 3, 8, 8, H("#8a6a4a"));
                break;
        }
    }
}
