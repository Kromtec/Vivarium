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
    private readonly Texture2D _dotTexture;

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                if (_isVisible)
                {
                    RequiresRefresh = true;
                }
            }
        }
    }
    private bool _isVisible;
    public bool RequiresRefresh { get; set; }

    private Rectangle _windowRect;

    // Data
    private readonly GenomeCensus _census; // Use shared census
    private GenomeFamily _selectedFamily;
    private int _selectedVariantIndex = 0;
    private DietType? _filterDiet = null; // null = All

    // UI State
    private int _scrollOffset = 0;
    private const int ItemHeight = 40;
    private const int ListWidth = 450; // Increased from 350 to 450
    private int _previousScrollValue;
    private MouseState _previousMouseState;

    // Texture Cache
    private readonly Dictionary<ulong, Texture2D> _helixCache = [];
    private readonly Dictionary<ulong, Texture2D> _gridCache = [];
    private readonly Dictionary<ulong, Texture2D> _agentTextureCache = [];

    public GenePoolWindow(GraphicsDevice graphics, SpriteFont font, GenomeCensus census)
    {
        _graphics = graphics;
        _font = font;
        _census = census;
        _pixelTexture = new Texture2D(graphics, 1, 1);
        _pixelTexture.SetData([Color.White]);
        _dotTexture = TextureGenerator.CreateCircle(graphics, 32);
    }

    // Removed GetVariantName - moved to GenomeCensus

    public void RefreshData(Agent[] agents)
    {
        // Delegate to Census
        _census.AnalyzePopulation(agents);

        // Generate Textures for the new census data
        foreach (var family in _census.TopFamilies)
        {
            // Generate texture for the family representative
            if (family.Identicon == null)
            {
                family.Identicon = GetOrGenerateHelix(family.Representative.Representative);
            }

            // Generate Agent Texture for list view
            if (family.AgentTexture == null)
            {
                family.AgentTexture = GetOrGenerateAgentTexture(family.Representative.Representative);
            }

            // Generate textures for variants
            foreach (var variant in family.Variants)
            {
                if (variant.Identicon == null)
                {
                    variant.Identicon = GetOrGenerateHelix(variant.Representative);
                }
                if (variant.GenomeGrid == null)
                {
                    variant.GenomeGrid = GetOrGenerateGrid(variant.Representative);
                }
            }
        }

        // Update local selection logic based on _census.TopFamilies
        UpdateSelection();
    }

    private void UpdateSelection()
    {
        var topFamilies = GetFilteredFamilies();

        // Update selection reference
        if (_selectedFamily != null)
        {
            // Try to find the same family (by Representative Hash)
            var newRef = topFamilies.FirstOrDefault(f => f.Representative.Hash == _selectedFamily.Representative.Hash);
            _selectedFamily = newRef;

            // Clamp variant index
            if (_selectedFamily != null)
            {
                _selectedVariantIndex = Math.Clamp(_selectedVariantIndex, 0, _selectedFamily.Variants.Count - 1);
            }
        }

        // Select first if none selected
        if (_selectedFamily == null && topFamilies.Count > 0)
        {
            _selectedFamily = topFamilies[0];
            _selectedVariantIndex = 0;
        }
    }

    private List<GenomeFamily> GetFilteredFamilies()
    {
        IEnumerable<GenomeFamily> filtered = _census.TopFamilies;
        if (_filterDiet.HasValue)
        {
            filtered = filtered.Where(g => g.Diet == _filterDiet.Value);
        }
        return filtered.Take(20).ToList();
    }

    private Texture2D GetOrGenerateHelix(Agent agent)
    {
        ulong hash = Genetics.CalculateGenomeHash(agent.Genome);
        if (!_helixCache.TryGetValue(hash, out var texture))
        {
            texture = GenomeHelper.GenerateHelixTexture(_graphics, agent);
            _helixCache[hash] = texture;
        }
        return texture;
    }

    private Texture2D GetOrGenerateAgentTexture(Agent agent)
    {
        ulong hash = Genetics.CalculateGenomeHash(agent.Genome);
        if (!_agentTextureCache.TryGetValue(hash, out var texture))
        {
            // Determine dominant trait for texture selection
            // 0: Strength, 1: Bravery, 2: Metabolism, 3: Perception, 4: Speed, 5: Constitution
            int traitIndex = 0;
            float maxVal = Math.Abs(agent.Strength);

            if (Math.Abs(agent.Bravery) > maxVal) { maxVal = Math.Abs(agent.Bravery); traitIndex = 1; }
            if (Math.Abs(agent.MetabolicEfficiency) > maxVal) { maxVal = Math.Abs(agent.MetabolicEfficiency); traitIndex = 2; }
            if (Math.Abs(agent.Perception) > maxVal) { maxVal = Math.Abs(agent.Perception); traitIndex = 3; }
            if (Math.Abs(agent.Speed) > maxVal) { maxVal = Math.Abs(agent.Speed); traitIndex = 4; }
            if (Math.Abs(agent.Constitution) > maxVal) { traitIndex = 5; }

            texture = TextureGenerator.CreateAgentTexture(_graphics, 64, agent.Diet, traitIndex);
            _agentTextureCache[hash] = texture;
        }
        return texture;
    }

    private Texture2D GetOrGenerateGrid(Agent agent)
    {
        ulong hash = Genetics.CalculateGenomeHash(agent.Genome);
        if (!_gridCache.TryGetValue(hash, out var texture))
        {
            texture = GenomeHelper.GenerateGenomeGridTexture(_graphics, agent);
            _gridCache[hash] = texture;
        }
        return texture;
    }

    public void UpdateInput()
    {
        var mouse = Mouse.GetState();

        int scrollDelta = mouse.ScrollWheelValue - _previousScrollValue;
        _previousScrollValue = mouse.ScrollWheelValue;

        if (!IsVisible)
        {
            _previousMouseState = mouse;
            return;
        }

        if (!IsMouseOver(mouse.Position))
        {
            _previousMouseState = mouse;
            return;
        }

        var topFamilies = GetFilteredFamilies();

        if (scrollDelta != 0)
        {
            _scrollOffset -= scrollDelta / 2;

            // Clamp Scroll
            int maxScroll = Math.Max(0, (topFamilies.Count * ItemHeight) - (_windowRect.Height - 100));
            _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);
        }

        // Single click detection
        bool isClick = mouse.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released;

        if (isClick)
        {
            Point mousePos = mouse.Position;

            // Filter Buttons
            const int buttonHeight = 24;
            int filterY = _windowRect.Y + 45;
            int filterX = _windowRect.X + UITheme.Padding;

            // All (Wider)
            if (new Rectangle(filterX, filterY, 45, buttonHeight).Contains(mousePos))
            {
                _filterDiet = null;
                RequiresRefresh = true;
            }
            // Herbivore
            if (new Rectangle(filterX + 50, filterY, 35, buttonHeight).Contains(mousePos))
            {
                _filterDiet = DietType.Herbivore;
                RequiresRefresh = true;
            }
            // Omnivore
            if (new Rectangle(filterX + 90, filterY, 35, buttonHeight).Contains(mousePos))
            {
                _filterDiet = DietType.Omnivore;
                RequiresRefresh = true;
            }
            // Carnivore
            if (new Rectangle(filterX + 130, filterY, 35, buttonHeight).Contains(mousePos))
            {
                _filterDiet = DietType.Carnivore;
                RequiresRefresh = true;
            }

            // List Area
            int listY = _windowRect.Y + 80;
            int listHeight = _windowRect.Height - 90;
            Rectangle listRect = new(_windowRect.X + UITheme.Padding, listY, ListWidth, listHeight);

            if (listRect.Contains(mousePos))
            {
                int relativeY = mousePos.Y - listRect.Y + _scrollOffset;
                int index = relativeY / ItemHeight;

                if (index >= 0 && index < topFamilies.Count)
                {
                    if (_selectedFamily != topFamilies[index])
                    {
                        _selectedFamily = topFamilies[index];
                        _selectedVariantIndex = 0; // Reset variant selection when changing family
                    }
                }
            }

            // Variant Navigation Buttons (Next/Prev)
            if (_selectedFamily != null && _selectedFamily.Variants.Count > 1)
            {
                int detailsX = _windowRect.X + UITheme.Padding + ListWidth + 30;
                int detailsY = _windowRect.Y + 50 + 30; // Header + Padding

                // Info block is now right aligned
                const int infoWidth = 250;
                int infoX = _windowRect.Right - UITheme.Padding - infoWidth;
                int infoY = detailsY + 75; // ID (40) + Diet (35) spacing

                // Prev Button
                if (new Rectangle(infoX + infoWidth - 30 - 150 - 30, infoY, 30, 20).Contains(mousePos))
                {
                    _selectedVariantIndex--;
                    if (_selectedVariantIndex < 0) _selectedVariantIndex = _selectedFamily.Variants.Count - 1;
                }

                // Next Button
                if (new Rectangle(infoX + infoWidth - 30, infoY, 30, 20).Contains(mousePos))
                {
                    _selectedVariantIndex++;
                    if (_selectedVariantIndex >= _selectedFamily.Variants.Count) _selectedVariantIndex = 0;
                }
            }

            // Close Button
            const int closeBtnSize = 20;
            Rectangle closeRect = new(_windowRect.Right - closeBtnSize - UITheme.Padding, _windowRect.Y + UITheme.Padding, closeBtnSize, closeBtnSize);
            if (closeRect.Contains(mousePos))
            {
                IsVisible = false;
            }
        }

        _previousMouseState = mouse;
    }

    public bool IsMouseOver(Point mousePos)
    {
        return IsVisible && _windowRect.Contains(mousePos);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible) return;

        // Center Window
        const int width = 1280;
        const int height = 600;
        _windowRect = new Rectangle(
            (_graphics.Viewport.Width - width) / 2,
            (_graphics.Viewport.Height - height) / 2,
            width,
            height
        );

        // Background
        UIComponents.DrawPanel(spriteBatch, _windowRect, _pixelTexture);

        // Header
        Vector2 headerPos = new(_windowRect.X + UITheme.Padding, _windowRect.Y + UITheme.Padding);
        const string headerText = "GENOME CENSUS (TOP 20 FAMILIES)";
        spriteBatch.DrawString(_font, headerText, new Vector2(headerPos.X, headerPos.Y + 3), UITheme.HeaderColor);

        // Filter Buttons
        const int buttonHeight = 24;
        int filterY = _windowRect.Y + 45;
        int filterX = _windowRect.X + UITheme.Padding;

        DrawFilterButton(spriteBatch, "ALL", null, filterX, filterY, buttonHeight, 45);
        DrawFilterButton(spriteBatch, "H", DietType.Herbivore, filterX + 50, filterY, buttonHeight);
        DrawFilterButton(spriteBatch, "O", DietType.Omnivore, filterX + 90, filterY, buttonHeight);
        DrawFilterButton(spriteBatch, "C", DietType.Carnivore, filterX + 130, filterY, buttonHeight);

        // Close Button
        const int closeBtnSize = 20;
        Rectangle closeBtnRect = new(_windowRect.Right - closeBtnSize - UITheme.Padding, _windowRect.Y + UITheme.Padding, closeBtnSize, closeBtnSize);

        // Use UIComponents for Close Button? It's a bit custom with the 'X'.
        // But we can use DrawButton logic partially or just keep it as is for now to avoid breaking the 'X' centering logic which is specific.
        // Actually, let's use DrawButton but with "X" text.
        // Check if mouse is over for hover effect
        var mouse = Mouse.GetState();
        bool closeHover = closeBtnRect.Contains(mouse.Position);
        bool closePress = closeHover && mouse.LeftButton == ButtonState.Pressed;

        UIComponents.DrawButton(spriteBatch, _font, closeBtnRect, "X", _pixelTexture, closeHover, closePress, UITheme.BadColor);

        // --- LEFT PANEL: LIST ---
        int listX = _windowRect.X + UITheme.Padding;
        int listY = _windowRect.Y + 80;
        int listHeight = _windowRect.Height - 90;

        // Scissor Test
        Rectangle currentScissor = _graphics.ScissorRectangle;
        Rectangle listClipRect = new(listX, listY, ListWidth, listHeight);

        listClipRect = Rectangle.Intersect(listClipRect, _graphics.Viewport.Bounds);

        spriteBatch.End();

        _graphics.ScissorRectangle = listClipRect;

        RasterizerState rasterizerState = new() { ScissorTestEnable = true };
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, rasterizerState);

        var topFamilies = GetFilteredFamilies();

        for (int i = 0; i < topFamilies.Count; i++)
        {
            var family = topFamilies[i];
            int itemY = listY + (i * ItemHeight) - _scrollOffset;

            // Don't draw if outside view
            if (itemY + ItemHeight < listY || itemY > listY + listHeight) continue;

            // Highlight Selection
            if (_selectedFamily != null && _selectedFamily.Representative.Hash == family.Representative.Hash)
            {
                spriteBatch.Draw(_pixelTexture, new Rectangle(listX, itemY, ListWidth, ItemHeight), Color.White * 0.1f);
            }

            // Identicon (Family Representative)
            Rectangle iconRect = new(listX + 5, itemY + 5, 30, 30); // Square for agent texture

            // Draw Agent Texture
            if (family.AgentTexture != null)
            {
                spriteBatch.Draw(family.AgentTexture, iconRect, family.Representative.Representative.Color);
            }

            // Draw Family Indicator (Dots) if multiple variants
            if (family.Variants.Count > 1)
            {
                DrawFamilyDots(spriteBatch, iconRect);
            }

            // Text
            string rankText = $"#{family.Rank}";
            spriteBatch.DrawString(_font, rankText, new Vector2(listX + 50, itemY + 8), UITheme.TextColorPrimary);

            // Scientific Name
            Vector2 rankSize = _font.MeasureString(rankText);
            spriteBatch.DrawString(_font, family.ScientificName, new Vector2(listX + 50 + rankSize.X + 10, itemY + 8), UITheme.TextColorPrimary);

            // Count Text
            string countText = $"{family.TotalCount}";
            if (family.Variants.Count > 1)
            {
                countText += $" ({family.Variants.Count})";
            }

            Vector2 countSize = _font.MeasureString(countText);
            float countX = listX + ListWidth - countSize.X - 15;

            spriteBatch.DrawString(_font, countText, new Vector2(countX, itemY + 8), UITheme.TextColorSecondary);
        }

        spriteBatch.End();

        _graphics.ScissorRectangle = currentScissor;
        spriteBatch.Begin();

        // Scrollbar
        int totalContentHeight = topFamilies.Count * ItemHeight;
        if (totalContentHeight > listHeight)
        {
            const int scrollbarWidth = 6;
            int scrollbarX = listX + ListWidth - scrollbarWidth;

            // Track
            spriteBatch.Draw(_pixelTexture, new Rectangle(scrollbarX, listY, scrollbarWidth, listHeight), Color.Black * 0.3f);

            // Thumb
            float viewRatio = (float)listHeight / totalContentHeight;
            int thumbHeight = Math.Max(20, (int)(listHeight * viewRatio));
            float scrollRatio = (float)_scrollOffset / (totalContentHeight - listHeight);
            int thumbY = listY + (int)(scrollRatio * (listHeight - thumbHeight));

            thumbY = Math.Clamp(thumbY, listY, listY + listHeight - thumbHeight);

            spriteBatch.Draw(_pixelTexture, new Rectangle(scrollbarX, thumbY, scrollbarWidth, thumbHeight), UITheme.ScrollThumbColor);
        }

        // Separator
        spriteBatch.Draw(_pixelTexture, new Rectangle(listX + ListWidth + 10, listY, 2, listHeight), UITheme.BorderColor);

        // Details Panel
        if (_selectedFamily != null)
        {
            int detailsX = listX + ListWidth + 30;
            int detailsY = _windowRect.Y + 50;
            int detailsWidth = _windowRect.Width - (ListWidth + 30 + (UITheme.Padding * 2));

            // Get the currently selected variant within the family
            if (_selectedVariantIndex >= _selectedFamily.Variants.Count) _selectedVariantIndex = 0;
            var variant = _selectedFamily.Variants[_selectedVariantIndex];

            spriteBatch.DrawString(_font, "GENOME DETAILS", new Vector2(detailsX, detailsY), UITheme.HeaderColor);
            detailsY += 30;

            // Helix (Variant)
            Rectangle bigIconRect = new(detailsX, detailsY, 256, 128);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
            spriteBatch.Draw(variant.Identicon, bigIconRect, Color.White);
            spriteBatch.End();
            spriteBatch.Begin();

            // Info Block (Right Aligned)
            const int infoWidth = 250;
            int infoX = _windowRect.Right - UITheme.Padding - infoWidth;
            int infoY = detailsY;

            // Scientific Name + Variant Name (Large)
            string fullName = $"{_selectedFamily.ScientificName} {variant.VariantName}";
            Vector2 nameSize = _font.MeasureString(fullName);
            spriteBatch.DrawString(_font, fullName, new Vector2(infoX + infoWidth - nameSize.X, infoY - 30), UITheme.HeaderColor);

            // Translation (Small, below name)
            string translation = $"\"{_selectedFamily.ScientificNameTranslation}\"";
            Vector2 transSize = _font.MeasureString(translation);
            spriteBatch.DrawString(_font, translation, new Vector2(infoX + infoWidth - transSize.X, infoY - 10), Color.Gray);

            // ID
            string idText = $"ID: {variant.Hash:X}";
            Vector2 idSize = _font.MeasureString(idText);
            spriteBatch.DrawString(_font, idText, new Vector2(infoX + infoWidth - idSize.X, infoY + 15), UITheme.TextColorSecondary);
            infoY += 40;

            // Diet
            string dietText = $"Diet: {variant.Diet}";
            Vector2 dietSize = _font.MeasureString(dietText);
            spriteBatch.DrawString(_font, dietText, new Vector2(infoX + infoWidth - dietSize.X, infoY), UITheme.TextColorPrimary);
            infoY += 35;

            // Variant Navigation
            if (_selectedFamily.Variants.Count > 1)
            {
                // Next Button (Rightmost)
                Rectangle nextRect = new(infoX + infoWidth - 30, infoY, 30, 20);
                bool nextHover = nextRect.Contains(mouse.Position);
                bool nextPress = nextHover && mouse.LeftButton == ButtonState.Pressed;
                UIComponents.DrawButton(spriteBatch, _font, nextRect, ">", _pixelTexture, nextHover, nextPress);

                // Variant Info (Index / Total)
                string varInfo = $"({_selectedVariantIndex + 1} / {_selectedFamily.Variants.Count})";
                Vector2 infoSize = _font.MeasureString(varInfo);

                const int textAreaWidth = 150;
                float textX = nextRect.X - textAreaWidth + ((textAreaWidth - infoSize.X) / 2);

                spriteBatch.DrawString(_font, varInfo, new Vector2(textX, infoY + 2), UITheme.TextColorPrimary);

                // Prev Button
                Rectangle prevRect = new(nextRect.X - textAreaWidth - 30, infoY, 30, 20);
                bool prevHover = prevRect.Contains(mouse.Position);
                bool prevPress = prevHover && mouse.LeftButton == ButtonState.Pressed;
                UIComponents.DrawButton(spriteBatch, _font, prevRect, "<", _pixelTexture, prevHover, prevPress);
            }
            else
            {
                string countText = $"Count: {variant.Count}";
                Vector2 countSize = _font.MeasureString(countText);
                spriteBatch.DrawString(_font, countText, new Vector2(infoX + infoWidth - countSize.X, infoY), UITheme.TextColorPrimary);
            }

            detailsY += 135;

            detailsY += 30;

            // Traits
            spriteBatch.DrawString(_font, "TRAITS", new Vector2(detailsX, detailsY), UITheme.HeaderColor);
            detailsY += 25;

            DrawTraitBar(spriteBatch, "Strength", variant.Representative.Strength, detailsX, ref detailsY);
            DrawTraitBar(spriteBatch, "Bravery", variant.Representative.Bravery, detailsX, ref detailsY);
            DrawTraitBar(spriteBatch, "Metabolism", variant.Representative.MetabolicEfficiency, detailsX, ref detailsY);
            DrawTraitBar(spriteBatch, "Perception", variant.Representative.Perception, detailsX, ref detailsY);
            DrawTraitBar(spriteBatch, "Speed", variant.Representative.Speed, detailsX, ref detailsY);
            DrawTraitBar(spriteBatch, "Trophic Bias", variant.Representative.TrophicBias, detailsX, ref detailsY);
            DrawTraitBar(spriteBatch, "Constitution", variant.Representative.Constitution, detailsX, ref detailsY);

            // Genome Grid (Bottom Aligned)
            if (variant.GenomeGrid != null)
            {
                int gridWidth = variant.GenomeGrid.Width;
                int gridHeight = variant.GenomeGrid.Height;

                int gridX = detailsX + ((detailsWidth - gridWidth) / 2);
                int gridY = _windowRect.Bottom - UITheme.Padding - gridHeight;

                spriteBatch.Draw(variant.GenomeGrid, new Vector2(gridX, gridY), Color.White);
            }
        }
        else
        {
            int detailsX = listX + ListWidth + 30;
            spriteBatch.DrawString(_font, "Select a genome family to view details.", new Vector2(detailsX, listY + 100), Color.Gray);
        }
    }

    private void DrawFamilyDots(SpriteBatch sb, Rectangle iconRect)
    {
        const int dotSize = 6;
        const int spacing = 2;
        const int totalWidth = (dotSize * 3) + (spacing * 2);

        int startX = iconRect.Right - totalWidth - 2;
        int startY = iconRect.Bottom - dotSize - 2;

        Color dotColor = Color.Gold;
        Color outlineColor = UITheme.PanelBgColor;

        for (int i = 0; i < 3; i++)
        {
            int x = startX + (i * (dotSize + spacing));
            int y = startY;

            // Outline
            sb.Draw(_dotTexture, new Rectangle(x - 1, y - 1, dotSize + 2, dotSize + 2), outlineColor);
            // Dot
            sb.Draw(_dotTexture, new Rectangle(x, y, dotSize, dotSize), dotColor);
        }
    }

    private void DrawFilterButton(SpriteBatch sb, string label, DietType? type, int x, int y, int height, int width = 35)
    {
        bool isSelected = _filterDiet == type;
        Rectangle rect = new(x, y, width, height);

        Color? overrideColor = null;
        if (type.HasValue && isSelected)
        {
            overrideColor = Agent.GetColorBasedOnDietType(type.Value);
        }
        else if (isSelected)
        {
            overrideColor = UITheme.ButtonColor; // Or a selected color
        }

        // Use UIComponents.DrawButton
        // We need to handle the "Selected" state which might differ from Hover/Press
        // For now, let's just use the overrideColor logic

        var mouse = Mouse.GetState();
        bool isHover = rect.Contains(mouse.Position);
        bool isPress = isHover && mouse.LeftButton == ButtonState.Pressed;

        UIComponents.DrawButton(sb, _font, rect, label, _pixelTexture, isHover, isPress, overrideColor);
    }

    private void DrawTraitBar(SpriteBatch sb, string label, float value, int x, ref int y)
    {
        sb.DrawString(_font, label, new Vector2(x, y), UITheme.TextColorSecondary);

        const int barWidth = 150;
        const int barHeight = 10;

        int windowRight = _windowRect.Right - UITheme.Padding;
        int barX = windowRight - barWidth;

        UIComponents.DrawBrainBar(sb, new Rectangle(barX, y + 5, barWidth, barHeight), value, false, _pixelTexture);

        y += 25;
    }

    // Removed DrawBorder - using UIComponents.DrawBorder
    // Removed GenomeFamily/GenomeVariant classes - moved to GenomeCensus.cs
}
