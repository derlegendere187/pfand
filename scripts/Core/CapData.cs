using Godot;

namespace Pfand.Core;

/// <summary>
/// Die Deckel-Motive (Set „Tiere“). Jedes Motiv ist Pixelkunst auf einem 12×12-Raster.
/// </summary>
public static class CapData
{
    public record struct Px(int X, int Y, int W, int H, string Col);

    public record Motif(string Name, string Face, Px[] Pixels);

    public static readonly Motif[] All =
    {
        new("FUCHS", "#2f6fae", new Px[]
        {
            new(3,2,1,1,"#e07a2e"), new(8,2,1,1,"#e07a2e"), new(3,3,6,3,"#e07a2e"),
            new(4,6,4,2,"#f4ecd8"), new(4,4,1,1,"#0b0c18"), new(7,4,1,1,"#0b0c18"), new(5,7,2,1,"#0b0c18"),
        }),
        new("IGEL", "#2f6fae", new Px[]
        {
            new(3,2,1,1,"#6b4a2e"), new(5,2,1,1,"#6b4a2e"), new(7,2,1,1,"#6b4a2e"), new(3,3,5,4,"#6b4a2e"),
            new(7,4,2,3,"#d9b38c"), new(4,7,1,1,"#d9b38c"), new(6,7,1,1,"#d9b38c"),
            new(7,4,1,1,"#0b0c18"), new(9,5,1,1,"#0b0c18"),
        }),
        new("TAUBE", "#f4ecd8", new Px[]
        {
            new(3,4,5,3,"#9aa0b4"), new(7,3,2,2,"#9aa0b4"), new(4,5,3,1,"#5c6278"), new(2,5,1,2,"#5c6278"),
            new(8,3,1,1,"#0b0c18"), new(9,4,1,1,"#c99a1e"), new(5,7,1,1,"#c99a1e"), new(6,7,1,1,"#c99a1e"),
        }),
        new("WASCHBÄR", "#2f6fae", new Px[]
        {
            new(3,2,1,1,"#9aa0b4"), new(8,2,1,1,"#9aa0b4"), new(3,3,6,4,"#9aa0b4"), new(3,4,6,1,"#0b0c18"),
            new(4,4,1,1,"#f4ecd8"), new(7,4,1,1,"#f4ecd8"), new(5,5,2,1,"#f4ecd8"), new(5,6,2,1,"#0b0c18"),
        }),
        new("ENTE", "#6fa8d6", new Px[]
        {
            new(3,5,5,3,"#f2c75c"), new(6,2,3,3,"#f2c75c"), new(4,6,2,1,"#c99a1e"),
            new(7,3,1,1,"#0b0c18"), new(9,3,1,1,"#e08a4a"),
        }),
        new("EICHHÖRNCHEN", "#4f8a4a", new Px[]
        {
            new(2,3,2,5,"#b5651d"), new(3,2,2,1,"#b5651d"), new(5,5,3,3,"#c4561a"), new(7,3,2,2,"#c4561a"),
            new(8,2,1,1,"#c4561a"), new(6,6,1,1,"#f4ecd8"), new(8,3,1,1,"#0b0c18"),
        }),
    };

    public static int Count => All.Length;
}
