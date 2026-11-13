using Godot;

namespace Pfand.Park;

/// <summary>Farben aus dem Design (warmer Beige-Pixel-Look).</summary>
public static class Palette
{
    static Color H(string hex) => Color.FromHtml(hex);

    // UI
    public static readonly Color Bg = H("#e6d7b8");
    public static readonly Color Panel = H("#f4ead2");
    public static readonly Color PanelLight = H("#fffaf0");
    public static readonly Color PanelShade = H("#d9c7a3");
    public static readonly Color Ink = H("#2a1d14");
    public static readonly Color InkSoft = H("#4a3424");
    public static readonly Color Muted = H("#6b5a48");
    public static readonly Color Border = H("#4a3424");
    public static readonly Color MoneyText = H("#8a5a10");
    public static readonly Color GoodText = H("#2f6b32");
    public static readonly Color BadText = H("#a8382e");
    public static readonly Color Machine = H("#d6ccb8");
    public static readonly Color MachineDark = H("#b5a991");
    public static readonly Color Display = H("#2a1d14");
    public static readonly Color DisplayText = H("#a8e08f");
    public static readonly Color Leergut = H("#4f8a4a");
    public static readonly Color ButtonGreen = H("#8cc472");
    public static readonly Color ButtonGreenHover = H("#9dd283");
    public static readonly Color ButtonGreenDown = H("#67a052");
    public static readonly Color Paper = H("#fffaf0");
    public static readonly Color AlbumCover = H("#6b3f2a");
    public static readonly Color AlbumPage = H("#efe3c8");
    public static readonly Color Gold = H("#f2c75c");

    // Park
    public static readonly Color Grass = H("#5a8a44");
    public static readonly Color TuftDark = H("#4f7c3c");
    public static readonly Color TuftLight = H("#68984c");
    public static readonly Color GrassBright = H("#7aa858");
    public static readonly Color Path = H("#b4966a");
    public static readonly Color PathDark = H("#9c7e54");
    public static readonly Color PathLight = H("#c6aa7c");
    public static readonly Color PathPebble = H("#d6bc90");
    public static readonly Color PathEdge = H("#c9ad7a");
    public static readonly Color Wood = H("#b07a4a");
    public static readonly Color WoodDark = H("#7a5434");
    public static readonly Color BinBody = H("#4f7a5a");
    public static readonly Color BinInside = H("#2f4a38");
    public static readonly Color LampPost = H("#5a4a3a");
    public static readonly Color LampLight = H("#f2c75c");
    public static readonly Color Glow = H("#fff3c4");
    public static readonly Color Skin = H("#e8c09a");
    public static readonly Color Night = H("#1e1a3a");

    public static readonly Color[] Flowers = { H("#fffaf0"), H("#f2c75c"), H("#d9735a"), H("#b496e0") };
    public static readonly Color[] Shirts = { H("#d9735a"), H("#6fa8d6"), H("#f2c75c"), H("#b496e0"), H("#8cc472"), H("#fffaf0"), H("#e08a4a") };
    public static readonly Color[] Hair = { H("#5a3a26"), H("#e0c070"), H("#2a1d14"), H("#a8503c"), H("#c9c2b4") };
    public static readonly Color[] Blankets = { H("#d9735a"), H("#6fa8d6"), H("#b496e0"), H("#e08a4a"), H("#8cc472") };
    public static readonly Color[] Cans = { H("#d9735a"), H("#6fa8d6"), H("#8cc472"), H("#b496e0") };
}
