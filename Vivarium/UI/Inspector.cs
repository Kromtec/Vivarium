using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Text;
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
    private int _selectedIndex; // Index in the respective array

    // UI Layout
    private Rectangle _uiBounds;

    public Inspector(GraphicsDevice graphics, SpriteFont font)
    {
        _font = font;

        // Create a simple 1x1 white texture for drawing the UI background box
        _pixelTexture = new Texture2D(graphics, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        // Define UI Panel size (Top Left corner)
        _uiBounds = new Rectangle(10, 10, 300, 400);
    }

    public void UpdateInput(Camera2D camera, GridCell[,] gridMap)
    {
        var mouseState = Mouse.GetState();

        // Check for Left Click to Select
        // Ensure we only click inside the game world, not on the UI (optional check)
        if (mouseState.LeftButton == ButtonState.Pressed)
        {
            // 1. Convert Screen Coordinates (Mouse) to World Coordinates (Grid)
            Vector2 mouseScreen = new Vector2(mouseState.X, mouseState.Y);
            Vector2 mouseWorld = camera.ScreenToWorld(mouseScreen);

            // 2. Convert World Coordinates to Grid Index
            // Assuming CellSize is global or passed in. Let's assume 10 for now, 
            // but ideally pass 'cellSize' or calculate it.
            // CAREFUL: You need access to CellSize here. Let's pass it or assume standard.
            // Better: Pass calculated grid coordinates from Game class or calculate here if CellSize is known.
            int cellSize = 10; // TODO: Pass this from outside or make it a const in a Config class

            int gx = (int)(mouseWorld.X / cellSize);
            int gy = (int)(mouseWorld.Y / cellSize);

            int w = gridMap.GetLength(0);
            int h = gridMap.GetLength(1);

            // Check Bounds
            if (gx >= 0 && gx < w && gy >= 0 && gy < h)
            {
                var cell = gridMap[gx, gy];

                if (cell.Type != EntityType.Empty)
                {
                    // Select!
                    IsEntitySelected = true;
                    SelectedGridPos = new Point(gx, gy);
                    SelectedType = cell.Type;
                    _selectedIndex = cell.Index;
                }
                else
                {
                    // Deselect if clicking empty space
                    IsEntitySelected = false;
                }
            }
        }
    }

    public void DrawUI(SpriteBatch spriteBatch, Agent[] agents, Plant[] plants, Structure[] structures)
    {
        if (!IsEntitySelected) return;

        // Draw Background Panel (Semi-transparent black)
        spriteBatch.Draw(_pixelTexture, _uiBounds, Color.Black * 0.7f);

        // Prepare Info Text
        StringBuilder text = new StringBuilder();

        text.AppendLine($"--- INSPECTOR ---");
        text.AppendLine($"Pos: {SelectedGridPos.X}, {SelectedGridPos.Y}");
        text.AppendLine($"Type: {SelectedType}");

        switch (SelectedType)
        {
            case EntityType.Agent:
                if (_selectedIndex >= 0 && _selectedIndex < agents.Length)
                {
                    ref Agent agent = ref agents[_selectedIndex];
                    if (agent.IsAlive)
                    {
                        text.AppendLine($"ID: {_selectedIndex}");
                        text.AppendLine($"Gen: {agent.Generation}");
                        text.AppendLine($"Age: {agent.Age:F0}");
                        text.AppendLine($"Energy: {agent.Energy:F1}");

                        text.AppendLine("--- BRAIN ---");
                        // Let's show the output neurons to see what it WANTS to do
                        text.AppendLine($"Mv North: {GetActionVal(ref agent, ActionType.MoveNorth):F2}");
                        text.AppendLine($"Mv South: {GetActionVal(ref agent, ActionType.MoveSouth):F2}");
                        text.AppendLine($"Mv East:  {GetActionVal(ref agent, ActionType.MoveEast):F2}");
                        text.AppendLine($"Mv West:  {GetActionVal(ref agent, ActionType.MoveWest):F2}");
                        text.AppendLine($"Attack:   {GetActionVal(ref agent, ActionType.Attack):F2}");
                    }
                    else
                    {
                        text.AppendLine("(DEAD)");
                    }
                }
                break;

            case EntityType.Plant:
                if (_selectedIndex >= 0 && _selectedIndex < plants.Length)
                {
                    ref Plant plant = ref plants[_selectedIndex];
                    text.AppendLine($"Energy: {plant.Energy:F1}");
                    text.AppendLine(plant.IsAlive ? "(Growing)" : "(Dead)");
                }
                break;

            case EntityType.Structure:
                text.AppendLine($"ID: {_selectedIndex}");
                text.AppendLine("Immovable Object.");
                break;
        }

        // Draw Text
        spriteBatch.DrawString(_font, text, new Vector2(_uiBounds.X + 10, _uiBounds.Y + 10), Color.White);
    }

    // Helper to peek into the brain
    private float GetActionVal(ref Agent agent, ActionType type)
    {
        int idx = BrainConfig.GetActionIndex(type);
        return agent.NeuronActivations[idx];
    }

    // Optional: Draw a marker in World Space around the selected entity
    public void DrawSelectionMarker(SpriteBatch spriteBatch, int cellSize)
    {
        if (!IsEntitySelected) return;

        Vector2 pos = new Vector2(
            SelectedGridPos.X * cellSize,
            SelectedGridPos.Y * cellSize
        );

        // Draw a hollow rectangle (4 lines) using the pixel texture
        int size = cellSize;
        int borderThickness = 2;

        Color highlightColor = Color.LightGoldenrodYellow;

        // Top
        spriteBatch.Draw(_pixelTexture, new Rectangle((int)pos.X, (int)pos.Y, size, borderThickness), highlightColor);
        // Bottom
        spriteBatch.Draw(_pixelTexture, new Rectangle((int)pos.X, (int)pos.Y + size - borderThickness, size, borderThickness), highlightColor);
        // Left
        spriteBatch.Draw(_pixelTexture, new Rectangle((int)pos.X, (int)pos.Y, borderThickness, size), highlightColor);
        // Right
        spriteBatch.Draw(_pixelTexture, new Rectangle((int)pos.X + size - borderThickness, (int)pos.Y, borderThickness, size), highlightColor);
    }
}