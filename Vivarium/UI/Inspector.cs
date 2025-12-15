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
    public long SelectedEntityId => _selectedEntityId; // Expose ID
    public int SelectedIndex => _selectedIndex; // Expose Index
    private long _selectedEntityId = -1;
    private int _selectedIndex;

    // Layout Settings
    private Rectangle _panelRect;
    private int _cursorY;

    // Deferred Layout List
    private readonly List<IInspectorElement> _elements = new List<IInspectorElement>();

    // Cached Genome Texture
    private Texture2D _cachedGenomeTexture;
    private long _cachedGenomeAgentId = -1;

    private readonly GenomeCensus _census; // Use shared census

    public Inspector(GraphicsDevice graphics, SpriteFont font, GenomeCensus census)
    {
        _graphics = graphics;
        _font = font;
        _census = census;
        _pixelTexture = new Texture2D(graphics, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    public void Deselect()
    {
        IsEntitySelected = false;
        _selectedEntityId = -1;
    }

    public void UpdateInput(Camera2D camera, GridCell[,] gridMap, Agent[] agents, Plant[] plants, Structure[] structures, int cellSize)
    {
        var mouseState = Mouse.GetState();

        // Left Click to Select
        if (mouseState.LeftButton == ButtonState.Pressed)
        {
            // UI Blocking: Don't select world if clicking inside the panel area
            if (IsEntitySelected && _panelRect.Contains(mouseState.Position)) return;

            Vector2 mouseWorld = camera.ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));

            // --- 1. VISUAL AGENT SELECTION (Hit Test against interpolated positions) ---
            bool agentFound = false;
            float halfCell = cellSize * 0.5f;
            float selectionRadiusSq = (halfCell * 1.2f) * (halfCell * 1.2f); // Slightly larger than radius for easier clicking

            // Iterate all agents to find if we clicked one visually
            // (Optimization: In a huge world, we should only check agents in the view, but for now this is fine)
            for (int i = 0; i < agents.Length; i++)
            {
                ref Agent agent = ref agents[i];
                if (!agent.IsAlive) continue;

                // Calculate Interpolated Position
                int dx = agent.X - agent.LastX;
                int dy = agent.Y - agent.LastY;

                // Handle Wrapping
                if (dx < -1) dx = 1;
                if (dx > 1) dx = -1;
                if (dy < -1) dy = 1;
                if (dy > 1) dy = -1;

                float offsetX = 0;
                float offsetY = 0;

                if (agent.MovementCooldown > 0 && agent.TotalMovementCooldown > 0)
                {
                    float t = (float)agent.MovementCooldown / agent.TotalMovementCooldown;
                    offsetX = -(dx * cellSize * t);
                    offsetY = -(dy * cellSize * t);
                }

                Vector2 visualPos = new Vector2(
                    (agent.X * cellSize + halfCell) + offsetX,
                    (agent.Y * cellSize + halfCell) + offsetY
                );

                // Check distance
                if (Vector2.DistanceSquared(mouseWorld, visualPos) < selectionRadiusSq)
                {
                    // FOUND IT!
                    IsEntitySelected = true;
                    SelectedGridPos = new Point(agent.X, agent.Y); // Logical position
                    SelectedType = EntityType.Agent;
                    _selectedIndex = i;
                    _selectedEntityId = agent.Id;
                    agentFound = true;
                    break; // Stop after first hit
                }
            }

            if (agentFound) return; // Skip grid check if we hit an agent

            // --- 2. FALLBACK GRID SELECTION (Plants, Structures, Stationary Agents) ---

            // Use Floor to handle negative coordinates correctly
            int gx = (int)Math.Floor(mouseWorld.X / cellSize);
            int gy = (int)Math.Floor(mouseWorld.Y / cellSize);

            int w = gridMap.GetLength(0);
            int h = gridMap.GetLength(1);

            // Wrap coordinates for infinite world
            gx %= w;
            if (gx < 0) gx += w;

            gy %= h;
            if (gy < 0) gy += h;

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

        // Build Command List
        _elements.Clear();
        int contentHeight = UITheme.Padding;

        // Header
        AddHeader($"{SelectedType.ToString().ToUpper()}", ref contentHeight);
        AddSeparator(ref contentHeight);

        // Content
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
                        // Genome
                        UpdateCachedGenomeTexture(agent);
                        if (_cachedGenomeTexture != null)
                        {
                            AddTexture(_cachedGenomeTexture, 128, 64, ref contentHeight);
                        }

                        // Scientific Name & Variant
                        var (ScientificName, Translation) = ScientificNameGenerator.GenerateFamilyName(agent);
                        string variantName = _census.GetVariantName(Genetics.CalculateGenomeHash(agent.Genome));

                        AddRow("Species", "", ref contentHeight);
                        string scientificName = ScientificName;
                        if (!string.IsNullOrEmpty(variantName))
                        {
                            scientificName = $"{scientificName} {variantName}";
                        }
                        AddRow("", scientificName, ref contentHeight);
                        AddRow("Meaning", "", ref contentHeight);
                        AddRow("", $"\"{Translation}\"", ref contentHeight);
                        AddSeparator(ref contentHeight);

                        AddRow("ID", $"#{agent.Id}", ref contentHeight);
                        if (agent.ParentId != -1)
                        {
                            AddRow("Parent ID", $"#{agent.ParentId}", ref contentHeight);
                        }

                        AddRow("Generation", $"{agent.Generation}", ref contentHeight);
                        AddRow("Diet", $"{agent.Diet}", ref contentHeight);

                        TimeSpan simTime = TimeSpan.FromSeconds(agent.Age / VivariumGame.FramesPerSecond);
                        string timeString = $"{simTime:hh\\:mm\\:ss}";
                        AddRow("Age", timeString, ref contentHeight);

                        AddProgressBar("Energy", agent.Energy, agent.MaxEnergy, Color.Lerp(UITheme.BadColor, UITheme.GoodColor, agent.Energy / agent.MaxEnergy), ref contentHeight);

                        // Traits
                        AddSeparator(ref contentHeight);
                        AddHeader("TRAITS", ref contentHeight);
                        AddBrainBar(nameof(TraitType.Strength), agent.Strength, false, ref contentHeight);
                        AddBrainBar(nameof(TraitType.Bravery), agent.Bravery, false, ref contentHeight);
                        AddBrainBar("Metabolism", agent.MetabolicEfficiency, false, ref contentHeight);
                        AddBrainBar(nameof(TraitType.Perception), agent.Perception, false, ref contentHeight);
                        AddBrainBar(nameof(TraitType.Speed), agent.Speed, false, ref contentHeight);
                        AddBrainBar("Trophic Bias", agent.TrophicBias, false, ref contentHeight);
                        AddBrainBar(nameof(TraitType.Constitution), agent.Constitution, false, ref contentHeight);

                        // Sensors
                        AddSeparator(ref contentHeight);
                        AddHeader("SENSORY INPUTS", ref contentHeight);
                        AddBrainBar("Oscillator", GetSensorVal(ref agent, SensorType.Oscillator), false, ref contentHeight);
                        AddBrainBar("Random", GetSensorVal(ref agent, SensorType.Random), true, ref contentHeight);
                        AddBrainBar("Agent Density", GetSensorVal(ref agent, SensorType.AgentDensity), true, ref contentHeight);
                        AddBrainBar("Plant Density", GetSensorVal(ref agent, SensorType.PlantDensity), true, ref contentHeight);
                        AddBrainBar("Structure Density", GetSensorVal(ref agent, SensorType.StructureDensity), true, ref contentHeight);

                        // Actions
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

                        // Cooldowns
                        AddSeparator(ref contentHeight);
                        AddHeader("COOLDOWNS", ref contentHeight);
                        AddProgressBar("Attack", agent.AttackCooldown, 60f, UITheme.WarningColor, ref contentHeight);
                        AddProgressBar("Move", agent.MovementCooldown, 5f, UITheme.CooldownMoveColor, ref contentHeight);
                        AddProgressBar("Breed", agent.ReproductionCooldown, 600f, UITheme.CooldownBreedColor, ref contentHeight);
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

        // Draw Background
        int panelWidth = 380;
        _panelRect = new Rectangle(_graphics.Viewport.Width - 20 - panelWidth, 20, panelWidth, contentHeight);

        UIComponents.DrawPanel(spriteBatch, _panelRect, _pixelTexture);

        // Execute Commands
        _cursorY = _panelRect.Y + UITheme.Padding;
        foreach (var cmd in _elements)
        {
            cmd.Draw(this, spriteBatch);
            _cursorY += cmd.Height;
        }
    }

    // --- BUILDER METHODS ---

    private void UpdateCachedGenomeTexture(Agent agent)
    {
        if (_cachedGenomeAgentId != agent.Id)
        {
            // Dispose old texture if it exists
            _cachedGenomeTexture?.Dispose();

            // Generate new texture
            _cachedGenomeTexture = GenomeHelper.GenerateHelixTexture(_graphics, agent);
            _cachedGenomeAgentId = agent.Id;
        }
    }

    private void AddTexture(Texture2D texture, int width, int height, ref int currentY)
    {
        _elements.Add(new TextureElement(texture, width, height, currentY));
        currentY += height + UITheme.Padding;
    }

    private void AddHeader(string text, ref int currentY, Color? color = null)
    {
        var e = new HeaderElement(text, color ?? UITheme.HeaderColor);
        _elements.Add(e);
        currentY += e.Height;
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
        void Draw(Inspector inspector, SpriteBatch sb);
    }

    private class TextureElement : IInspectorElement
    {
        private readonly Texture2D _texture;
        private readonly int _width;
        private readonly int _height;
        private readonly int _y;

        public int Height => _height + UITheme.Padding;

        public TextureElement(Texture2D texture, int width, int height, int y)
        {
            _texture = texture;
            _width = width;
            _height = height;
            _y = y;
        }

        public void Draw(Inspector inspector, SpriteBatch sb)
        {
            int x = inspector._panelRect.X + (inspector._panelRect.Width - _width) / 2; // Center
            int y = inspector._cursorY;

            // Pixel art scaling
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);

            sb.Draw(_texture, new Rectangle(x, y, _width, _height), Color.White);

            sb.End();
            sb.Begin();
        }
    }

    private class HeaderElement : IInspectorElement
    {
        private readonly string _text;
        private readonly Color _color;

        public HeaderElement(string text, Color color)
        {
            _text = text;
            _color = color;
        }

        public int Height => 27; // LineHeight + 5

        public void Draw(Inspector inspector, SpriteBatch sb)
        {
            sb.DrawString(inspector._font, _text, new Vector2(inspector._panelRect.X + UITheme.Padding, inspector._cursorY), _color);
        }
    }

    private class RowElement : IInspectorElement
    {
        private readonly string _label;
        private readonly string _value;

        public RowElement(String label, string value)
        {
            _label = label;
            _value = value;
        }

        public int Height => UITheme.LineHeight;

        public void Draw(Inspector inspector, SpriteBatch sb)
        {
            int leftX = inspector._panelRect.X + UITheme.Padding;
            int rightX = inspector._panelRect.X + inspector._panelRect.Width - UITheme.Padding;
            sb.DrawString(inspector._font, _label, new Vector2(leftX, inspector._cursorY), UITheme.TextColorSecondary);
            Vector2 valSize = inspector._font.MeasureString(_value);
            sb.DrawString(inspector._font, _value, new Vector2(rightX - valSize.X, inspector._cursorY), UITheme.TextColorPrimary);
        }
    }

    private class SeparatorElement : IInspectorElement
    {
        public int Height => 15; // 5 + 10
        public void Draw(Inspector inspector, SpriteBatch sb)
        {
            // Center roughly
            int y = inspector._cursorY + 5;
            sb.Draw(inspector._pixelTexture, new Rectangle(inspector._panelRect.X + UITheme.Padding, y, inspector._panelRect.Width - (UITheme.Padding * 2), 1), UITheme.BorderColor);
        }
    }

    private class ProgressBarElement : IInspectorElement
    {
        private readonly string _label;
        private readonly float _value;
        private readonly float _max;
        private readonly Color _color;

        public ProgressBarElement(string label, float value, float max, Color color)
        {
            _label = label;
            _value = value;
            _max = max;
            _color = color;
        }

        public int Height => UITheme.LineHeight;

        public void Draw(Inspector inspector, SpriteBatch sb)
        {
            int leftX = inspector._panelRect.X + UITheme.Padding;
            int rightX = inspector._panelRect.X + inspector._panelRect.Width - UITheme.Padding;
            int barWidth = 150; // Increased from 100
            int barX = rightX - barWidth;

            sb.DrawString(inspector._font, _label, new Vector2(leftX, inspector._cursorY), UITheme.TextColorSecondary);

            // Use UIComponents
            float ratio = _value / _max;
            UIComponents.DrawSimpleProgressBar(sb, new Rectangle(barX, inspector._cursorY + 5, barWidth, 10), ratio, _color, inspector._pixelTexture);
        }
    }

    private class BrainBarElement : IInspectorElement
    {
        private readonly string _label;
        private readonly float _value;
        private readonly bool _positiveOnly;

        public BrainBarElement(string label, float value, bool positiveOnly)
        {
            _label = label;
            _value = value;
            _positiveOnly = positiveOnly;
        }

        public int Height => UITheme.LineHeight;

        public void Draw(Inspector inspector, SpriteBatch sb)
        {
            int leftX = inspector._panelRect.X + UITheme.Padding;
            int rightX = inspector._panelRect.X + inspector._panelRect.Width - UITheme.Padding;
            int barWidth = 150; // Increased from 100
            int barX = rightX - barWidth;

            sb.DrawString(inspector._font, _label, new Vector2(leftX, inspector._cursorY), UITheme.TextColorSecondary);

            // Use UIComponents
            UIComponents.DrawBrainBar(sb, new Rectangle(barX, inspector._cursorY + 5, barWidth, 10), _value, _positiveOnly, inspector._pixelTexture);
        }
    }
}
