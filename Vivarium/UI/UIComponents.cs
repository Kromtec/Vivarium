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
        Vector2 pos = new(rect.X + ((rect.Width - size.X) / 2), rect.Y + ((rect.Height - size.Y) / 2) + 3);
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
        Vector2 pos = new(rect.X + 5, rect.Y + ((rect.Height - size.Y) / 2));

        // Clip text if needed (simple scissor or just draw)
        // For now just draw
        sb.DrawString(font, currentText, pos, UITheme.TextColorPrimary);

        // Arrow
        string arrow = isOpen ? "^" : "v";
        Vector2 arrowSize = font.MeasureString(arrow);
        sb.DrawString(font, arrow, new Vector2(rect.Right - arrowSize.X - 5, rect.Y + ((rect.Height - arrowSize.Y) / 2)), UITheme.TextColorSecondary);
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
            Vector2 pos = new(itemRect.X + 5, itemRect.Y + ((itemRect.Height - size.Y) / 2));
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
        Rectangle barRect = new(rect.X, rect.Y + rect.Height - barHeight, rect.Width, barHeight);

        // Background
        sb.Draw(pixel, barRect, Color.Black * 0.5f);

        // Fill
        float t = Math.Clamp(value / max, 0f, 1f);
        Rectangle fillRect = new(barRect.X, barRect.Y, (int)(barRect.Width * t), barRect.Height);
        sb.Draw(pixel, fillRect, color);

        // Border
        DrawBorder(sb, barRect, 1, UITheme.BorderColor, pixel);
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

    public static bool DrawSlider(SpriteBatch sb, SpriteFont font, Rectangle rect, string label, ref float value, float min, float max, Texture2D pixel, string format = "0.00")
    {
        // Label
        if (!string.IsNullOrEmpty(label))
        {
            sb.DrawString(font, label, new Vector2(rect.X, rect.Y), UITheme.TextColorSecondary);
        }

        // Slider Area
        int sliderY = rect.Y + 20;
        Rectangle sliderRect = new(rect.X, sliderY, rect.Width, 20);

        // Track
        Rectangle trackRect = new(sliderRect.X, sliderRect.Y + 8, sliderRect.Width, 4);
        sb.Draw(pixel, trackRect, Color.Gray);

        // Thumb
        float t = (value - min) / (max - min);
        int thumbX = (int)(trackRect.X + (t * trackRect.Width));
        Rectangle thumbRect = new(thumbX - 5, sliderRect.Y, 10, 20);

        var mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();
        bool isHovered = thumbRect.Contains(mouse.Position) || trackRect.Contains(mouse.Position);
        bool isPressed = mouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;

        bool changed = false;

        // Simple interaction: if mouse is pressed within the slider area (expanded), update value
        // This doesn't support "drag outside" but is simple for now.
        if (isPressed && sliderRect.Contains(mouse.Position))
        {
            float mouseT = (mouse.Position.X - trackRect.X) / (float)trackRect.Width;
            mouseT = Math.Clamp(mouseT, 0f, 1f);
            float newValue = min + (mouseT * (max - min));

            if (Math.Abs(newValue - value) > 0.0001f)
            {
                value = newValue;
                changed = true;
            }
        }

        // Draw Thumb
        sb.Draw(pixel, thumbRect, isHovered ? Color.White : UITheme.ButtonColor);
        DrawBorder(sb, thumbRect, 1, UITheme.BorderColor, pixel);

        // Value Text
        string valueText = value.ToString(format);
        Vector2 valueSize = font.MeasureString(valueText);
        sb.DrawString(font, valueText, new Vector2(rect.Right - valueSize.X, rect.Y), UITheme.TextColorPrimary);

        return changed;
    }

    public static bool DrawSlider(SpriteBatch sb, SpriteFont font, Rectangle rect, string label, ref int value, int min, int max, Texture2D pixel)
    {
        float fValue = value;
        bool changed = DrawSlider(sb, font, rect, label, ref fValue, min, max, pixel, "0");
        if (changed)
        {
            value = (int)Math.Round(fValue);
        }
        return changed;
    }

    public static bool DrawSlider(SpriteBatch sb, SpriteFont font, Rectangle rect, string label, ref double value, double min, double max, Texture2D pixel, string format = "0.00")
    {
        float fValue = (float)value;
        bool changed = DrawSlider(sb, font, rect, label, ref fValue, (float)min, (float)max, pixel, format);
        if (changed)
        {
            value = fValue;
        }
        return changed;
    }
}
