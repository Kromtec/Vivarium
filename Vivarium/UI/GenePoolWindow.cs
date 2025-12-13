using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Vivarium.Biology;
using Vivarium.Entities;
using Vivarium.Visuals;

namespace Vivarium.UI;

public class GenePoolWindow
{
    private readonly GraphicsDevice _graphics;
    private readonly SpriteFont _font;
    private readonly Texture2D _pixelTexture;

    public bool IsVisible { get; set; }
    private Rectangle _windowRect;
    
    // Data
    private List<GenomeEntry> _topGenomes = new List<GenomeEntry>();
    private GenomeEntry? _selectedGenome;
    private Texture2D _selectedIdenticon;

    // UI State
    private int _scrollOffset = 0;
    private const int ItemHeight = 40;
    private const int ListWidth = 250;
    private int _previousScrollValue;

    public GenePoolWindow(GraphicsDevice graphics, SpriteFont font)
    {
        _graphics = graphics;
        _font = font;
        _pixelTexture = new Texture2D(graphics, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    public void RefreshData(Agent[] agents)
    {
        // 1. Group agents by Genome Hash
        var groups = new Dictionary<ulong, GenomeEntry>();

        foreach (var agent in agents)
        {
            if (!agent.IsAlive) continue;

            ulong hash = GenomeHelper.CalculateGenomeHash(agent.Genome);

            if (!groups.ContainsKey(hash))
            {
                groups[hash] = new GenomeEntry
                {
                    Hash = hash,
                    Count = 0,
                    Representative = agent, // Store one agent to read traits later
                    Diet = agent.Diet
                };
            }

            var entry = groups[hash];
            entry.Count++;
            groups[hash] = entry;
        }

        // 2. Sort by Count and take Top 20
        _topGenomes = groups.Values
            .OrderByDescending(g => g.Count)
            .Take(20)
            .ToList();

        // 3. Generate Identicons
        foreach (var entry in _topGenomes)
        {
            // Dispose old texture if needed? (For now we just create new ones, might need pooling later)
            entry.Identicon = GenomeHelper.GenerateHelixTexture(_graphics, entry.Representative);
        }

        // Reset selection if it's no longer in the list
        if (_selectedGenome != null && !_topGenomes.Any(g => g.Hash == _selectedGenome.Hash))
        {
            _selectedGenome = null;
        }
    }

    public void UpdateInput()
    {
        var mouse = Mouse.GetState();
        
        // Always update previous scroll value to prevent jumps when window becomes visible
        int scrollDelta = mouse.ScrollWheelValue - _previousScrollValue;
        _previousScrollValue = mouse.ScrollWheelValue;

        if (!IsVisible) return;
        
        // Only handle input if mouse is over the window
        if (!IsMouseOver(mouse.Position)) return;

        if (scrollDelta != 0)
        {
            _scrollOffset -= scrollDelta / 2; // Adjust scroll speed
            
            // Clamp Scroll
            int maxScroll = Math.Max(0, (_topGenomes.Count * ItemHeight) - (_windowRect.Height - 100));
            _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);
        }

        // Simple Click Handling for the List
        if (mouse.LeftButton == ButtonState.Pressed)
        {
            Point mousePos = mouse.Position;
            
            // Check if inside List Area
            Rectangle listRect = new Rectangle(_windowRect.X + UITheme.Padding, _windowRect.Y + 50, ListWidth, _windowRect.Height - 60);
            
            if (listRect.Contains(mousePos))
            {
                int relativeY = mousePos.Y - listRect.Y + _scrollOffset;
                int index = relativeY / ItemHeight;

                if (index >= 0 && index < _topGenomes.Count)
                {
                    _selectedGenome = _topGenomes[index];
                }
            }
            
            // Close Button (Top Right)
            Rectangle closeRect = new Rectangle(_windowRect.Right - 30, _windowRect.Y + 5, 25, 25);
            if (closeRect.Contains(mousePos))
            {
                IsVisible = false;
            }
        }
    }

    public bool IsMouseOver(Point mousePos)
    {
        return IsVisible && _windowRect.Contains(mousePos);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible) return;

        // Center Window
        int width = 700; // Increased width from 600 to 700
        int height = 500;
        _windowRect = new Rectangle(
            (_graphics.Viewport.Width - width) / 2,
            (_graphics.Viewport.Height - height) / 2,
            width,
            height
        );

        // Background
        spriteBatch.Draw(_pixelTexture, new Rectangle(_windowRect.X + UITheme.ShadowOffset, _windowRect.Y + UITheme.ShadowOffset, width, height), UITheme.ShadowColor);
        spriteBatch.Draw(_pixelTexture, _windowRect, UITheme.PanelBgColor);
        DrawBorder(spriteBatch, _windowRect, UITheme.BorderThickness, UITheme.BorderColor);

        // Header
        spriteBatch.DrawString(_font, "GENOME CENSUS (TOP 20)", new Vector2(_windowRect.X + UITheme.Padding, _windowRect.Y + UITheme.Padding), UITheme.HeaderColor);
        
        // Close Button [X]
        spriteBatch.DrawString(_font, "[X]", new Vector2(_windowRect.Right - 30, _windowRect.Y + 5), UITheme.BadColor);

        // --- LEFT PANEL: LIST ---
        int listX = _windowRect.X + UITheme.Padding;
        int listY = _windowRect.Y + 50;
        int listHeight = _windowRect.Height - 60;

        // Scissor Test for Scrolling
        // We need to clip the drawing to the list area so items don't draw outside
        Rectangle currentScissor = _graphics.ScissorRectangle;
        Rectangle listClipRect = new Rectangle(listX, listY, ListWidth, listHeight);
        
        // Ensure clip rect is within viewport
        listClipRect = Rectangle.Intersect(listClipRect, _graphics.Viewport.Bounds);
        
        // End current batch to apply scissor
        spriteBatch.End();
        
        _graphics.ScissorRectangle = listClipRect;
        
        // Start new batch with Scissor Test enabled
        RasterizerState rasterizerState = new RasterizerState { ScissorTestEnable = true };
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, rasterizerState);

