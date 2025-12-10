using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Vivarium.Entities;
using Vivarium.World;
using Vivarium.Engine;
using Vivarium.Biology;

namespace Vivarium.UI;

public class Inspector
{
    private readonly SpriteFont _font;
    private readonly Texture2D _pixelTexture;

    // Selection State
    public bool IsEntitySelected { get; private set; }
    public Point SelectedGridPos { get; private set; }
    public EntityType SelectedType { get; private set; }
    private long _selectedEntityId = -1;
    private int _selectedIndex;

    // Layout Settings
    private Rectangle _panelRect;
    private int _cursorY;
    private const int Padding = 15;
    private const int LineHeight = 22;
    private const int HighlightBorderThickness = 1;

    // Colors (Modern Palette)
    private readonly Color _panelBgColor = new Color(30, 30, 35, 230); // Dark Slate, semi-transparent
    private readonly Color _borderColor = new Color(60, 60, 70);
    private readonly Color _labelColor = new Color(180, 180, 190);     // Light Grey
    private readonly Color _valueColor = Color.White;
    private readonly Color _headerColor = new Color(100, 200, 255);    // Soft Cyan

    public Inspector(GraphicsDevice graphics, SpriteFont font)
    {
        _font = font;
        _pixelTexture = new Texture2D(graphics, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        // Fixed panel position (Top Left)
        _panelRect = new Rectangle(20, 20, 290, 0); // Height calculates dynamically
    }

    public void UpdateInput(Camera2D camera, GridCell[,] gridMap, Agent[] agents, Plant[] plants, Structure[] structures)
    {
        var mouseState = Mouse.GetState();

        // Left Click to Select
        if (mouseState.LeftButton == ButtonState.Pressed)
        {
            // UI Blocking: Don't select world if clicking inside the panel area
            // (Only works if panel is drawn, keeping height dynamic is tricky, 
            // but we can assume a max height or store last frame height)
            if (IsEntitySelected && _panelRect.Contains(mouseState.Position)) return;

            Vector2 mouseWorld = camera.ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));

            // Assume CellSize = 10 (should be passed in config ideally)
            int cellSize = 10;
            int gx = (int)(mouseWorld.X / cellSize);
            int gy = (int)(mouseWorld.Y / cellSize);

            int w = gridMap.GetLength(0);
            int h = gridMap.GetLength(1);

            if (gx >= 0 && gx < w && gy >= 0 && gy < h)
            {
                var cell = gridMap[gx, gy];
                if (cell.Type != EntityType.Empty)
                {
                    IsEntitySelected = true;
                    SelectedGridPos = new Point(gx, gy);
                    SelectedType = cell.Type;
                    _selectedIndex = cell.Index;
                    if (SelectedType == EntityType.Agent)
                    {
                        _selectedEntityId = agents[_selectedIndex].Id;
                    }
                    else if (SelectedType == EntityType.Plant)
                    {
                        _selectedEntityId = plants[_selectedIndex].Id;
                    }
                    else if (SelectedType == EntityType.Structure)
                    {
                        _selectedEntityId = structures[_selectedIndex].Id;
                    }
                    else
                    {
                        _selectedEntityId = -1;
                    }
                }
                else
                {
                    IsEntitySelected = false;
                }
            }
        }

