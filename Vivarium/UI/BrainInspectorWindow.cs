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

public class BrainInspectorWindow
{
    private readonly GraphicsDevice _graphics;
    private readonly SpriteFont _font;
    private readonly Texture2D _pixelTexture;
    private readonly Texture2D _circleTexture;

    public bool IsVisible { get; set; }
    private Rectangle _windowRect;
    private Agent _targetAgent;
    private bool _needsLayoutUpdate = false;

    // Dropdown State
    private bool _isDropdownOpen = false;
    private int _selectedActionIndex = -1; // -1 = ALL
    private string[] _actionOptions;
    private Rectangle _dropdownRect;

    // Graph Data
    private class Node
    {
        public int Index;
        public List<int> AggregatedIndices; // For cluster
        public Vector2 Position;
        public string Label;
        public float Activation;
        public bool IsActive; // Has connections
        public NodeType Type;
        public Rectangle Bounds; // For cluster sizing
    }

    private enum NodeType { Sensor, Action, Hidden, HiddenCluster }

    private class Connection
    {
        public Node Source;
        public Node Sink;
        public float Weight;
        public bool IsActive; // Source is active
    }

    private List<Node> _nodes = new();
    private List<Connection> _connections = new();

    // Layout Constants
    private const int NodeRadius = 8;
    private const int Padding = 20;
    private const int HeaderHeight = 70;

    public BrainInspectorWindow(GraphicsDevice graphics, SpriteFont font)
    {
        _graphics = graphics;
        _font = font;
        _pixelTexture = new Texture2D(graphics, 1, 1);
        _pixelTexture.SetData([Color.White]);
        _circleTexture = TextureGenerator.CreateCircle(graphics, NodeRadius);

        // Initialize Action Options
        var actions = Enum.GetNames<ActionType>();
        _actionOptions = new string[actions.Length + 1];
        _actionOptions[0] = "ALL ACTIONS";
        Array.Copy(actions, 0, _actionOptions, 1, actions.Length);
    }

    public void SetTarget(Agent agent)
    {
        _targetAgent = agent;
        _needsLayoutUpdate = true;
        IsVisible = true;
        _isDropdownOpen = false; // Reset dropdown
    }

    public void UpdateInput(MouseState mouseState, MouseState prevMouseState, ref bool isPaused, ref bool singleStep)
    {
        if (!IsVisible) return;

        var keyboard = Keyboard.GetState();

        // Ensure Layout/Rects are valid
        int screenW = _graphics.Viewport.Width;
        int screenH = _graphics.Viewport.Height;
        int winW = (int)(screenW * 0.9f);
        int winH = (int)(screenH * 0.9f);
        
        Rectangle newRect = new Rectangle((screenW - winW) / 2, (screenH - winH) / 2, winW, winH);
        if (_windowRect != newRect)
        {
            _windowRect = newRect;
            _needsLayoutUpdate = true;
        }

        // Update Dropdown Rect
        _dropdownRect = new Rectangle(_windowRect.Right - 200 - 60, _windowRect.Y + 15, 200, 25);

        // Close Button Logic
        Rectangle closeRect = new Rectangle(_windowRect.Right - 30, _windowRect.Y + 10, 20, 20);
        if (closeRect.Contains(mouseState.Position) && 
            mouseState.LeftButton == ButtonState.Pressed && 
            prevMouseState.LeftButton == ButtonState.Released)
        {
            IsVisible = false;
            return;
        }

        // Dropdown Logic
        if (_isDropdownOpen)
        {
            // Calculate list rect
            int itemHeight = 25;
            int listHeight = itemHeight * _actionOptions.Length;
            Rectangle listRect = new Rectangle(_dropdownRect.X, _dropdownRect.Bottom, _dropdownRect.Width, listHeight);

            if (listRect.Contains(mouseState.Position))
            {
                if (mouseState.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton == ButtonState.Released)
                {
                    int index = (mouseState.Y - listRect.Y) / itemHeight;
                    if (index >= 0 && index < _actionOptions.Length)
                    {
                        _selectedActionIndex = index - 1; // -1 for ALL
                        _isDropdownOpen = false;
                        _needsLayoutUpdate = true;
                    }
                }
                return; // Consume input
            }
            else if (mouseState.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton == ButtonState.Released && !_dropdownRect.Contains(mouseState.Position))
            {
                _isDropdownOpen = false; // Click outside closes
            }
        }

        if (_dropdownRect.Contains(mouseState.Position) && 
            mouseState.LeftButton == ButtonState.Pressed && 
            prevMouseState.LeftButton == ButtonState.Released && 
            !_isDropdownOpen)
        {
            _isDropdownOpen = true;
            return; // Consume input
        }

        // Close Button Logic (handled in Draw usually, but simple check here)
        // Or ESC key
        if (keyboard.IsKeyDown(Keys.Escape))
        {
            IsVisible = false;
        }

        // Pause/Step Control
        // If window is visible, we enforce pause unless stepping
        if (!isPaused)
        {
            isPaused = true;
        }

        // Step with Space or Period
        // We need a debouncer or just rely on the game's existing input handling if we pass it through?
        // The prompt says: "we should have the abilty to step tick by tick, like we can in the paused game itself"
        // VivariumGame handles 'OemPeriod' for single step. We just need to make sure we don't block it.
        // But we might want a button in this UI too.
    }