        for (int i = 0; i < _topGenomes.Count; i++)
        {
            var entry = _topGenomes[i];
            int itemY = listY + (i * ItemHeight) - _scrollOffset;

            // Optimization: Don't draw if outside view
            if (itemY + ItemHeight < listY || itemY > listY + listHeight) continue;

            // Highlight Selection
            if (_selectedGenome != null && _selectedGenome.Hash == entry.Hash)
            {
                spriteBatch.Draw(_pixelTexture, new Rectangle(listX, itemY, ListWidth, ItemHeight), Color.White * 0.1f);
            }

            // Identicon (Helix)
            // List item height is 40. We want to fit a 64x32 texture.
            // Let's scale it to fit nicely. 60x30 fits well.
            Rectangle iconRect = new Rectangle(listX + 5, itemY + 5, 60, 30);
            spriteBatch.Draw(entry.Identicon, iconRect, Color.White);
            

            // Text
            // Left align Rank
            string rankText = $"#{i + 1}";
            spriteBatch.DrawString(_font, rankText, new Vector2(listX + 70, itemY + 8), UITheme.TextColorPrimary);

            // Right align Count
            // ListWidth is 250. Padding is 15.
            // Let's align it to the right edge of the list item area (ListWidth)
            string countText = $"{entry.Count}";
            Vector2 countSize = _font.MeasureString(countText);
            // Subtract scrollbar width (6) + padding (5)
            float countX = listX + ListWidth - countSize.X - 15; 
            
            spriteBatch.DrawString(_font, countText, new Vector2(countX, itemY + 8), UITheme.TextColorSecondary);
        }

        // End Scissor Batch
        spriteBatch.End();

        // Restore Scissor and Restart Normal Batch
        _graphics.ScissorRectangle = currentScissor;
        spriteBatch.Begin();

        // Scrollbar
        int totalContentHeight = _topGenomes.Count * ItemHeight;
        if (totalContentHeight > listHeight)
        {
            int scrollbarWidth = 6;
            int scrollbarX = listX + ListWidth - scrollbarWidth;
            
            // Track
            spriteBatch.Draw(_pixelTexture, new Rectangle(scrollbarX, listY, scrollbarWidth, listHeight), Color.Black * 0.3f);
            
            // Thumb
            float viewRatio = (float)listHeight / totalContentHeight;
            int thumbHeight = Math.Max(20, (int)(listHeight * viewRatio));
            float scrollRatio = (float)_scrollOffset / (totalContentHeight - listHeight);
            int thumbY = listY + (int)(scrollRatio * (listHeight - thumbHeight));
            
            // Clamp thumbY to ensure it stays within the track
            thumbY = Math.Clamp(thumbY, listY, listY + listHeight - thumbHeight);

            spriteBatch.Draw(_pixelTexture, new Rectangle(scrollbarX, thumbY, scrollbarWidth, thumbHeight), Color.Gray * 0.8f);
        }

        // Separator
        spriteBatch.Draw(_pixelTexture, new Rectangle(listX + ListWidth + 10, listY, 2, height - 70), UITheme.BorderColor);

