using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Vivarium.Config;

namespace Vivarium.UI;

public class SettingsWindow
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteFont _font;
    private readonly Texture2D _pixelTexture;
    private readonly SimulationConfig _config;

    public bool IsVisible { get; set; } = false;

    private Rectangle _windowRect;
    private int _currentTab = 0;
    private readonly string[] _tabs = ["Agent", "Plant", "Brain", "Genetics"];
    
    private int _scrollOffset = 0;

    public SettingsWindow(GraphicsDevice graphicsDevice, SpriteFont font, SimulationConfig config)
    {
        _graphicsDevice = graphicsDevice;
        _font = font;
        _config = config;
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData([Color.White]);
    }

    public void HandleInput(MouseState mouseState, int scrollDelta)
    {
        if (!IsVisible) return;

        if (scrollDelta != 0 && _windowRect.Contains(mouseState.Position))
        {
            _scrollOffset -= scrollDelta / 2; // Adjust sensitivity
            if (_scrollOffset < 0) _scrollOffset = 0;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible) return;

        int screenW = _graphicsDevice.Viewport.Width;
        int screenH = _graphicsDevice.Viewport.Height;

        int width = 600;
        int height = 700;
        _windowRect = new Rectangle((screenW - width) / 2, (screenH - height) / 2, width, height);

        // Draw Window Background
        UIComponents.DrawPanel(spriteBatch, _windowRect, _pixelTexture);

        // Draw Header
        int cursorY = _windowRect.Y + UITheme.Padding;
        spriteBatch.DrawString(_font, "SIMULATION SETTINGS", new Vector2(_windowRect.X + UITheme.Padding, cursorY), UITheme.HeaderColor);
        
        // Close Button (X)
        Rectangle closeRect = new(_windowRect.Right - 30, _windowRect.Y + 10, 20, 20);
        var mouse = Mouse.GetState();
        bool closeHover = closeRect.Contains(mouse.Position);
        bool closeClick = closeHover && mouse.LeftButton == ButtonState.Pressed;
        UIComponents.DrawButton(spriteBatch, _font, closeRect, "X", _pixelTexture, closeHover, closeClick, Color.Red);
        if (closeClick) IsVisible = false;

        cursorY += 40;

        // Draw Tabs
        int tabWidth = (_windowRect.Width - (UITheme.Padding * 2)) / _tabs.Length;
        for (int i = 0; i < _tabs.Length; i++)
        {
            Rectangle tabRect = new(_windowRect.X + UITheme.Padding + (i * tabWidth), cursorY, tabWidth - 5, 30);
            bool isSelected = i == _currentTab;
            bool isHovered = tabRect.Contains(mouse.Position);
            bool isClicked = isHovered && mouse.LeftButton == ButtonState.Pressed;

            if (isClicked) 
            {
                _currentTab = i;
                _scrollOffset = 0; // Reset scroll on tab change
            }

            Color tabColor = isSelected ? UITheme.ButtonColor : (isHovered ? Color.Gray : Color.DarkGray);
            UIComponents.DrawButton(spriteBatch, _font, tabRect, _tabs[i], _pixelTexture, isHovered, isClicked, tabColor);
        }

        cursorY += 40;

        // Draw Content Area
        Rectangle contentRect = new(_windowRect.X + UITheme.Padding, cursorY, _windowRect.Width - (UITheme.Padding * 2), _windowRect.Height - (cursorY - _windowRect.Y) - UITheme.Padding);
        
        // Scissor test to clip content
        Rectangle currentScissor = spriteBatch.GraphicsDevice.ScissorRectangle;
        
        // End current batch to apply scissor
        spriteBatch.End();
        
        spriteBatch.GraphicsDevice.ScissorRectangle = Rectangle.Intersect(currentScissor, contentRect);
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, new RasterizerState { ScissorTestEnable = true });

        // Get the target object based on tab
        object targetConfig = _currentTab switch
        {
            0 => _config.Agent,
            1 => _config.Plant,
            2 => _config.Brain,
            3 => _config.Genetics,
            _ => _config.Agent
        };

        DrawConfigProperties(spriteBatch, targetConfig, contentRect);

        spriteBatch.End();
        
        // Restore scissor and batch
        spriteBatch.GraphicsDevice.ScissorRectangle = currentScissor;
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
    }

    private void DrawConfigProperties(SpriteBatch sb, object configObject, Rectangle area)
    {
        int y = area.Y - _scrollOffset;
        int itemHeight = 50;

        PropertyInfo[] properties = configObject.GetType().GetProperties();

        foreach (var prop in properties)
        {
            // Check for Range attribute
            var rangeAttr = prop.GetCustomAttribute<RangeAttribute>();
            if (rangeAttr == null) continue;

            // Only handle float, double, int
            if (prop.PropertyType != typeof(float) && prop.PropertyType != typeof(double) && prop.PropertyType != typeof(int)) continue;

            // Optimization: Don't draw if outside view
            if (y > area.Bottom) break;
            if (y + itemHeight < area.Top) 
            {
                y += itemHeight;
                continue;
            }

            Rectangle itemRect = new(area.X, y, area.Width, itemHeight);
            
            // Draw Slider
            string label = SplitCamelCase(prop.Name);
            
            if (prop.PropertyType == typeof(float))
            {
                float val = (float)prop.GetValue(configObject);
                float min = Convert.ToSingle(rangeAttr.Minimum);
                float max = Convert.ToSingle(rangeAttr.Maximum);
                
                if (UIComponents.DrawSlider(sb, _font, itemRect, label, ref val, min, max, _pixelTexture))
                {
                    prop.SetValue(configObject, val);
                }
            }
            else if (prop.PropertyType == typeof(int))
            {
                int val = (int)prop.GetValue(configObject);
                int min = Convert.ToInt32(rangeAttr.Minimum);
                int max = Convert.ToInt32(rangeAttr.Maximum);
                
                if (UIComponents.DrawSlider(sb, _font, itemRect, label, ref val, min, max, _pixelTexture))
                {
                    prop.SetValue(configObject, val);
                }
            }
            else if (prop.PropertyType == typeof(double))
            {
                double val = (double)prop.GetValue(configObject);
                double min = Convert.ToDouble(rangeAttr.Minimum);
                double max = Convert.ToDouble(rangeAttr.Maximum);
                
                if (UIComponents.DrawSlider(sb, _font, itemRect, label, ref val, min, max, _pixelTexture))
                {
                    prop.SetValue(configObject, val);
                }
            }

            y += itemHeight;
        }
    }

    private string SplitCamelCase(string input)
    {
        return System.Text.RegularExpressions.Regex.Replace(input, "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
    }
}