        // 2.AGENT TRACKING LOGIC
        if (IsEntitySelected && SelectedType == EntityType.Agent)
        {
            if (_selectedIndex >= 0 && _selectedIndex < agents.Length)
            {
                ref Agent trackedAgent = ref agents[_selectedIndex];

                if (trackedAgent.Id == _selectedEntityId && trackedAgent.IsAlive)
                {
                    SelectedGridPos = new Point(trackedAgent.X, trackedAgent.Y);
                }
                else
                {
                    // AGENT DIED OR CHANGED - CLEAR SELECTION
                }
            }
        }
    }

    public void DrawUI(SpriteBatch spriteBatch, Agent[] agents, Plant[] plants, Structure[] structures)
    {
        if (!IsEntitySelected) return;

        // 1. Calculate Content Height dynamically based on what we select
        // This is a bit "hacky" in immediate mode, we just guess/resize or draw background first.
        // Let's set a fixed decent height or make it huge.
        // Better: Draw background AFTER knowing lines? Harder.
        // Simple approach: Draw Background with fixed/large height or adjust per frame.
        int contentHeight = 450;
        _panelRect.Height = contentHeight;

        // Draw Panel Shadow & Background
        spriteBatch.Draw(_pixelTexture, new Rectangle(_panelRect.X + 4, _panelRect.Y + 4, _panelRect.Width, _panelRect.Height), Color.Black * 0.5f);
        spriteBatch.Draw(_pixelTexture, _panelRect, _panelBgColor);

        // Draw Border
        DrawBorder(spriteBatch, _panelRect, 1, _borderColor);

        // Reset Cursor
        _cursorY = _panelRect.Y + Padding;

        // --- HEADER ---
        DrawHeader(spriteBatch, $"{SelectedType.ToString().ToUpper()}");
        DrawSeparator(spriteBatch);

        // --- CONTENT ---
        DrawRow(spriteBatch, "Grid Pos", $"{SelectedGridPos.X}/{SelectedGridPos.Y}");
        DrawRow(spriteBatch, "Index", $"{_selectedIndex}");

        DrawSeparator(spriteBatch);

        switch (SelectedType)
        {
            case EntityType.Agent:
                if (_selectedIndex >= 0 && _selectedIndex < agents.Length)
                {
                    ref Agent agent = ref agents[_selectedIndex];
                    bool isSameAgent = (agent.Id == _selectedEntityId);
                    if (isSameAgent && agent.IsAlive)
                    {
                        DrawRow(spriteBatch, "ID", $"#{agent.Id}");
                        DrawRow(spriteBatch, "Generation", $"{agent.Generation}");
                        DrawRow(spriteBatch, "Age", $"{agent.Age:F0} ticks | {agent.Age / VivariumGame.FramesPerSecond:F0} s");
                        DrawProgressBar(spriteBatch, "Energy", agent.Energy, 100f, Color.Lerp(Color.Red, Color.Lime, agent.Energy / 100f));

                        DrawSeparator(spriteBatch);
                        DrawHeader(spriteBatch, "BRAIN ACTIVITY");

                        // Outputs (Actions) - visualize with centered bars (-1 to 1)
                        DrawBrainBar(spriteBatch, "Move N/S", GetActionVal(ref agent, ActionType.MoveNorth) - GetActionVal(ref agent, ActionType.MoveSouth));
                        DrawBrainBar(spriteBatch, "Move W/E", GetActionVal(ref agent, ActionType.MoveEast) - GetActionVal(ref agent, ActionType.MoveWest));
                        DrawBrainBar(spriteBatch, "Attack", GetActionVal(ref agent, ActionType.Attack), isPositiveOnly: true);
                        DrawBrainBar(spriteBatch, "Reproduce", GetActionVal(ref agent, ActionType.Reproduce), isPositiveOnly: true);
                        DrawBrainBar(spriteBatch, "Suicide", GetActionVal(ref agent, ActionType.KillSelf), isPositiveOnly: true);
                        //DrawSeparator(spriteBatch);
                        //DrawHeader(spriteBatch, "GENOME");
                        //foreach (var gene in agent.Genome)
                        //{
                        //    DrawHeader(spriteBatch, gene.ToString(), Color.LightSeaGreen);
                        //}
                    }
                    else
                    {
                        DrawHeader(spriteBatch, "STATUS: DECEASED", Color.Red);
                    }
                }
                break;

            case EntityType.Plant:
                if (_selectedIndex >= 0 && _selectedIndex < plants.Length)
                {
                    ref Plant plant = ref plants[_selectedIndex];
                    bool isSamePlant = (plant.Id == _selectedEntityId);
                    if (isSamePlant && plant.IsAlive)
                    {
                        DrawRow(spriteBatch, "ID", $"#{plant.Id}");
                        DrawRow(spriteBatch, "Age", $"{plant.Age:F0} ticks | {plant.Age / VivariumGame.FramesPerSecond:F0} s");
                        DrawProgressBar(spriteBatch, "Energy", plant.Energy, 100f, Color.Green);
                        DrawRow(spriteBatch, "Status", plant.Age < Plant.MaturityAge ? "Growing" : "Mature");
                    }
                    else
                    {
                        DrawHeader(spriteBatch, "STATUS: WITHERED", Color.Red);
                    }
                }
                break;

            case EntityType.Structure:
                if (_selectedIndex >= 0 && _selectedIndex < structures.Length)
                {
                    ref Structure structure = ref structures[_selectedIndex];
                    bool isSameStructure = (structure.Id == _selectedEntityId);
                    if (isSameStructure)
                    {
                        DrawRow(spriteBatch, "ID", $"#{structure.Id}");
                        DrawRow(spriteBatch, "Material", "Stone");
                        DrawRow(spriteBatch, "Durability", "Infinite");
                    }
                    else
                    {
                        DrawHeader(spriteBatch, "STATUS: ERROR", Color.Red);
                    }
                }
                break;
        }
    }

    // --- SELECTION MARKER (World Space) ---
    public void DrawSelectionMarker(SpriteBatch spriteBatch, int cellSize, float totalTime)
    {
        if (!IsEntitySelected) return;

        // 1. Calculate the exact CENTER of the selected cell
        Vector2 cellCenter = new Vector2(
            (SelectedGridPos.X * cellSize) + (cellSize / 2.0f) + HighlightBorderThickness,
            (SelectedGridPos.Y * cellSize) + (cellSize / 2.0f) + HighlightBorderThickness
        );

        // 2. Calculate the pulsed size (current width/height)
        // Sine wave from 0.8 to 1.2
        float pulse = ((float)Math.Sin(totalTime * 10f) * 0.2f) + 1.0f;
        float currentSize = (cellSize * pulse) + (HighlightBorderThickness * 2);

        // 3. Calculate Top-Left position based on Center and Size
        // This ensures it expands equally in all directions
        float halfSize = currentSize / 2.0f;

        Rectangle r = new Rectangle(
            (int)(cellCenter.X - halfSize),
            (int)(cellCenter.Y - halfSize),
            (int)currentSize,
            (int)currentSize
        );

        DrawBorder(spriteBatch, r, HighlightBorderThickness, Color.Cyan);
    }

    // --- UI HELPER METHODS ---

    private void DrawHeader(SpriteBatch sb, string text, Color? color = null)
    {
        sb.DrawString(_font, text, new Vector2(_panelRect.X + Padding, _cursorY), color ?? _headerColor);
        _cursorY += LineHeight + 5;
    }

    private void DrawRow(SpriteBatch sb, string label, string value)
    {
        int leftX = _panelRect.X + Padding;
        int rightX = _panelRect.X + _panelRect.Width - Padding;

        // Draw Label
        sb.DrawString(_font, label, new Vector2(leftX, _cursorY), _labelColor);

        // Measure Value to align right
        Vector2 valSize = _font.MeasureString(value);
        sb.DrawString(_font, value, new Vector2(rightX - valSize.X, _cursorY), _valueColor);

        _cursorY += LineHeight;
    }

    private void DrawSeparator(SpriteBatch sb)
    {
        _cursorY += 5;
        sb.Draw(_pixelTexture, new Rectangle(_panelRect.X + Padding, _cursorY, _panelRect.Width - (Padding * 2), 1), _borderColor);
        _cursorY += 10;
    }

    private void DrawProgressBar(SpriteBatch sb, string label, float value, float max, Color barColor)
    {
        int leftX = _panelRect.X + Padding;
        int rightX = _panelRect.X + _panelRect.Width - Padding;
        int barWidth = 100;
        int barHeight = 12;

        // Draw Label
        sb.DrawString(_font, label, new Vector2(leftX, _cursorY), _labelColor);

        // Draw Bar Background
        Rectangle barBg = new Rectangle(rightX - barWidth, _cursorY + 4, barWidth, barHeight);
        sb.Draw(_pixelTexture, barBg, Color.Black * 0.5f);

        // Draw Bar Fill
        float pct = Math.Clamp(value / max, 0f, 1f);
        Rectangle barFill = new Rectangle(rightX - barWidth, _cursorY + 4, (int)(barWidth * pct), barHeight);
        sb.Draw(_pixelTexture, barFill, barColor);

        _cursorY += LineHeight;
    }

    // Special bar for Neural Values (-1 to 1) or (0 to 1)
    private void DrawBrainBar(SpriteBatch sb, string label, float value, bool isPositiveOnly = false)
    {
        int leftX = _panelRect.X + Padding;
        int rightX = _panelRect.X + _panelRect.Width - Padding;
        int barWidth = 100;
        int barHeight = 10;
        int barY = _cursorY + 5;

        sb.DrawString(_font, label, new Vector2(leftX, _cursorY), _labelColor);

        // Background
        Rectangle bgRect = new Rectangle(rightX - barWidth, barY, barWidth, barHeight);
        sb.Draw(_pixelTexture, bgRect, Color.Black * 0.5f);

        // Center line
        int centerX = bgRect.X + (barWidth / 2);

        if (isPositiveOnly)
        {
            // 0 to 1 (Left to Right)
            float pct = Math.Clamp(value, 0f, 1f);
            sb.Draw(_pixelTexture, new Rectangle(bgRect.X, barY, (int)(barWidth * pct), barHeight), Color.OrangeRed);
        }
        else
        {
            // -1 to 1 (Center outward)
            sb.Draw(_pixelTexture, new Rectangle(centerX, barY - 2, 1, barHeight + 4), Color.Gray); // Center notch

            float valClamped = Math.Clamp(value, -1f, 1f);
            int fillWidth = (int)((barWidth / 2) * Math.Abs(valClamped));

            if (valClamped > 0)
            {
                // Right side (Greenish)
                sb.Draw(_pixelTexture, new Rectangle(centerX, barY, fillWidth, barHeight), Color.Cyan);
            }
            else
            {
                // Left side (Reddish)
                sb.Draw(_pixelTexture, new Rectangle(centerX - fillWidth, barY, fillWidth, barHeight), Color.Magenta);
            }
        }

        _cursorY += LineHeight;
    }

    private void DrawBorder(SpriteBatch sb, Rectangle r, int thickness, Color c)
    {
        sb.Draw(_pixelTexture, new Rectangle(r.X, r.Y, r.Width, thickness), c); // Top
        sb.Draw(_pixelTexture, new Rectangle(r.X, r.Y + r.Height - thickness, r.Width, thickness), c); // Bottom
        sb.Draw(_pixelTexture, new Rectangle(r.X, r.Y, thickness, r.Height), c); // Left
        sb.Draw(_pixelTexture, new Rectangle(r.X + r.Width - thickness, r.Y, thickness, r.Height), c); // Right
    }

    private static float GetActionVal(ref Agent agent, ActionType type)
    {
        int idx = BrainConfig.GetActionIndex(type);
        return agent.NeuronActivations[idx];
    }
}