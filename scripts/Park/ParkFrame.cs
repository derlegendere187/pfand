using Godot;

namespace Pfand.Park;

/// <summary>
/// Hält den Park in fester Größe (16:9-Standard) und skaliert ihn passend in den freien Platz.
/// So ist das Spielfeld auf jedem Bildschirm gleich groß, nur die Darstellung wächst oder schrumpft.
/// </summary>
public partial class ParkFrame : Control
{
    /// <summary>Spielfeldgröße in Spielpixeln, entspricht dem freien Platz bei 16:9.</summary>
    public static readonly Vector2 World = new(870, 690);

    public readonly ParkView Park = new();

    public override void _Ready()
    {
        ClipContents = true;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        MouseFilter = MouseFilterEnum.Ignore;
        Park.Size = World;
        AddChild(Park);
        Resized += Fit;
        Fit();
    }

    void Fit()
    {
        if (Size.X < 1 || Size.Y < 1) return;
        float s = Mathf.Min(Size.X / World.X, Size.Y / World.Y);
        Park.Scale = new Vector2(s, s);
        Park.Position = ((Size - World * s) / 2).Floor();
        Park.Size = World;
        QueueRedraw();
    }

    public override void _Draw()
    {
        // Platzhalter für den Rand neben dem Park
        DrawRect(new Rect2(Vector2.Zero, Size), Palette.TuftDark);
    }
}