        // --- RIGHT PANEL: DETAILS ---
        if (_selectedGenome != null)
        {
            int detailsX = listX + ListWidth + 30;
            int detailsY = listY;
            var g = _selectedGenome;

            spriteBatch.DrawString(_font, "GENOME DETAILS", new Vector2(detailsX, detailsY), UITheme.HeaderColor);
            detailsY += 30;

            // Big Identicon (Helix)
            // Original texture is 256x128. Draw at native resolution for best quality.
            Rectangle bigIconRect = new Rectangle(detailsX, detailsY, 256, 128);
            // Use PointClamp sampler state for pixel art scaling
            // We need to end the current batch and start a new one with PointClamp
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
            
            spriteBatch.Draw(g.Identicon, bigIconRect, Color.White);
            
            spriteBatch.End();
            spriteBatch.Begin(); // Restart default batch

            // Border removed for cleaner look
            // DrawBorder(spriteBatch, bigIconRect, 2, Agent.GetColorBasedOnDietType(g.Diet));
            
            // Move cursor below the icon
            detailsY += 135;

            // Hash ID & Diet
            spriteBatch.DrawString(_font, $"ID: {g.Hash:X}", new Vector2(detailsX, detailsY), UITheme.TextColorSecondary);
            detailsY += 20;
            spriteBatch.DrawString(_font, $"Diet: {g.Diet}", new Vector2(detailsX, detailsY), UITheme.TextColorPrimary);
            
            detailsY += 30;

            // Traits
            DrawTraitBar(spriteBatch, "Strength", g.Representative.Strength, detailsX, ref detailsY);
            DrawTraitBar(spriteBatch, "Bravery", g.Representative.Bravery, detailsX, ref detailsY);
            DrawTraitBar(spriteBatch, "Metabolism", g.Representative.MetabolicEfficiency, detailsX, ref detailsY);
            DrawTraitBar(spriteBatch, "Perception", g.Representative.Perception, detailsX, ref detailsY);
            DrawTraitBar(spriteBatch, "Speed", g.Representative.Speed, detailsX, ref detailsY);
            DrawTraitBar(spriteBatch, "Trophic Bias", g.Representative.TrophicBias, detailsX, ref detailsY);
            DrawTraitBar(spriteBatch, "Constitution", g.Representative.Constitution, detailsX, ref detailsY);
        }
        else
        {
            int detailsX = listX + ListWidth + 30;
            spriteBatch.DrawString(_font, "Select a genome to view details.", new Vector2(detailsX, listY + 100), Color.Gray);
        }
    }

    private void DrawTraitBar(SpriteBatch sb, string label, float value, int x, ref int y)
    {
        sb.DrawString(_font, label, new Vector2(x, y), UITheme.TextColorSecondary);
        
        int barWidth = 150;
        int barHeight = 10;
        // Align bar to the right side of the details panel
        // The details panel starts at x. Let's assume a fixed width for the details area or calculate it.
        // The window width is 700. ListWidth is 250. Padding is 15.
        // Details area width = 700 - 250 - 30 - 15 = ~405.
        // Let's align the bar to the right edge of the window minus padding.
        
        int windowRight = _windowRect.Right - UITheme.Padding;
        int barX = windowRight - barWidth;
        
        // Background
        sb.Draw(_pixelTexture, new Rectangle(barX, y + 5, barWidth, barHeight), Color.Black * 0.5f);
        
        // Fill (Normalized -1 to 1 -> 0 to 1 for display simplicity or split?)
        // Let's do split like Inspector
        int centerX = barX + (barWidth / 2);
        float valClamped = Math.Clamp(value, -1f, 1f);
        int fillWidth = (int)((barWidth / 2) * Math.Abs(valClamped));
        
        if (valClamped > 0)
            sb.Draw(_pixelTexture, new Rectangle(centerX, y + 5, fillWidth, barHeight), UITheme.GoodColor);
        else
            sb.Draw(_pixelTexture, new Rectangle(centerX - fillWidth, y + 5, fillWidth, barHeight), Color.Magenta);

        y += 25;
    }

    private void DrawBorder(SpriteBatch sb, Rectangle r, int thickness, Color c)
    {
        sb.Draw(_pixelTexture, new Rectangle(r.X, r.Y, r.Width, thickness), c);
        sb.Draw(_pixelTexture, new Rectangle(r.X, r.Y + r.Height - thickness, r.Width, thickness), c);
        sb.Draw(_pixelTexture, new Rectangle(r.X, r.Y, thickness, r.Height), c);
        sb.Draw(_pixelTexture, new Rectangle(r.X + r.Width - thickness, r.Y, thickness, r.Height), c);
    }

    public class GenomeEntry
    {
        public ulong Hash;
        public int Count;
        public Agent Representative; // A copy or ref to one agent to read traits
        public DietType Diet;
        public Texture2D Identicon;
    }
}
