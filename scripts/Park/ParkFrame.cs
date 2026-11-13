using Godot;
using Pfand.UI;

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
    const float Border = 3;

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
        // Platz für den Rahmen lassen
        var room = Size - new Vector2(2 * Border, 2 * Border);
        float s = Mathf.Min(room.X / World.X, room.Y / World.Y);
        Park.Scale = new Vector2(s, s);
        Park.Position = ((Size - World * s) / 2).Floor();
        Park.Size = World;
        QueueRedraw();
    }

    public override void _Draw()
    {
        // Daneben bleibt der Holzhintergrund sichtbar, nur der Park bekommt einen Rahmen
        var r = new Rect2(Park.Position, World * Park.Scale.X).Grow(Border);
        DrawRect(r, Wood.Edge);
    }
}
