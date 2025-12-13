using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Vivarium.Entities;
using Vivarium.World;
using Vivarium.Engine;
using Vivarium.Biology;
using static Vivarium.Biology.Genetics;

namespace Vivarium.UI;

public class Inspector
{
    private readonly GraphicsDevice _graphics;
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
    
    // Colors (Modern Palette) - Now using UITheme
    // private readonly Color _panelBgColor = new Color(30, 30, 35, 230); 
    // private readonly Color _borderColor = new Color(60, 60, 70);
    private readonly Color _labelColor = new Color(180, 180, 190);     // Light Grey
    private readonly Color _valueColor = Color.White;
    private readonly Color _headerColor = new Color(100, 200, 255);    // Soft Cyan

    // Deferred Layout List
    private readonly List<IInspectorElement> _elements = new List<IInspectorElement>();

    public Inspector(GraphicsDevice graphics, SpriteFont font)
    {
        _graphics = graphics;
        _font = font;
        _pixelTexture = new Texture2D(graphics, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    public void UpdateInput(Camera2D camera, GridCell[,] gridMap, Agent[] agents, Plant[] plants, Structure[] structures)
    {
        var mouseState = Mouse.GetState();

        // Left Click to Select
        if (mouseState.LeftButton == ButtonState.Pressed)
        {
            // UI Blocking: Don't select world if clicking inside the panel area
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
            }
        }
    }

    public void DrawUI(SpriteBatch spriteBatch, Agent[] agents, Plant[] plants, Structure[] structures)
    {
        if (!IsEntitySelected) return;

        // 1. Build Command List & Calculate Height
        _elements.Clear();
        int contentHeight = UITheme.Padding;

        // --- HEADER ---
        AddHeader($"{SelectedType.ToString().ToUpper()}", ref contentHeight);
        AddSeparator(ref contentHeight);

        // --- CONTENT ---
        AddRow("Grid Pos", $"{SelectedGridPos.X}/{SelectedGridPos.Y}", ref contentHeight);
        AddRow("Index", $"{_selectedIndex}", ref contentHeight);

        AddSeparator(ref contentHeight);

        switch (SelectedType)
        {
            case EntityType.Agent:
                if (_selectedIndex >= 0 && _selectedIndex < agents.Length)
                {
                    ref Agent agent = ref agents[_selectedIndex];
                    bool isSameAgent = (agent.Id == _selectedEntityId);
                    if (isSameAgent && agent.IsAlive)
                    {
                        AddRow("ID", $"#{agent.Id}", ref contentHeight);
                        AddRow("Parent ID", $"#{agent.ParentId}", ref contentHeight);
                        AddRow("Generation", $"{agent.Generation}", ref contentHeight);
                        AddRow("Diet", $"{agent.Diet}", ref contentHeight);
                        AddRow("Age", $"{agent.Age:F0} t | {agent.Age / VivariumGame.FramesPerSecond:F0} s", ref contentHeight);
                        AddProgressBar("Energy", agent.Energy, agent.MaxEnergy, Color.Lerp(UITheme.BadColor, UITheme.GoodColor, agent.Energy / agent.MaxEnergy), ref contentHeight);
                        AddProgressBar("Hunger", agent.Hunger, 100f, Color.Lerp(UITheme.GoodColor, UITheme.BadColor, agent.Hunger / 100f), ref contentHeight);

                        // Cooldowns
                        AddSeparator(ref contentHeight);
                        AddHeader("COOLDOWNS", ref contentHeight);
                        AddProgressBar("Attack", agent.AttackCooldown, 60f, UITheme.WarningColor, ref contentHeight);
                        AddProgressBar("Move", agent.MovementCooldown, 5f, Color.LightBlue, ref contentHeight);
                        AddProgressBar("Breed", agent.ReproductionCooldown, 600f, Color.Pink, ref contentHeight);

                        // Traits
                        AddSeparator(ref contentHeight);
                        AddHeader("TRAITS", ref contentHeight);
                        AddBrainBar(nameof(TraitType.Strength), agent.Strength, false, ref contentHeight);
                        AddBrainBar(nameof(TraitType.Constitution), agent.Constitution, false, ref contentHeight);
                        AddBrainBar(nameof(TraitType.Bravery), agent.Bravery, false, ref contentHeight);
                        AddBrainBar("Metabolism", agent.MetabolicEfficiency, false, ref contentHeight);
                        AddBrainBar(nameof(TraitType.Perception), agent.Perception, false, ref contentHeight);
                        AddBrainBar(nameof(TraitType.Speed), agent.Speed, false, ref contentHeight);
                        AddBrainBar("Carni <-> Herbi", agent.TrophicBias, false, ref contentHeight);

                        // Inputs (Sensors)
                        AddSeparator(ref contentHeight);
                        AddHeader("SENSORY INPUTS", ref contentHeight);
                        AddBrainBar("Oscillator", GetSensorVal(ref agent, SensorType.Oscillator), false, ref contentHeight);
                        AddBrainBar("Rand", GetSensorVal(ref agent, SensorType.Random), true, ref contentHeight);
                        AddBrainBar("Agnt Dens", GetSensorVal(ref agent, SensorType.AgentDensity), true, ref contentHeight);
                        AddBrainBar("Plnt Dens", GetSensorVal(ref agent, SensorType.PlantDensity), true, ref contentHeight);
                        AddBrainBar("Strt Dens", GetSensorVal(ref agent, SensorType.StructureDensity), true, ref contentHeight);

                        // Outputs (Actions)
                        AddSeparator(ref contentHeight);
                        AddHeader("BRAIN ACTIVITY", ref contentHeight);
                        AddBrainBar("Move N", GetActionVal(ref agent, ActionType.MoveN), true, ref contentHeight);
                        AddBrainBar("Move E", GetActionVal(ref agent, ActionType.MoveE), true, ref contentHeight);
                        AddBrainBar("Move S", GetActionVal(ref agent, ActionType.MoveS), true, ref contentHeight);
                        AddBrainBar("Move W", GetActionVal(ref agent, ActionType.MoveW), true, ref contentHeight);
                        AddBrainBar("Attack", GetActionVal(ref agent, ActionType.Attack), true, ref contentHeight);
                        AddBrainBar("Reproduce", GetActionVal(ref agent, ActionType.Reproduce), true, ref contentHeight);
                        AddBrainBar("Flee", GetActionVal(ref agent, ActionType.Flee), true, ref contentHeight);
                        AddBrainBar("Suicide", GetActionVal(ref agent, ActionType.Suicide), true, ref contentHeight);
                    }
                    else
                    {
                        AddHeader("STATUS: DECEASED", ref contentHeight, UITheme.BadColor);
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
                        AddRow("ID", $"#{plant.Id}", ref contentHeight);
                        AddRow("Age", $"{plant.Age:F0} t | {plant.Age / VivariumGame.FramesPerSecond:F0} s", ref contentHeight);
                        AddProgressBar("Energy", plant.Energy, 100f, UITheme.GoodColor, ref contentHeight);
                        AddRow("Status", plant.Age < Plant.MaturityAge ? "Growing" : "Mature", ref contentHeight);
                    }
                    else
                    {
                        AddHeader("STATUS: WITHERED", ref contentHeight, UITheme.BadColor);
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
                        AddRow("ID", $"#{structure.Id}", ref contentHeight);
                        AddRow("Material", "Stone", ref contentHeight);
                        AddRow("Durability", "Infinite", ref contentHeight);
                    }
                    else
                    {
                        AddHeader("STATUS: ERROR", ref contentHeight, UITheme.BadColor);
                    }
                }
                break;
        }

        contentHeight += UITheme.Padding;

        // 2. Set Height & Draw Background
        // Fixed panel position (Top Right), height is dynamic
        _panelRect = new Rectangle(_graphics.Viewport.Width - 20 - 280, 20, 280, contentHeight);

        // Shadow & Bg
        spriteBatch.Draw(_pixelTexture, new Rectangle(_panelRect.X + UITheme.ShadowOffset, _panelRect.Y + UITheme.ShadowOffset, _panelRect.Width, _panelRect.Height), UITheme.ShadowColor);
        spriteBatch.Draw(_pixelTexture, _panelRect, UITheme.PanelBgColor);

        // Border
        DrawBorder(spriteBatch, _panelRect, UITheme.BorderThickness, UITheme.BorderColor);

        // 3. Execute Commands
        _cursorY = _panelRect.Y + UITheme.Padding; // Reset cursor for drawing
        foreach (var cmd in _elements)
        {
            cmd.Draw(this, spriteBatch);
            _cursorY += cmd.Height; // Advance cursor by the element's height
        }
    }

    // --- BUILDER METHODS ---

    private void AddHeader(string text, ref int currentHeight, Color? color = null)
    {
        var e = new HeaderElement(text, color ?? UITheme.HeaderColor);
        _elements.Add(e);
        currentHeight += e.Height;
    }

    private void AddRow(string label, string value, ref int currentHeight)
    {
        var e = new RowElement(label, value);
        _elements.Add(e);
        currentHeight += e.Height;
    }

    private void AddSeparator(ref int currentHeight)
    {
        var e = new SeparatorElement();
        _elements.Add(e);
        currentHeight += e.Height;
    }

    private void AddProgressBar(string label, float value, float max, Color color, ref int currentHeight)
    {
        var e = new ProgressBarElement(label, value, max, color);
        _elements.Add(e);
        currentHeight += e.Height;
    }

    private void AddBrainBar(string label, float value, bool positiveOnly, ref int currentHeight)
    {
        var e = new BrainBarElement(label, value, positiveOnly);
        _elements.Add(e);
        currentHeight += e.Height;
    }


    // --- SELECTION MARKER (World Space) ---
    public void DrawSelectionMarker(SpriteBatch spriteBatch, int cellSize, float totalTime)
    {
        if (!IsEntitySelected) return;

        Vector2 cellCenter = new Vector2(
            (SelectedGridPos.X * cellSize) + (cellSize / 2.0f) + 1,
            (SelectedGridPos.Y * cellSize) + (cellSize / 2.0f) + 1
        );

        float pulse = ((float)Math.Sin(totalTime * 10f) * 0.2f) + 1.0f;
        float currentSize = (cellSize * pulse) + (1 * 2);
        float halfSize = currentSize / 2.0f;

        Rectangle r = new Rectangle(
            (int)(cellCenter.X - halfSize),
            (int)(cellCenter.Y - halfSize),
            (int)currentSize,
            (int)currentSize
        );

        DrawBorder(spriteBatch, r, 1, Color.Gold);
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

    private static float GetSensorVal(ref Agent agent, SensorType type)
    {
        int idx = (int)type;
        return agent.NeuronActivations[idx];
    }

    // --- ELEMENT INTERFACES & STRUCTS (Internal) ---

    private interface IInspectorElement
    {
        int Height { get; }
        void Draw(Inspector context, SpriteBatch sb);
    }

    private readonly struct HeaderElement(string text, Color color) : IInspectorElement
    {
        public int Height => 27; // LineHeight + 5
        public void Draw(Inspector ctx, SpriteBatch sb)
        {
            sb.DrawString(ctx._font, text, new Vector2(ctx._panelRect.X + UITheme.Padding, ctx._cursorY), color);
        }
    }

    private readonly struct RowElement(string label, string value) : IInspectorElement
    {
        public int Height => UITheme.LineHeight;
        public void Draw(Inspector ctx, SpriteBatch sb)
        {
            int leftX = ctx._panelRect.X + UITheme.Padding;
            int rightX = ctx._panelRect.X + ctx._panelRect.Width - UITheme.Padding;
            sb.DrawString(ctx._font, label, new Vector2(leftX, ctx._cursorY), UITheme.TextColorSecondary);
            Vector2 valSize = ctx._font.MeasureString(value);
            sb.DrawString(ctx._font, value, new Vector2(rightX - valSize.X, ctx._cursorY), UITheme.TextColorPrimary);
        }
    }

    private readonly struct SeparatorElement : IInspectorElement
    {
        public int Height => 15; // 5 + 10
        public void Draw(Inspector ctx, SpriteBatch sb)
        {
            // Center roughly
            int y = ctx._cursorY + 5;
            sb.Draw(ctx._pixelTexture, new Rectangle(ctx._panelRect.X + UITheme.Padding, y, ctx._panelRect.Width - (UITheme.Padding * 2), 1), UITheme.BorderColor);
        }
    }

    private readonly struct ProgressBarElement(string label, float value, float max, Color color) : IInspectorElement
    {
        public int Height => UITheme.LineHeight;
        public void Draw(Inspector ctx, SpriteBatch sb)
        {
            int leftX = ctx._panelRect.X + UITheme.Padding;
            int rightX = ctx._panelRect.X + ctx._panelRect.Width - UITheme.Padding;
            int barWidth = 100;
            int barHeight = 12;

            sb.DrawString(ctx._font, label, new Vector2(leftX, ctx._cursorY), UITheme.TextColorSecondary);

            Rectangle barBg = new Rectangle(rightX - barWidth, ctx._cursorY + 4, barWidth, barHeight);
            sb.Draw(ctx._pixelTexture, barBg, Color.Black * 0.5f);

            float pct = Math.Clamp(value / max, 0f, 1f);
            Rectangle barFill = new Rectangle(rightX - barWidth, ctx._cursorY + 4, (int)(barWidth * pct), barHeight);
            sb.Draw(ctx._pixelTexture, barFill, color);
        }
    }

    private readonly struct BrainBarElement(string label, float value, bool positiveOnly) : IInspectorElement
    {
        public int Height => UITheme.LineHeight;
        public void Draw(Inspector ctx, SpriteBatch sb)
        {
            int leftX = ctx._panelRect.X + UITheme.Padding;
            int rightX = ctx._panelRect.X + ctx._panelRect.Width - UITheme.Padding;
            int barWidth = 100;
            int barHeight = 10;
            int barY = ctx._cursorY + 5;

            sb.DrawString(ctx._font, label, new Vector2(leftX, ctx._cursorY), UITheme.TextColorSecondary);

            Rectangle bgRect = new Rectangle(rightX - barWidth, barY, barWidth, barHeight);
            sb.Draw(ctx._pixelTexture, bgRect, Color.Black * 0.5f);

            if (positiveOnly)
            {
                float pct = Math.Clamp(value, 0f, 1f);
                sb.Draw(ctx._pixelTexture, new Rectangle(bgRect.X, barY, (int)(barWidth * pct), barHeight), UITheme.GoodColor);
            }
            else
            {
                int centerX = bgRect.X + (barWidth / 2);
                sb.Draw(ctx._pixelTexture, new Rectangle(centerX, barY - 2, 1, barHeight + 4), Color.Gray);

                float valClamped = Math.Clamp(value, -1f, 1f);
                int fillWidth = (int)((barWidth / 2) * Math.Abs(valClamped));
                if (valClamped > 0)
                {
                    sb.Draw(ctx._pixelTexture, new Rectangle(centerX, barY, fillWidth, barHeight), Color.Cyan);
                }
                else
                {
                    sb.Draw(ctx._pixelTexture, new Rectangle(centerX - fillWidth, barY, fillWidth, barHeight), Color.Magenta);
                }
            }
        }
    }
}
