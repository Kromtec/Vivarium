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
    public static readonly Color ButtonColor = new Color(50, 50, 60);       // Dark Grey Button

    // Genome / Weight Colors (GitHub Style)
    public static readonly Color WeightNeutral = new Color(22, 27, 34);
    
    public static readonly Color[] WeightPositive = new Color[] {
        new Color(14, 68, 41),
        new Color(0, 109, 50),
        new Color(38, 166, 65),
        new Color(57, 211, 83)
    };

    public static readonly Color[] WeightNegative = new Color[] {
        new Color(68, 14, 14),
        new Color(109, 0, 0),
        new Color(166, 38, 38),
        new Color(211, 57, 57)
    };

    // Helper to get color from weight
    public static Color GetColorForWeight(float weight)
    {
        if (System.Math.Abs(weight) < 0.2f) return WeightNeutral;
        
        if (weight > 0)
        {
            if (weight < 1.0f) return WeightPositive[0];
            if (weight < 2.0f) return WeightPositive[1];
            if (weight < 3.0f) return WeightPositive[2];
            return WeightPositive[3];
        }
        else
        {
            float absW = System.Math.Abs(weight);
            if (absW < 1.0f) return WeightNegative[0];
            if (absW < 2.0f) return WeightNegative[1];
            if (absW < 3.0f) return WeightNegative[2];
            return WeightNegative[3];
        }
    }

    // Layout
    public const int Padding = 15;
    public const int LineHeight = 24;
    public const int BorderThickness = 1;
    public const int ShadowOffset = 4;
}
