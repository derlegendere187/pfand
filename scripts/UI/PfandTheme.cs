using Godot;
using Pfand.Park;

namespace Pfand.UI;

/// <summary>Das UI-Theme (Holz-Panels, Pixel-Kästen, eine Pixel-Schrift) plus kleine Bau-Helfer.</summary>
public static class PfandTheme
{
    public static FontFile Body { get; private set; }    // VT323 – die eine Schrift für alles
    public static FontFile Pixel => Body;

    public static Theme Build()
    {
        Body = LoadFont("res://assets/fonts/VT323-Regular.ttf");

        var t = new Theme { DefaultFont = Body, DefaultFontSize = 22 };

        t.SetStylebox("panel", "PanelContainer", Box(Palette.Panel, Palette.Border));
        t.SetColor("font_color", "Label", Palette.Ink);

        t.SetStylebox("normal", "Button", ButtonBox(Wood.Btn, Wood.BtnHi, Wood.BtnLo));
        t.SetStylebox("hover", "Button", ButtonBox(Wood.BtnHover, Wood.BtnHi, Wood.BtnLo));
        t.SetStylebox("pressed", "Button", ButtonBox(Wood.BtnDown, Wood.BtnLo, Wood.BtnDown));
        t.SetStylebox("disabled", "Button", ButtonBox(Wood.Off, Wood.OffHi, Wood.Off));
        t.SetStylebox("focus", "Button", new StyleBoxEmpty());
        t.SetColor("font_color", "Button", Wood.BtnInk);
        t.SetColor("font_hover_color", "Button", Wood.BtnInk);
        t.SetColor("font_pressed_color", "Button", Wood.BtnInk);
        t.SetColor("font_focus_color", "Button", Wood.BtnInk);
        t.SetColor("font_hover_pressed_color", "Button", Wood.BtnInk);
        t.SetColor("font_disabled_color", "Button", Wood.OffInk);
        t.SetFont("font", "Button", Body);
        t.SetFontSize("font_size", "Button", 24);

        t.SetStylebox("panel", "ScrollContainer", new StyleBoxEmpty());
        t.SetStylebox("panel", "TooltipPanel", new PixelBox(Wood.Paper, Wood.Edge, null, Wood.PaperShade, 8));
        t.SetColor("font_color", "TooltipLabel", Wood.Print);
        t.SetFontSize("font_size", "TooltipLabel", 22);
        return t;
    }

    static FontFile LoadFont(string path)
    {
        var f = GD.Load<FontFile>(path);
        if (f == null) return null;
        f.Antialiasing = TextServer.FontAntialiasing.None;
        f.Hinting = TextServer.Hinting.None;
        f.SubpixelPositioning = TextServer.SubpixelPositioning.Disabled;
        return f;
    }

    /// <summary>Pixel-Kasten mit Rand und abgeschnittenen Ecken.</summary>
    public static StyleBox Box(Color bg, Color border, int borderWidth = 4, int margin = 8)
        => new PixelBox(bg, border, bg.Lightened(0.12f), bg.Darkened(0.12f), margin);

    /// <summary>Knopf: Rand, helle Oberkante, zwei Pixel dunkle Unterkante.</summary>
    public static StyleBox ButtonBox(Color fill, Color hi, Color lo, int margin = 6)
        => new PixelBox(fill, Wood.Edge, hi, lo, margin) { Bevel = 2 };

    public static StyleBox WoodPanel(float margin = 12, bool nails = true) => new WoodBox(margin, nails);

    /// <summary>Label mit Body-Schrift (VT323).</summary>
    public static Label Text(string text, int size = 22, Color? color = null)
    {
        var l = new Label { Text = text };
        l.AddThemeFontOverride("font", Body);
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", color ?? Palette.Ink);
        return l;
    }

    /// <summary>Überschrift in derselben Schrift, nur größer (alte Größen 8/16/24 werden umgerechnet).</summary>
    public static Label Title(string text, int size = 8, Color? color = null)
    {
        var l = new Label { Text = text };
        l.AddThemeFontOverride("font", Body);
        l.AddThemeFontSizeOverride("font_size", TitleSize(size));
        l.AddThemeColorOverride("font_color", color ?? Palette.Ink);
        return l;
    }

    public static int TitleSize(int old) => old <= 8 ? 22 : old <= 16 ? 32 : 40;

    public static PanelContainer Panel(Color bg, Color? border = null, int margin = 8)
    {
        var p = new PanelContainer();
        p.AddThemeStyleboxOverride("panel", Box(bg, border ?? Palette.Border, 4, margin));
        return p;
    }

    public static Control Spacer()
        => new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
}