    public void UpdateLayout()
    {
        if (_targetAgent.Genome == null) return;

        _nodes.Clear();
        _connections.Clear();

        int width = _windowRect.Width;
        int height = _windowRect.Height - HeaderHeight;
        int startY = _windowRect.Y + HeaderHeight;
        int startX = _windowRect.X;
        int availableHeight = height - Padding * 2;
        int laneWidth = width / 3;

        // Helper to decode genes exactly like Brain.Think
        int validSourceCount = BrainConfig.SensorCount + BrainConfig.HiddenCount;
        int validSinkCount = BrainConfig.ActionCount + BrainConfig.HiddenCount;

        (int source, int sink) DecodeGene(Gene gene)
        {
            // SOURCE RESTRICTION: Sensors or Hidden only.
            int rawSource = gene.SourceId % validSourceCount;
            int sourceIdx = (rawSource < BrainConfig.SensorCount) 
                ? rawSource 
                : rawSource + BrainConfig.ActionCount;

            // SINK RESTRICTION: Actions or Hidden only.
            int sinkIdx = (gene.SinkId % validSinkCount) + BrainConfig.ActionsStart;

            return (sourceIdx, sinkIdx);
        }

        // 1. Identify Active Nodes (Filtered)
        HashSet<int> activeIndices = new();
        
        if (_selectedActionIndex == -1)
        {
            // Show ALL active connections
            foreach (var gene in _targetAgent.Genome)
            {
                var (sourceIdx, sinkIdx) = DecodeGene(gene);
                
                activeIndices.Add(sourceIdx);
                activeIndices.Add(sinkIdx);
            }
        }
        else
        {
            // Filter by Selected Action
            int targetActionIdx = BrainConfig.ActionsStart + _selectedActionIndex;
            activeIndices.Add(targetActionIdx); // Always show the target action

            // Backwards Traversal
            Queue<int> nodesToProcess = new();
            nodesToProcess.Enqueue(targetActionIdx);
            HashSet<int> visited = new();
            visited.Add(targetActionIdx);

            while (nodesToProcess.Count > 0)
            {
                int currentSink = nodesToProcess.Dequeue();

                foreach (var gene in _targetAgent.Genome)
                {
                    var (sourceIdx, sinkIdx) = DecodeGene(gene);
                    
                    if (sinkIdx == currentSink)
                    {
                        activeIndices.Add(sourceIdx);
                        activeIndices.Add(sinkIdx); // Ensure sink is marked active

                        // If source is Hidden, we need to trace it back further
                        // Sensors don't need tracing (they are leaves in backwards traversal)
                        if (sourceIdx >= BrainConfig.HiddenStart && !visited.Contains(sourceIdx))
                        {
                            visited.Add(sourceIdx);
                            nodesToProcess.Enqueue(sourceIdx);
                        }
                    }
                }
            }
        }

        // 2. Create Nodes
        // Sensors (Left Column)
        int sensorCount = BrainConfig.SensorCount;
        int activeSensors = 0;
        float maxLabelWidth = 0f;

        // First pass: Count active sensors and find max label width
        for (int i = 0; i < sensorCount; i++)
        {
            if (activeIndices.Contains(i))
            {
                activeSensors++;
                string label = ((SensorType)i).ToString();
                Vector2 size = _font.MeasureString(label);
                if (size.X > maxLabelWidth) maxLabelWidth = size.X;
            }
        }

        // Add some padding to the label width
        float sensorXOffset = maxLabelWidth + Padding;

        // Calculate vertical spacing and centering
        float maxSpacing = 40f;
        float sensorSpacing = Math.Min(maxSpacing, (float)availableHeight / Math.Max(1, activeSensors));
        float totalSensorHeight = sensorSpacing * Math.Max(0, activeSensors - 1);
        float sensorStartY = startY + Padding + (availableHeight - totalSensorHeight) / 2f;

        int currentSensor = 0;
        for (int i = 0; i < sensorCount; i++)
        {
            if (activeIndices.Contains(i))
            {
                float y = sensorStartY + currentSensor * sensorSpacing;
                _nodes.Add(new Node
                {
                    Index = i,
                    Position = new Vector2(startX + laneWidth / 2, y),
                    Label = ((SensorType)i).ToString(),
                    Type = NodeType.Sensor,
                    IsActive = true
                });
                currentSensor++;
            }
        }

        // Actions (Right Column)
        int actionStart = BrainConfig.ActionsStart;
        int actionCount = BrainConfig.ActionCount;
        int activeActions = 0;
        for (int i = actionStart; i < actionStart + actionCount; i++)
        {
            if (activeIndices.Contains(i)) activeActions++;
        }

        // Calculate vertical spacing and centering for Actions
        float actionSpacing = Math.Min(maxSpacing, (float)availableHeight / Math.Max(1, activeActions));
        float totalActionHeight = actionSpacing * Math.Max(0, activeActions - 1);
        float actionStartY = startY + Padding + (availableHeight - totalActionHeight) / 2f;

        int currentAction = 0;
        for (int i = actionStart; i < actionStart + actionCount; i++)
        {
            if (activeIndices.Contains(i))
            {
                float y = actionStartY + currentAction * actionSpacing;
                _nodes.Add(new Node
                {
                    Index = i,
                    Position = new Vector2(startX + width - laneWidth / 2, y),
                    Label = ((ActionType)(i - actionStart)).ToString(),
                    Type = NodeType.Action,
                    IsActive = true
                });
                currentAction++;
            }
        }

        // Hidden (Center)
        // If filtering is active, use Blackbox (HiddenCluster)
        // Otherwise use standard nodes
        
        int hiddenStart = BrainConfig.HiddenStart;
        List<int> activeHiddenIndices = new();
        for (int i = hiddenStart; i < BrainConfig.NeuronCount; i++)
        {
            if (activeIndices.Contains(i)) activeHiddenIndices.Add(i);
        }

        // ALWAYS use Blackbox if there are hidden neurons
        if (activeHiddenIndices.Count > 0)
        {
            // BLACKBOX MODE
            float midX = startX + width / 2f;
            float midY = startY + height / 2f;
            int boxSize = 100;
            
            var clusterNode = new Node
            {
                Index = -1,
                AggregatedIndices = activeHiddenIndices,
                Position = new Vector2(midX, midY),
                Label = "HIDDEN LAYER",
                Type = NodeType.HiddenCluster,
                IsActive = true,
                Bounds = new Rectangle((int)(midX - boxSize/2), (int)(midY - boxSize/2), boxSize, boxSize)
            };
            _nodes.Add(clusterNode);
        }

        // 3. Create Connections
        foreach (var gene in _targetAgent.Genome)
        {
            var (sourceIdx, sinkIdx) = DecodeGene(gene);

            // Only add connection if both nodes are in our active set
            if (activeIndices.Contains(sourceIdx) && activeIndices.Contains(sinkIdx))
            {
                // Find nodes. If Blackbox mode, remap hidden indices to the Cluster Node.
                Node sourceNode = null;
                Node sinkNode = null;

                if (sourceIdx >= BrainConfig.HiddenStart)
                {
                    sourceNode = _nodes.FirstOrDefault(n => n.Type == NodeType.HiddenCluster);
                }
                else
                {
                    sourceNode = _nodes.FirstOrDefault(n => n.Index == sourceIdx);
                }

                if (sinkIdx >= BrainConfig.HiddenStart)
                {
                    sinkNode = _nodes.FirstOrDefault(n => n.Type == NodeType.HiddenCluster);
                }
                else
                {
                    sinkNode = _nodes.FirstOrDefault(n => n.Index == sinkIdx);
                }

                // Avoid self-loops on the cluster
                if (sourceNode != null && sinkNode != null && sourceNode != sinkNode)
                {
                    _connections.Add(new Connection
                    {
                        Source = sourceNode,
                        Sink = sinkNode,
                        Weight = gene.Weight
                    });
                }
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible) return;

        // Center Window
        int screenW = _graphics.Viewport.Width;
        int screenH = _graphics.Viewport.Height;
        int winW = (int)(screenW * 0.9f);
        int winH = (int)(screenH * 0.9f);
        
        Rectangle newRect = new Rectangle((screenW - winW) / 2, (screenH - winH) / 2, winW, winH);
        if (_windowRect != newRect)
        {
            _windowRect = newRect;
            _needsLayoutUpdate = true;
        }

        // Update Dropdown Rect
        _dropdownRect = new Rectangle(_windowRect.Right - 200 - 60, _windowRect.Y + 15, 200, 25);

        if (_needsLayoutUpdate)
        {
            UpdateLayout();
            _needsLayoutUpdate = false;
        }

        // Update Activations
        if (_targetAgent.NeuronActivations != null)
        {
            foreach (var node in _nodes)
            {
                if (node.Index >= 0 && node.Index < _targetAgent.NeuronActivations.Length)
                {
                    node.Activation = _targetAgent.NeuronActivations[node.Index];
                }
            }
        }

        // Draw Background
        UIComponents.DrawPanel(spriteBatch, _windowRect, _pixelTexture);

        // Draw Lanes
        int laneWidth = _windowRect.Width / 3;
        int laneTop = _windowRect.Y + 40;
        int laneHeight = _windowRect.Height - 50; // Leave some bottom padding

        // Lane 1 (Sensors) - Darker
        Rectangle lane1 = new Rectangle(_windowRect.X + 5, laneTop, laneWidth - 5, laneHeight);
        spriteBatch.Draw(_pixelTexture, lane1, Color.Gainsboro * 0.05f);

        // Lane 2 (Hidden) - Lighter
        Rectangle lane2 = new Rectangle(_windowRect.X + laneWidth, laneTop, laneWidth, laneHeight);
        //spriteBatch.Draw(_pixelTexture, lane2, Color.Thistle * 0.05f);

        // Lane 3 (Actions) - Darker
        Rectangle lane3 = new Rectangle(_windowRect.X + laneWidth * 2, laneTop, laneWidth - 5, laneHeight);
        spriteBatch.Draw(_pixelTexture, lane3, Color.Gainsboro * 0.05f);

        // Header
        spriteBatch.DrawString(_font, $"NEURAL NETWORK INSPECTOR - AGENT #{_targetAgent.Id}", new Vector2(_windowRect.X + Padding, _windowRect.Y + Padding / 2), UITheme.HeaderColor);

        // Column Titles
        float titleY = laneTop + 10;
        
        Vector2 sensorSize = _font.MeasureString("SENSORS");
        spriteBatch.DrawString(_font, "SENSORS", new Vector2(lane1.Center.X - sensorSize.X / 2, titleY), Color.LightGray);
        
        Vector2 hiddenSize = _font.MeasureString("HIDDEN NEURONS");
        spriteBatch.DrawString(_font, "HIDDEN NEURONS", new Vector2(lane2.Center.X - hiddenSize.X / 2, titleY), Color.LightGray);

        Vector2 actionSize = _font.MeasureString("ACTIONS");
        spriteBatch.DrawString(_font, "ACTIONS", new Vector2(lane3.Center.X - actionSize.X / 2, titleY), Color.LightGray);

        // Dropdown
        var mouseState = Mouse.GetState();
        bool ddHover = _dropdownRect.Contains(mouseState.Position);
        string currentText = _selectedActionIndex == -1 ? "ALL ACTIONS" : ((ActionType)_selectedActionIndex).ToString();
        
        UIComponents.DrawDropdown(spriteBatch, _font, _dropdownRect, currentText, _isDropdownOpen, _pixelTexture, ddHover);

        // Draw Connections
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        foreach (var conn in _connections)
        {
            // Color based on weight
            Color color = conn.Weight > 0 ? UITheme.GoodColor : UITheme.BadColor;
            
            // Opacity based on Source Activation (The "Flow")
            // We map activation -1..1 to 0..1 magnitude for visibility?
            // Or just absolute value?
            // If source is 0, line should be faint.
            float flow = Math.Abs(conn.Source.Activation);
            float alpha = 0.2f + (flow * 0.8f);
            
            DrawLine(spriteBatch, conn.Source.Position, conn.Sink.Position, color * alpha, 1 + (flow * 2));
        }

        // Draw Nodes
        foreach (var node in _nodes)
        {
            if (node.Type == NodeType.HiddenCluster)
            {
                // Draw Blackbox
                UIComponents.DrawPanel(spriteBatch, node.Bounds, _pixelTexture);
                
                // Calculate Stats
                int pos = 0, neg = 0, neu = 0;
                if (_targetAgent.NeuronActivations != null && node.AggregatedIndices != null)
                {
                    foreach (int idx in node.AggregatedIndices)
                    {
                        float val = _targetAgent.NeuronActivations[idx];
                        if (val > 0.1f) pos++;
                        else if (val < -0.1f) neg++;
                        else neu++;
                    }
                }

                // Draw Symbols
                int startX = node.Bounds.X + 10;
                int startY = node.Bounds.Y + 10;
                int spacing = 25;

                // Green
                spriteBatch.Draw(_circleTexture, new Vector2(startX, startY), null, UITheme.GoodColor, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_font, $"{pos}", new Vector2(startX + 20, startY - 5), Color.White);

                // Gray
                spriteBatch.Draw(_circleTexture, new Vector2(startX, startY + spacing), null, Color.Gray, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_font, $"{neu}", new Vector2(startX + 20, startY + spacing - 5), Color.White);

                // Red
                spriteBatch.Draw(_circleTexture, new Vector2(startX, startY + spacing * 2), null, UITheme.BadColor, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_font, $"{neg}", new Vector2(startX + 20, startY + spacing * 2 - 5), Color.White);

                continue;
            }

            // Color based on Activation
            // -1 (Red) .. 0 (Black/Gray) .. 1 (Green)
            Color nodeColor = Color.Gray;
            if (node.Activation > 0.1f) nodeColor = Color.Lerp(Color.Gray, Color.Lime, node.Activation);
            else if (node.Activation < -0.1f) nodeColor = Color.Lerp(Color.Gray, Color.Red, -node.Activation);

            Vector2 origin = new Vector2(NodeRadius, NodeRadius);
            spriteBatch.Draw(_circleTexture, node.Position, null, nodeColor, 0f, origin, 1f, SpriteEffects.None, 0f);

            // Label
            if (node.Type != NodeType.Hidden)
            {
                Vector2 size = _font.MeasureString(node.Label);
                Vector2 labelPos = node.Position;
                if (node.Type == NodeType.Sensor) labelPos.X -= size.X + NodeRadius + 5;
                else labelPos.X += NodeRadius + 5;
                
                labelPos.Y -= size.Y / 2;
                spriteBatch.DrawString(_font, node.Label, labelPos, Color.White);
            }
        }

        // Close Button
        Rectangle closeRect = new Rectangle(_windowRect.Right - 30, _windowRect.Y + 10, 20, 20);
        bool hover = closeRect.Contains(mouseState.Position);
        // Click handled in UpdateInput
        UIComponents.DrawButton(spriteBatch, _font, closeRect, "X", _pixelTexture, hover, false, UITheme.BadColor);
        
        // Draw Dropdown List Overlay (Last to be on top)
        if (_isDropdownOpen)
        {
            int itemHeight = 25;
            int listHeight = itemHeight * _actionOptions.Length;
            Rectangle listRect = new Rectangle(_dropdownRect.X, _dropdownRect.Bottom, _dropdownRect.Width, listHeight);
            
            int hoveredIndex = -1;
            if (listRect.Contains(mouseState.Position))
            {
                hoveredIndex = (mouseState.Y - listRect.Y) / itemHeight;
            }

            UIComponents.DrawDropdownList(spriteBatch, _font, listRect, _actionOptions, hoveredIndex, _pixelTexture);
        }
    }

    private void DrawLine(SpriteBatch sb, Vector2 start, Vector2 end, Color color, float thickness)
    {
        Vector2 edge = end - start;
        float angle = (float)Math.Atan2(edge.Y, edge.X);
        float length = edge.Length();

        sb.Draw(_pixelTexture, start, null, color, angle, Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0);
    }
}
