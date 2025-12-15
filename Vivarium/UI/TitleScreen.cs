using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Vivarium.Visuals;

namespace Vivarium.UI;

public class TitleScreen
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteFont _font;
    private Texture2D[] _structureTextures;
    private Texture2D[] _plantTextures;
    private Texture2D _pixelTexture;
    
    private struct PlantDecoration
    {
        public Vector2 RelativePosition;
        public int TextureIndex;
        public float Scale;
        public float Rotation;
        public Color Color;
    }

    private List<PlantDecoration> _decorations = new();

    // Letter definitions (7x9 grids)
    private static readonly Dictionary<char, string[]> Letters = new()
    {
        {'V', new[] { 
            "1000001", 
            "1000001", 
            "1000001", 
            "1000001", 
            "0100010", 
            "0100010", 
            "0010100", 
            "0010100", 
            "0001000" 
        }},
        {'I', new[] { 
            "0111110", 
            "0001000", 
            "0001000", 
            "0001000", 
            "0001000", 
            "0001000", 
            "0001000", 
            "0001000", 
            "0111110" 
        }},
        {'A', new[] { 
            "0011100", 
            "0100010", 
            "1000001", 
            "1000001", 
            "1111111", 
            "1000001", 
            "1000001", 
            "1000001", 
            "1000001" 
        }},
        {'R', new[] { 
            "1111110", 
            "1000001", 
            "1000001", 
            "1000001", 
            "1111110", 
            "1001000", 
            "1000100", 
            "1000010", 
            "1000001" 
        }},
        {'U', new[] { 
            "1000001", 
            "1000001", 
            "1000001", 
            "1000001", 
            "1000001", 
            "1000001", 
            "1000001", 
            "0111110", 
            "0011100" 
        }},
        {'M', new[] { 
            "1000001", 
            "1100011", 
            "1010101", 
            "1001001", 
            "1000001", 
            "1000001", 
            "1000001", 
            "1000001", 
            "1000001" 
        }}
    };

    public TitleScreen(GraphicsDevice graphicsDevice, SpriteFont font)
    {
        _graphicsDevice = graphicsDevice;
        _font = font;
        LoadContent();
        GenerateDecorations();
    }

    private void LoadContent()
    {
        // Generate Structure Textures (16 variations) matching in-game parameters
        // Size 50, Corner 15, Border 4
        _structureTextures = new Texture2D[16];
        for (int i = 0; i < 16; i++)
        {
            bool top = (i & 1) != 0;
            bool right = (i & 2) != 0;
            bool bottom = (i & 4) != 0;
            bool left = (i & 8) != 0;
            _structureTextures[i] = TextureGenerator.CreateStructureTexture(_graphicsDevice, 50, 15, 4, top, right, bottom, left);
        }

        // Generate Plant Textures (Variations)
        _plantTextures = new Texture2D[4];
        int plantBorder = 10; 
        _plantTextures[0] = TextureGenerator.CreateOrganicShape(_graphicsDevice, 64, 5, 0.2f, plantBorder); // Standard Flower
        _plantTextures[1] = TextureGenerator.CreateOrganicShape(_graphicsDevice, 64, 3, 0.15f, plantBorder); // Triangle/Tulip
        _plantTextures[2] = TextureGenerator.CreateOrganicShape(_graphicsDevice, 64, 6, 0.25f, plantBorder); // Complex Flower
        _plantTextures[3] = TextureGenerator.CreateOrganicShape(_graphicsDevice, 64, 4, 0.2f, plantBorder); // Clover
        
        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    private void GenerateDecorations()
    {
        _decorations.Clear();
        var rng = new Random(12345); // Fixed seed for consistency

        string title = "VIVARIUM";
        
        // Layout parameters for generation (must match Draw)
        float blockSize = 28f; 
        float spacing = 0f; 
        
        float letterSpacing = 14f;
        
        List<Vector2> occupiedBlocks = new();
        
        float currentX = 0;
        float cursorY = 0;

        // 1. Calculate occupied positions
        foreach (char c in title)
        {
            if (Letters.ContainsKey(c))
            {
                string[] grid = Letters[c];
                for (int row = 0; row < 9; row++)
                {
                    for (int col = 0; col < 7; col++)
                    {
                        if (grid[row][col] == '1')
                        {
                            float bx = currentX + col * (blockSize + spacing);
                            float by = cursorY + row * (blockSize + spacing);
                            occupiedBlocks.Add(new Vector2(bx, by));
                        }
                    }
                }
            }
            currentX += (7 * blockSize) + (6 * spacing) + letterSpacing;
        }

        // Center the decorations relative to the title block
        float totalWidth = currentX - letterSpacing;
        float totalHeight = 9 * blockSize;
        Vector2 centerOffset = new Vector2(totalWidth / 2f, totalHeight / 2f);

        // 2. Generate plants
        int plantCount = 150; 
        for(int i=0; i<plantCount; i++)
        {
            // Pick a random occupied block
            if (occupiedBlocks.Count == 0) break;
            Vector2 anchor = occupiedBlocks[rng.Next(occupiedBlocks.Count)];
            
            // Pick a random offset
            float angle = (float)(rng.NextDouble() * Math.PI * 2);
            float dist = blockSize * (0.5f + (float)rng.NextDouble() * 0.8f); 
            
            Vector2 plantPos = anchor + new Vector2(MathF.Cos(angle) * dist, MathF.Sin(angle) * dist);
            
            // Check collision with blocks (don't draw on top of blocks)
            bool collides = false;
            foreach(var block in occupiedBlocks)
            {
                // Simple circle collision
                if (Vector2.Distance(block, plantPos) < blockSize * 0.6f)
                {
                    collides = true;
                    break;
                }
            }
            
            if (!collides)
            {
                // Add decoration
                // Store position relative to the title's top-left
                _decorations.Add(new PlantDecoration
                {
                    RelativePosition = plantPos,
                    TextureIndex = rng.Next(_plantTextures.Length),
                    Scale = 0.3f + (float)rng.NextDouble() * 0.4f, // Random scale
                    Rotation = (float)(rng.NextDouble() * Math.PI * 2),
                    Color = Color.Lerp(VivariumColors.Plant, Color.DarkGreen, (float)rng.NextDouble() * 0.3f)
                });
            }
        }
    }

    public void Update(MouseState mouseState, KeyboardState keyboardState, KeyboardState prevKeyboardState, ref GameState gameState, Action onExit)
    {
        // Exit on ESC
        if (keyboardState.IsKeyDown(Keys.Escape) && !prevKeyboardState.IsKeyDown(Keys.Escape))
        {
            onExit?.Invoke();
        }

        int screenWidth = _graphicsDevice.Viewport.Width;
        int screenHeight = _graphicsDevice.Viewport.Height;

        int btnWidth = 200;
        int btnHeight = 50;
        Rectangle startBtnRect = new Rectangle(
            (screenWidth - btnWidth) / 2,
            (screenHeight - btnHeight) / 2 + 150,
            btnWidth,
            btnHeight
        );

        if (startBtnRect.Contains(mouseState.Position) && mouseState.LeftButton == ButtonState.Pressed)
        {
            gameState = GameState.Simulation;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        int screenWidth = _graphicsDevice.Viewport.Width;
        int screenHeight = _graphicsDevice.Viewport.Height;

        // Draw "VIVARIUM"
        string title = "VIVARIUM";
        int blockSize = 28; // Bigger blocks
        int spacing = 0; // No spacing to connect textures
        int letterSpacing = 14;
        
        // Calculate total width
        int totalWidth = 0;
        foreach (char c in title)
        {
            totalWidth += (7 * blockSize) + (6 * spacing) + letterSpacing;
        }
        totalWidth -= letterSpacing; // Remove last spacing

        int startX = (screenWidth - totalWidth) / 2;
        int startY = (screenHeight / 2) - 200; // Moved up slightly more to accommodate taller letters

        // Draw Decorations (Plants) first (behind)
        foreach (var deco in _decorations)
        {
            Texture2D tex = _plantTextures[deco.TextureIndex];
            Vector2 pos = new Vector2(startX, startY) + deco.RelativePosition;
            Vector2 origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
            
            // Offset pos to center of block (since RelativePosition was based on block top-left)
            // Actually in GenerateDecorations: bx = col * size. This is top-left of block.
            // So plantPos is relative to top-left of blocks.
            // We want to draw centered.
            Vector2 drawPos = pos + new Vector2(blockSize/2f, blockSize/2f);

            spriteBatch.Draw(tex, drawPos, null, deco.Color, deco.Rotation, origin, deco.Scale, SpriteEffects.None, 0f);
        }

        // Draw Letters
        int currentX = startX;
        foreach (char c in title)
        {
            DrawLetter(spriteBatch, c, currentX, startY, blockSize, spacing);
            currentX += (7 * blockSize) + (6 * spacing) + letterSpacing;
        }

        // Draw Start Button
        int btnWidth = 200;
        int btnHeight = 50;
        Rectangle startBtnRect = new Rectangle(
            (screenWidth - btnWidth) / 2,
            (screenHeight - btnHeight) / 2 + 150,
            btnWidth,
            btnHeight
        );

        var mouseState = Mouse.GetState();
        bool isHovered = startBtnRect.Contains(mouseState.Position);
        bool isPressed = isHovered && mouseState.LeftButton == ButtonState.Pressed;

        UIComponents.DrawButton(spriteBatch, _font, startBtnRect, "START", _pixelTexture, isHovered, isPressed);

        // Draw Version
        string versionText = "v1.0 - .NET 10 / C# 14";
        Vector2 verSize = _font.MeasureString(versionText);
        spriteBatch.DrawString(_font, versionText, new Vector2(screenWidth - verSize.X - 10, screenHeight - verSize.Y - 10), UITheme.TextColorSecondary);

        spriteBatch.End();
    }

    private void DrawLetter(SpriteBatch sb, char c, int x, int y, int size, int spacing)
    {
        if (!Letters.ContainsKey(c)) return;

        string[] grid = Letters[c];
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                if (grid[row][col] == '1')
                {
                    // Determine neighbors for texture connection
                    bool top = (row > 0) && (grid[row - 1][col] == '1');
                    bool bottom = (row < 8) && (grid[row + 1][col] == '1');
                    bool left = (col > 0) && (grid[row][col - 1] == '1');
                    bool right = (col < 6) && (grid[row][col + 1] == '1');

                    int index = (left ? 8 : 0) + (bottom ? 4 : 0) + (right ? 2 : 0) + (top ? 1 : 0);
                    
                    sb.Draw(_structureTextures[index], new Rectangle(x + col * (size + spacing), y + row * (size + spacing), size, size), VivariumColors.Structure);
                }
            }
        }
    }
}
