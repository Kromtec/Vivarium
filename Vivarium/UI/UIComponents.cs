using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Vivarium.UI;

public static class UIComponents
{
    public static void DrawPanel(SpriteBatch sb, Rectangle rect, Texture2D pixel)
    {
        sb.Draw(pixel, new Rectangle(rect.X + UITheme.ShadowOffset, rect.Y + UITheme.ShadowOffset, rect.Width, rect.Height), UITheme.ShadowColor);
        sb.Draw(pixel, rect, UITheme.PanelBgColor);
        DrawBorder(sb, rect, UITheme.BorderThickness, UITheme.BorderColor, pixel);
    }

    public static void DrawBorder(SpriteBatch sb, Rectangle r, int thickness, Color c, Texture2D pixel)
    {
        sb.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, thickness), c); // Top
        sb.Draw(pixel, new Rectangle(r.X, r.Y + r.Height - thickness, r.Width, thickness), c); // Bottom
        sb.Draw(pixel, new Rectangle(r.X, r.Y, thickness, r.Height), c); // Left
        sb.Draw(pixel, new Rectangle(r.X + r.Width - thickness, r.Y, thickness, r.Height), c); // Right
    }

    public static void DrawButton(SpriteBatch sb, SpriteFont font, Rectangle rect, string text, Texture2D pixel, bool isHovered, bool isPressed, Color? overrideColor = null)
    {
        Color bgColor = overrideColor ?? (isHovered ? UITheme.ButtonHoverColor : UITheme.ButtonColor);
        if (isPressed) bgColor = Color.Lerp(bgColor, Color.Black, 0.2f);

        sb.Draw(pixel, rect, bgColor);
        DrawBorder(sb, rect, 1, isHovered ? Color.White : UITheme.BorderColor, pixel);

        if (!string.IsNullOrEmpty(text))
        {
            Vector2 size = font.MeasureString(text);
            Vector2 pos = new Vector2(
                rect.X + (rect.Width - size.X) / 2,
                rect.Y + (rect.Height - size.Y) / 2 + 2 // +2 for vertical centering adjustment
            );
            sb.DrawString(font, text, pos, Color.White);
        }
    }

    public static void DrawProgressBar(SpriteBatch sb, SpriteFont font, Rectangle rect, string label, float value, float max, Color color, Texture2D pixel)
    {
        // Label
        if (!string.IsNullOrEmpty(label))
        {
            sb.DrawString(font, label, new Vector2(rect.X, rect.Y), UITheme.TextColorSecondary);
        }

        // Bar Area
        int barHeight = 10;
        int barY = rect.Y + 5; // Offset from label
        if (string.IsNullOrEmpty(label)) barY = rect.Y; // If no label, start at top

        // Background
        Rectangle barRect = new Rectangle(rect.X + (string.IsNullOrEmpty(label) ? 0 : 100), barY, rect.Width - (string.IsNullOrEmpty(label) ? 0 : 100), barHeight);
        // If label is present, we assume a fixed width for label area or passed rect includes it.
        // The Inspector uses a fixed layout: Label at X, Bar at Right-100.
        // Let's make this flexible. If label is provided, we draw it and push bar to right.
        // But for generic usage, maybe just draw the bar in the rect provided?
        // Let's stick to the Inspector style for now as it's the main user.

        // Actually, let's make it simple: Draw bar in the given rect. Label drawing is caller's responsibility or separate.
        // But the request is to refactor components.
        // Let's support the "Label ...... [Bar]" layout.
    }

    public static void DrawSimpleProgressBar(SpriteBatch sb, Rectangle rect, float ratio, Color color, Texture2D pixel)
    {
        // Bg
        sb.Draw(pixel, rect, Color.Black * 0.5f);

        // Fill
        float r = Math.Clamp(ratio, 0f, 1f);
        sb.Draw(pixel, new Rectangle(rect.X, rect.Y, (int)(rect.Width * r), rect.Height), color);
    }

    public static void DrawBrainBar(SpriteBatch sb, Rectangle rect, float value, bool positiveOnly, Texture2D pixel)
    {
        // Bg
        sb.Draw(pixel, rect, Color.Black * 0.5f);

        int centerX = rect.X + (rect.Width / 2);

        if (positiveOnly)
        {
            // 0 to 1
            float ratio = Math.Clamp(value, 0f, 1f);
            Color c = UITheme.GetColorForWeight(value);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, (int)(rect.Width * ratio), rect.Height), c);
        }
        else
        {
            // -1 to 1
            float valClamped = Math.Clamp(value, -1f, 1f);
            int fillWidth = (int)((rect.Width / 2) * Math.Abs(valClamped));

            Color c = UITheme.GetColorForWeight(value);

            if (valClamped > 0)
                sb.Draw(pixel, new Rectangle(centerX, rect.Y, fillWidth, rect.Height), c);
            else
                sb.Draw(pixel, new Rectangle(centerX - fillWidth, rect.Y, fillWidth, rect.Height), c);
        }
    }
}
