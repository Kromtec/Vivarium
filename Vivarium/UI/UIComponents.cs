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
        Color baseColor = overrideColor ?? UITheme.ButtonColor;
        Color color = isHovered ? (isPressed ? Color.Lerp(baseColor, Color.Black, 0.2f) : Color.Lerp(baseColor, Color.White, 0.2f)) : baseColor;

        // Shadow
        sb.Draw(pixel, new Rectangle(rect.X + 2, rect.Y + 2, rect.Width, rect.Height), Color.Black * 0.5f);
        // Body
        sb.Draw(pixel, rect, color);
        // Border
        DrawBorder(sb, rect, 1, UITheme.BorderColor, pixel);

        // Text
        Vector2 size = font.MeasureString(text);
        Vector2 pos = new(rect.X + (rect.Width - size.X) / 2, rect.Y + (rect.Height - size.Y) / 2);
        sb.DrawString(font, text, pos, UITheme.TextColorPrimary);
    }

    public static void DrawDropdown(SpriteBatch sb, SpriteFont font, Rectangle rect, string currentText, bool isOpen, Texture2D pixel, bool isHovered)
    {
        Color baseColor = UITheme.PanelBgColor;
        Color color = isHovered ? Color.Lerp(baseColor, Color.White, 0.1f) : baseColor;

        // Body
        sb.Draw(pixel, rect, color);
        // Border
        DrawBorder(sb, rect, 1, UITheme.BorderColor, pixel);

        // Text
        // Truncate if too long?
        Vector2 size = font.MeasureString(currentText);
        // Left align with padding
        Vector2 pos = new(rect.X + 5, rect.Y + (rect.Height - size.Y) / 2);

        // Clip text if needed (simple scissor or just draw)
        // For now just draw
        sb.DrawString(font, currentText, pos, UITheme.TextColorPrimary);

        // Arrow
        string arrow = isOpen ? "^" : "v";
        Vector2 arrowSize = font.MeasureString(arrow);
        sb.DrawString(font, arrow, new Vector2(rect.Right - arrowSize.X - 5, rect.Y + (rect.Height - arrowSize.Y) / 2), UITheme.TextColorSecondary);
    }

    public static void DrawDropdownList(SpriteBatch sb, SpriteFont font, Rectangle rect, string[] items, int hoveredIndex, Texture2D pixel)
    {
        // Background
        sb.Draw(pixel, rect, UITheme.PanelBgColor);
        DrawBorder(sb, rect, 1, UITheme.BorderColor, pixel);

        int itemHeight = rect.Height / (items.Length > 0 ? items.Length : 1);

        for (int i = 0; i < items.Length; i++)
        {
            Rectangle itemRect = new(rect.X, rect.Y + (i * itemHeight), rect.Width, itemHeight);

            if (i == hoveredIndex)
            {
                sb.Draw(pixel, itemRect, UITheme.ButtonColor * 0.5f);
            }

            Vector2 size = font.MeasureString(items[i]);
            Vector2 pos = new(itemRect.X + 5, itemRect.Y + (itemRect.Height - size.Y) / 2);
            sb.DrawString(font, items[i], pos, UITheme.TextColorPrimary);
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
        const int barHeight = 10;
        int barY = rect.Y + 5; // Offset from label
        if (string.IsNullOrEmpty(label)) barY = rect.Y; // If no label, start at top

        // Background
        Rectangle barRect = new(rect.X + (string.IsNullOrEmpty(label) ? 0 : 100), barY, rect.Width - (string.IsNullOrEmpty(label) ? 0 : 100), barHeight);
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
        sb.Draw(pixel, rect, UITheme.BarBackgroundColor);

        // Fill
        float r = Math.Clamp(ratio, 0f, 1f);
        sb.Draw(pixel, new Rectangle(rect.X, rect.Y, (int)(rect.Width * r), rect.Height), color);
    }

    public static void DrawBrainBar(SpriteBatch sb, Rectangle rect, float value, bool positiveOnly, Texture2D pixel)
    {
        // Bg
        sb.Draw(pixel, rect, UITheme.BarBackgroundColor);

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
