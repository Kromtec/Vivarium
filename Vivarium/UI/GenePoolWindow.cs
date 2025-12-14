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
    private List<GenomeFamily> _topFamilies = new List<GenomeFamily>();
    private GenomeFamily _selectedFamily;
    private int _selectedVariantIndex = 0;
    private DietType? _filterDiet = null; // null = All

    // UI State
    private int _scrollOffset = 0;
    private const int ItemHeight = 40;
    private const int ListWidth = 270;
    private int _previousScrollValue;
    private MouseState _previousMouseState;

    // Texture Cache
    private readonly Dictionary<ulong, Texture2D> _helixCache = new Dictionary<ulong, Texture2D>();
    private readonly Dictionary<ulong, Texture2D> _gridCache = new Dictionary<ulong, Texture2D>();

    public GenePoolWindow(GraphicsDevice graphics, SpriteFont font)
    {
        _graphics = graphics;
        _font = font;
        _pixelTexture = new Texture2D(graphics, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
        _dotTexture = TextureGenerator.CreateCircle(graphics, 32);
    }

    public void RefreshData(Agent[] agents)
    {
        // 1. Group agents by exact genome hash (Variants)
        var variants = new Dictionary<ulong, GenomeVariant>();

        foreach (var agent in agents)
        {
            if (!agent.IsAlive) continue;

            ulong hash = GenomeHelper.CalculateGenomeHash(agent.Genome);

            if (!variants.ContainsKey(hash))
            {
                variants[hash] = new GenomeVariant
                {
                    Hash = hash,
                    Count = 0,
                    Representative = agent,
                    Diet = agent.Diet
                };
            }

            var entry = variants[hash];
            entry.Count++;
            variants[hash] = entry;
        }

        // Sort variants by Count (Most popular first)
        var sortedVariants = variants.Values
            .OrderByDescending(v => v.Count)
            .ToList();

        // 2. Cluster Variants into Families
        var families = new List<GenomeFamily>();
        var unassigned = new List<GenomeVariant>(sortedVariants);

        // Threshold: 90% similarity to be in the same family
        const float SimilarityThreshold = 0.90f;

        while (unassigned.Count > 0)
        {
            // Take the most popular remaining variant as the seed for a new family
            var seed = unassigned[0];
            unassigned.RemoveAt(0);

            var family = new GenomeFamily
            {
                Representative = seed, // The most popular variant represents the family
                Diet = seed.Diet
            };
            family.Variants.Add(seed);

            // Find all other variants that are similar to the seed
            for (int i = unassigned.Count - 1; i >= 0; i--)
            {
                var candidate = unassigned[i];
                
                // Optimization: Only check similarity if Diet matches (Diet is a hard filter usually)
                if (candidate.Diet == seed.Diet)
                {
                    float similarity = Genetics.CalculateSimilarity(seed.Representative.Genome, candidate.Representative.Genome);
                    if (similarity >= SimilarityThreshold)
                    {
                        family.Variants.Add(candidate);
                        unassigned.RemoveAt(i);
                    }
                }
            }

            // Calculate total count for the family
            family.TotalCount = family.Variants.Sum(v => v.Count);
            
            // Sort variants within family by count
            family.Variants = family.Variants.OrderByDescending(v => v.Count).ToList();

            families.Add(family);
        }

        // 3. Sort Families by Total Count
        var allSortedFamilies = families
            .OrderByDescending(f => f.TotalCount)
            .ToList();

        // Assign Ranks
        for (int i = 0; i < allSortedFamilies.Count; i++)
        {
            allSortedFamilies[i].Rank = i + 1;
        }

        // Filter and Top 20
        IEnumerable<GenomeFamily> filtered = allSortedFamilies;
        if (_filterDiet.HasValue)
        {
            filtered = filtered.Where(g => g.Diet == _filterDiet.Value);
        }

        _topFamilies = filtered.Take(20).ToList();

        // Generate Identicons for Families (using Representative)
        foreach (var family in _topFamilies)
        {
            // Generate texture for the family representative
            family.Identicon = GetOrGenerateHelix(family.Representative.Representative);
            
            // Removed AddFamilyIndicator call to avoid baking dots into the texture
            // Dots will be drawn in Draw()

            // Generate textures for variants if needed (lazy loading would be better but we do it here for simplicity)
            foreach (var variant in family.Variants)
            {
                if (variant.Identicon == null)
                {
                    variant.Identicon = GetOrGenerateHelix(variant.Representative);
                    variant.GenomeGrid = GetOrGenerateGrid(variant.Representative);
                }
            }
        }

        // Update selection reference
        if (_selectedFamily != null)
        {
            // Try to find the same family (by Representative Hash of the seed)
            var newRef = _topFamilies.FirstOrDefault(f => f.Representative.Hash == _selectedFamily.Representative.Hash);
            _selectedFamily = newRef;
            
            // Clamp variant index
            if (_selectedFamily != null)
            {
                _selectedVariantIndex = Math.Clamp(_selectedVariantIndex, 0, _selectedFamily.Variants.Count - 1);
            }
        }

        // Select first if none selected
        if (_selectedFamily == null && _topFamilies.Count > 0)
        {
            _selectedFamily = _topFamilies[0];
            _selectedVariantIndex = 0;
        }
    }

    private Texture2D GetOrGenerateHelix(Agent agent)
    {
        ulong hash = GenomeHelper.CalculateGenomeHash(agent.Genome);
        if (!_helixCache.TryGetValue(hash, out var texture))
        {
            texture = GenomeHelper.GenerateHelixTexture(_graphics, agent);
            _helixCache[hash] = texture;
        }
        return texture;
    }

    private Texture2D GetOrGenerateGrid(Agent agent)
    {
        ulong hash = GenomeHelper.CalculateGenomeHash(agent.Genome);
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

        if (scrollDelta != 0)
        {
            _scrollOffset -= scrollDelta / 2;

            // Clamp Scroll
            int maxScroll = Math.Max(0, (_topFamilies.Count * ItemHeight) - (_windowRect.Height - 100));
            _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);
        }

        // Single click detection
        bool isClick = mouse.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released;

        if (isClick)
        {
            Point mousePos = mouse.Position;

            // Filter Buttons
            int buttonHeight = 24;
            int filterY = _windowRect.Y + 45;
            int filterX = _windowRect.X + UITheme.Padding;

            // All
            if (new Rectangle(filterX, filterY, 35, buttonHeight).Contains(mousePos))
            {
                _filterDiet = null;
                RequiresRefresh = true;
            }
            // Herbivore
            if (new Rectangle(filterX + 40, filterY, 35, buttonHeight).Contains(mousePos)) 
            {
                _filterDiet = DietType.Herbivore;
                RequiresRefresh = true;
            }
            // Omnivore
            if (new Rectangle(filterX + 80, filterY, 35, buttonHeight).Contains(mousePos)) 
            {
                _filterDiet = DietType.Omnivore;
                RequiresRefresh = true;
            }
            // Carnivore
            if (new Rectangle(filterX + 120, filterY, 35, buttonHeight).Contains(mousePos)) 
            {
                _filterDiet = DietType.Carnivore;
                RequiresRefresh = true;
            }

            // List Area
            int listY = _windowRect.Y + 80;
            int listHeight = _windowRect.Height - 90;
            Rectangle listRect = new Rectangle(_windowRect.X + UITheme.Padding, listY, ListWidth, listHeight);

            if (listRect.Contains(mousePos))
            {
                int relativeY = mousePos.Y - listRect.Y + _scrollOffset;
                int index = relativeY / ItemHeight;

                if (index >= 0 && index < _topFamilies.Count)
                {
                    if (_selectedFamily != _topFamilies[index])
                    {
                        _selectedFamily = _topFamilies[index];
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
                int infoWidth = 250;
                int infoX = _windowRect.Right - UITheme.Padding - infoWidth;
                int infoY = detailsY + 60; // ID + Diet lines approx
                
                // Prev Button
                if (new Rectangle(infoX, infoY, 30, 20).Contains(mousePos))
                {
                    _selectedVariantIndex--;
                    if (_selectedVariantIndex < 0) _selectedVariantIndex = _selectedFamily.Variants.Count - 1;
                }
                
                // Next Button
                if (new Rectangle(infoX + 30 + 150, infoY, 30, 20).Contains(mousePos))
                {
                    _selectedVariantIndex++;
                    if (_selectedVariantIndex >= _selectedFamily.Variants.Count) _selectedVariantIndex = 0;
                }
            }

            // Close Button
            int closeBtnSize = 20;
            Rectangle closeRect = new Rectangle(_windowRect.Right - closeBtnSize - UITheme.Padding, _windowRect.Y + UITheme.Padding, closeBtnSize, closeBtnSize);
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
        int width = 1100;
        int height = 600;
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
        Vector2 headerPos = new Vector2(_windowRect.X + UITheme.Padding, _windowRect.Y + UITheme.Padding);
        string headerText = "GENOME CENSUS (TOP 20 FAMILIES)";
        Vector2 headerSize = _font.MeasureString(headerText);
        spriteBatch.DrawString(_font, headerText, new Vector2(headerPos.X, headerPos.Y + 3), UITheme.HeaderColor);

        // Filter Buttons
        int buttonHeight = 24;
        int filterY = _windowRect.Y + 45;
        int filterX = _windowRect.X + UITheme.Padding;

        DrawFilterButton(spriteBatch, "ALL", null, filterX, filterY, buttonHeight);
        DrawFilterButton(spriteBatch, "H", DietType.Herbivore, filterX + 40, filterY, buttonHeight);
        DrawFilterButton(spriteBatch, "O", DietType.Omnivore, filterX + 80, filterY, buttonHeight);
        DrawFilterButton(spriteBatch, "C", DietType.Carnivore, filterX + 120, filterY, buttonHeight);

        // Close Button
        int closeBtnSize = 20;
        Rectangle closeBtnRect = new Rectangle(_windowRect.Right - closeBtnSize - UITheme.Padding, _windowRect.Y + UITheme.Padding, closeBtnSize, closeBtnSize);

        DrawBorder(spriteBatch, closeBtnRect, 1, UITheme.BadColor);

        Vector2 xSize = _font.MeasureString("X");
        Vector2 xPos = new Vector2(
            closeBtnRect.X + (closeBtnRect.Width - xSize.X) / 2,
            closeBtnRect.Y + (closeBtnRect.Height - xSize.Y) / 2 + 1
        );
        spriteBatch.DrawString(_font, "X", xPos, UITheme.BadColor);

        // --- LEFT PANEL: LIST ---
        int listX = _windowRect.X + UITheme.Padding;
        int listY = _windowRect.Y + 80;
        int listHeight = _windowRect.Height - 90;

        // Scissor Test
        Rectangle currentScissor = _graphics.ScissorRectangle;
        Rectangle listClipRect = new Rectangle(listX, listY, ListWidth, listHeight);

        listClipRect = Rectangle.Intersect(listClipRect, _graphics.Viewport.Bounds);

        spriteBatch.End();

        _graphics.ScissorRectangle = listClipRect;

        RasterizerState rasterizerState = new RasterizerState { ScissorTestEnable = true };
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, rasterizerState);

        for (int i = 0; i < _topFamilies.Count; i++)
        {
            var family = _topFamilies[i];
            int itemY = listY + (i * ItemHeight) - _scrollOffset;

            // Don't draw if outside view
            if (itemY + ItemHeight < listY || itemY > listY + listHeight) continue;

            // Highlight Selection
            if (_selectedFamily != null && _selectedFamily.Representative.Hash == family.Representative.Hash)
            {
                spriteBatch.Draw(_pixelTexture, new Rectangle(listX, itemY, ListWidth, ItemHeight), Color.White * 0.1f);
            }

            // Identicon (Family Representative)
            Rectangle iconRect = new Rectangle(listX + 5, itemY + 5, 60, 30);
            spriteBatch.Draw(family.Identicon, iconRect, Color.White);

            // Draw Family Indicator (Dots) if multiple variants
            if (family.Variants.Count > 1)
            {
                DrawFamilyDots(spriteBatch, iconRect);
            }

            // Text
            string rankText = $"#{family.Rank}";
            spriteBatch.DrawString(_font, rankText, new Vector2(listX + 70, itemY + 8), UITheme.TextColorPrimary);

            // Count Text (Total Family Count)
            string countText = $"{family.TotalCount}";
            if (family.Variants.Count > 1)
            {
                countText += $" ({family.Variants.Count} vars)";
            }
            
            Vector2 countSize = _font.MeasureString(countText);
            float countX = listX + ListWidth - countSize.X - 15;

            spriteBatch.DrawString(_font, countText, new Vector2(countX, itemY + 8), UITheme.TextColorSecondary);
        }

        spriteBatch.End();

        _graphics.ScissorRectangle = currentScissor;
        spriteBatch.Begin();

        // Scrollbar
        int totalContentHeight = _topFamilies.Count * ItemHeight;
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

            thumbY = Math.Clamp(thumbY, listY, listY + listHeight - thumbHeight);

            spriteBatch.Draw(_pixelTexture, new Rectangle(scrollbarX, thumbY, scrollbarWidth, thumbHeight), Color.Gray * 0.8f);
        }

        // Separator
        spriteBatch.Draw(_pixelTexture, new Rectangle(listX + ListWidth + 10, listY, 2, listHeight), UITheme.BorderColor);

        // Details Panel
        if (_selectedFamily != null)
        {
            int detailsX = listX + ListWidth + 30;
            int detailsY = _windowRect.Y + 50;
            int detailsWidth = _windowRect.Width - (ListWidth + 30 + UITheme.Padding * 2);
            
            // Get the currently selected variant within the family
            if (_selectedVariantIndex >= _selectedFamily.Variants.Count) _selectedVariantIndex = 0;
            var variant = _selectedFamily.Variants[_selectedVariantIndex];

            spriteBatch.DrawString(_font, "GENOME DETAILS", new Vector2(detailsX, detailsY), UITheme.HeaderColor);
            detailsY += 30;

            // Helix (Variant)
            Rectangle bigIconRect = new Rectangle(detailsX, detailsY, 256, 128);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
            spriteBatch.Draw(variant.Identicon, bigIconRect, Color.White);
            spriteBatch.End();
            spriteBatch.Begin(); 

            // Info Block (Right Aligned)
            int infoWidth = 250;
            int infoX = _windowRect.Right - UITheme.Padding - infoWidth;
            int infoY = detailsY;

            // ID
            spriteBatch.DrawString(_font, $"ID: {variant.Hash:X}", new Vector2(infoX, infoY), UITheme.TextColorSecondary);
            infoY += 25;

            // Diet
            spriteBatch.DrawString(_font, $"Diet: {variant.Diet}", new Vector2(infoX, infoY), UITheme.TextColorPrimary);
            infoY += 35;

            // Variant Navigation
            if (_selectedFamily.Variants.Count > 1)
            {
                // Prev Button
                Rectangle prevRect = new Rectangle(infoX, infoY, 30, 20);
                spriteBatch.Draw(_pixelTexture, prevRect, Color.DarkGray);
                DrawBorder(spriteBatch, prevRect, 1, Color.White);
                spriteBatch.DrawString(_font, "<", new Vector2(prevRect.X + 10, prevRect.Y), Color.White);
                
                // Variant Info
                string varInfo = $"Var {_selectedVariantIndex + 1}/{_selectedFamily.Variants.Count}";
                Vector2 infoSize = _font.MeasureString(varInfo);
                
                // Fixed width area for text to avoid clipping
                int textAreaWidth = 150;
                float textX = infoX + 30 + (textAreaWidth - infoSize.X) / 2;
                
                spriteBatch.DrawString(_font, varInfo, new Vector2(textX, infoY), UITheme.TextColorPrimary);
                
                // Next Button
                Rectangle nextRect = new Rectangle(infoX + 30 + textAreaWidth, infoY, 30, 20);
                spriteBatch.Draw(_pixelTexture, nextRect, Color.DarkGray);
                DrawBorder(spriteBatch, nextRect, 1, Color.White);
                spriteBatch.DrawString(_font, ">", new Vector2(nextRect.X + 10, nextRect.Y), Color.White);
            }
            else
            {
                spriteBatch.DrawString(_font, $"Count: {variant.Count}", new Vector2(infoX, infoY), UITheme.TextColorPrimary);
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
                
                // Center horizontally in details area
                int gridX = detailsX + (detailsWidth - gridWidth) / 2;
                
                // Align to bottom with padding
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
        int dotSize = 6;
        int spacing = 2;
        int totalWidth = (dotSize * 3) + (spacing * 2);
        
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

    private void DrawFilterButton(SpriteBatch sb, string label, DietType? type, int x, int y, int height)
    {
        bool isSelected = _filterDiet == type;
        Rectangle rect = new Rectangle(x, y, 35, height);

        Color bgColor = isSelected ? UITheme.ButtonColor : Color.Black * 0.5f;
        if (type.HasValue)
        {
            if (isSelected) bgColor = Agent.GetColorBasedOnDietType(type.Value);
        }

        sb.Draw(_pixelTexture, rect, bgColor);
        DrawBorder(sb, rect, 1, isSelected ? Color.White : UITheme.BorderColor);

        Vector2 size = _font.MeasureString(label);
        Vector2 pos = new Vector2(x + (rect.Width - size.X) / 2, y + (rect.Height - size.Y) / 2 + 1);
        sb.DrawString(_font, label, pos, Color.White);
    }

    private void DrawTraitBar(SpriteBatch sb, string label, float value, int x, ref int y)
    {
        sb.DrawString(_font, label, new Vector2(x, y), UITheme.TextColorSecondary);

        int barWidth = 150;
        int barHeight = 10;

        int windowRight = _windowRect.Right - UITheme.Padding;
        int barX = windowRight - barWidth;

        // Background
        sb.Draw(_pixelTexture, new Rectangle(barX, y + 5, barWidth, barHeight), Color.Black * 0.5f);

        int centerX = barX + (barWidth / 2);
        
        float displayVal = Math.Clamp(value, -1f, 1f);
        int fillWidth = (int)((barWidth / 2) * Math.Abs(displayVal));
        
        Color barColor = UITheme.GetColorForWeight(value);

        if (displayVal > 0)
            sb.Draw(_pixelTexture, new Rectangle(centerX, y + 5, fillWidth, barHeight), barColor);
        else
            sb.Draw(_pixelTexture, new Rectangle(centerX - fillWidth, y + 5, fillWidth, barHeight), barColor);

        y += 25;
    }

    private void DrawBorder(SpriteBatch sb, Rectangle r, int thickness, Color c)
    {
        sb.Draw(_pixelTexture, new Rectangle(r.X, r.Y, r.Width, thickness), c);
        sb.Draw(_pixelTexture, new Rectangle(r.X, r.Y + r.Height - thickness, r.Width, thickness), c);
        sb.Draw(_pixelTexture, new Rectangle(r.X, r.Y, thickness, r.Height), c);
        sb.Draw(_pixelTexture, new Rectangle(r.X + r.Width - thickness, r.Y, thickness, r.Height), c);
    }

    public class GenomeFamily
    {
        public GenomeVariant Representative; // The seed variant
        public List<GenomeVariant> Variants = new List<GenomeVariant>();
        public int TotalCount;
        public int Rank;
        public DietType Diet;
        public Texture2D Identicon;
    }

    public class GenomeVariant
    {
        public ulong Hash;
        public int Count;
        public Agent Representative;
        public DietType Diet;
        public Texture2D Identicon;
        public Texture2D GenomeGrid;
    }
}
