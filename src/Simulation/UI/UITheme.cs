using Microsoft.Xna.Framework;

namespace Vivarium.UI;

public static class UITheme
{
    // Colors (Modern Dark Palette)
    public static readonly Color PanelBgColor = new(0, 0, 0, 180); // Black semi-transparent
    public static readonly Color BorderColor = new(80, 80, 90);       // Subtle border
    public static readonly Color ShadowColor = Color.Black * 0.5f;

    public static readonly Color TextColorPrimary = new(230, 230, 240);
    public static readonly Color TextColorSecondary = new(160, 160, 170);
    public static readonly Color HeaderColor = new(100, 200, 255);    // Cyan accent

    public static readonly Color GoodColor = new(100, 255, 100);      // Soft Green
    public static readonly Color WarningColor = new(255, 200, 50);    // Soft Orange
    public static readonly Color BadColor = new(255, 80, 80);         // Soft Red
    public static readonly Color ButtonColor = new(30, 30, 35);       // Darker Grey Button
    public static readonly Color ButtonHoverColor = new(50, 50, 60);  // Lighter Grey Button
    public static readonly Color ScrollThumbColor = Color.Gray;

    public static readonly Color BarBackgroundColor = new(45, 45, 50); // Subtle background for bars

    public static readonly Color CooldownMoveColor = Color.LightBlue;
    public static readonly Color CooldownBreedColor = Color.Pink;

    // Genome / Weight Colors
    public static readonly Color WeightNeutral = new(60, 65, 75);

    public static readonly Color[] WeightPositive = [
        new Color(60, 140, 80),
        new Color(80, 180, 100),
        new Color(100, 220, 120),
        new Color(120, 255, 140)
    ];

    public static readonly Color[] WeightNegative = [
        new Color(140, 60, 60),
        new Color(180, 80, 80),
        new Color(220, 100, 100),
        new Color(255, 120, 120)
    ];

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
