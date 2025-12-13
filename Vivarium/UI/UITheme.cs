using Microsoft.Xna.Framework;

namespace Vivarium.UI;

public static class UITheme
{
    // Colors (Modern Dark Palette)
    public static readonly Color PanelBgColor = new Color(25, 25, 30, 245); // Very dark blue-grey, high opacity
    public static readonly Color BorderColor = new Color(60, 65, 75);       // Subtle border
    public static readonly Color ShadowColor = Color.Black * 0.5f;
    
    public static readonly Color TextColorPrimary = new Color(230, 230, 240);
    public static readonly Color TextColorSecondary = new Color(160, 160, 170);
    public static readonly Color HeaderColor = new Color(100, 200, 255);    // Cyan accent
    
    public static readonly Color GoodColor = new Color(100, 255, 100);      // Soft Green
    public static readonly Color WarningColor = new Color(255, 200, 50);    // Soft Orange
    public static readonly Color BadColor = new Color(255, 80, 80);         // Soft Red

    // Layout
    public const int Padding = 15;
    public const int LineHeight = 24;
    public const int BorderThickness = 1;
    public const int ShadowOffset = 4;
}
