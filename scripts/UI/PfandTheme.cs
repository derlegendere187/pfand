using Godot;
using Pfand.Park;

namespace Pfand.UI;

/// <summary>Das UI-Theme (beige Panels, braune 4px-Ränder, Pixel-Schriften) plus kleine Bau-Helfer.</summary>
public static class PfandTheme
{
    public static FontFile Pixel { get; private set; }   // Press Start 2P – Überschriften
    public static FontFile Body { get; private set; }    // VT323 – Fließtext, Zahlen

    public static Theme Build()
    {
        Pixel = LoadFont("res://assets/fonts/PressStart2P-Regular.ttf");
        Body = LoadFont("res://assets/fonts/VT323-Regular.ttf");

        var t = new Theme { DefaultFont = Body, DefaultFontSize = 22 };

        t.SetStylebox("panel", "PanelContainer", Box(Palette.Panel, Palette.Border));
        t.SetColor("font_color", "Label", Palette.Ink);

        t.SetStylebox("normal", "Button", Box(Palette.ButtonGreen, Palette.Border, 4, 6));
        t.SetStylebox("hover", "Button", Box(Palette.ButtonGreenHover, Palette.Border, 4, 6));
        t.SetStylebox("pressed", "Button", Box(Palette.ButtonGreenDown, Palette.Border, 4, 6));
        t.SetStylebox("disabled", "Button", Box(Palette.PanelShade, Palette.MachineDark, 4, 6));
        t.SetStylebox("focus", "Button", new StyleBoxEmpty());
        t.SetColor("font_color", "Button", Palette.Ink);
        t.SetColor("font_hover_color", "Button", Palette.Ink);
        t.SetColor("font_pressed_color", "Button", Palette.Ink);
        t.SetColor("font_focus_color", "Button", Palette.Ink);
        t.SetColor("font_disabled_color", "Button", Palette.Muted);
        t.SetFont("font", "Button", Body);
        t.SetFontSize("font_size", "Button", 24);

        t.SetStylebox("panel", "ScrollContainer", new StyleBoxEmpty());
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

    public static StyleBoxFlat Box(Color bg, Color border, int borderWidth = 4, int margin = 8)
    {
        var sb = new StyleBoxFlat { BgColor = bg, BorderColor = border };
        sb.SetBorderWidthAll(borderWidth);
        sb.SetContentMarginAll(margin);
        return sb;
    }

    /// <summary>Label mit Body-Schrift (VT323).</summary>
    public static Label Text(string text, int size = 22, Color? color = null)
    {
        var l = new Label { Text = text };
        l.AddThemeFontOverride("font", Body);
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", color ?? Palette.Ink);
        return l;
    }

    /// <summary>Label mit Pixel-Schrift (Press Start 2P). Am schärfsten bei 8, 16, 24.</summary>
    public static Label Title(string text, int size = 8, Color? color = null)
    {
        var l = new Label { Text = text };
        l.AddThemeFontOverride("font", Pixel);
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", color ?? Palette.Ink);
        return l;
    }

    public static PanelContainer Panel(Color bg, Color? border = null, int margin = 8)
    {
        var p = new PanelContainer();
        p.AddThemeStyleboxOverride("panel", Box(bg, border ?? Palette.Border, 4, margin));
        return p;
    }

    public static Control Spacer()
        => new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
}
