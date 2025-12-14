using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Vivarium.Biology;
using Vivarium.Engine;
using Vivarium.Entities;
using Vivarium.UI;
using Vivarium.World;

namespace Vivarium.Visuals;

public class WorldRenderer
{
    private readonly GraphicsDevice _graphicsDevice;
    private SpriteBatch _spriteBatch;

    // Textures
    private Texture2D _pixelTexture;
    private Texture2D _circleTexture;
    private Texture2D _starTexture;
    private Texture2D _roundedRectTexture;
    private Texture2D _arrowTexture;
    private Texture2D _dotTexture;
    private Texture2D _ringTexture;
    private Texture2D _selectionRingTexture;
    private Texture2D[] _structureTextures; // Array for 16 variations
    private Texture2D[] _plantTextures; // Array for plant variations

    public WorldRenderer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent()
    {
        _spriteBatch = new SpriteBatch(_graphicsDevice);
        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData([Color.White]);

        _circleTexture = TextureGenerator.CreateCircle(_graphicsDevice, 50);
        _starTexture = TextureGenerator.CreateStar(_graphicsDevice, 50, 5);
        _roundedRectTexture = TextureGenerator.CreateRoundedRect(_graphicsDevice, 50, 20, 5);
        _arrowTexture = TextureGenerator.CreateTriangle(_graphicsDevice, 32);
        _dotTexture = TextureGenerator.CreateCircle(_graphicsDevice, 16);
        _ringTexture = TextureGenerator.CreateRing(_graphicsDevice, 64, 8);
        _selectionRingTexture = TextureGenerator.CreateRing(_graphicsDevice, 64, 16); // Thicker ring for selection

        // Generate Structure Textures (16 variations)
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
        int plantBorder = 5; // Increased thickness to prevent broken edges at small scales
        _plantTextures[0] = TextureGenerator.CreateOrganicShape(_graphicsDevice, 64, 5, 0.2f, plantBorder); // Standard Flower
        _plantTextures[1] = TextureGenerator.CreateOrganicShape(_graphicsDevice, 64, 3, 0.15f, plantBorder); // Triangle/Tulip
        _plantTextures[2] = TextureGenerator.CreateOrganicShape(_graphicsDevice, 64, 6, 0.25f, plantBorder); // Complex Flower
        _plantTextures[3] = TextureGenerator.CreateOrganicShape(_graphicsDevice, 64, 4, 0.2f, plantBorder); // Clover
    }

    public RenderStats Draw(
        GameTime gameTime,
        Camera2D camera,
        GridCell[,] gridMap,
        Agent[] agents,
        Plant[] plants,
        Structure[] structures,
        Inspector inspector,
        int cellSize)
    {
        int gridWidth = gridMap.GetLength(0);
        int gridHeight = gridMap.GetLength(1);
        int worldWidth = gridWidth * cellSize;
        int worldHeight = gridHeight * cellSize;

        // Calculate visible area in world coordinates
        Vector2 topLeft = camera.ScreenToWorld(Vector2.Zero);
        Vector2 bottomRight = camera.ScreenToWorld(new Vector2(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height));
        Rectangle viewRect = new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)(bottomRight.X - topLeft.X), (int)(bottomRight.Y - topLeft.Y));

        // Determine which copies of the world we need to draw
        int[] offsets = { -1, 0, 1 };

        RenderStats stats = new RenderStats();
        bool statsCaptured = false;

        foreach (int ox in offsets)
        {
            foreach (int oy in offsets)
            {
                // Calculate the position of this world copy
                int worldOffsetX = ox * worldWidth;
                int worldOffsetY = oy * worldHeight;

                Rectangle worldRect = new Rectangle(worldOffsetX, worldOffsetY, worldWidth, worldHeight);

                if (viewRect.Intersects(worldRect))
                {
                    // Create a transform that shifts the drawing to this copy's position
                    Matrix transform = Matrix.CreateTranslation(worldOffsetX, worldOffsetY, 0) * camera.GetTransformation();

                    _spriteBatch.Begin(
                        SpriteSortMode.Deferred,
                        BlendState.NonPremultiplied,
                        SamplerState.LinearClamp,
                        null,
                        null,
                        null,
                        transform
                    );

                    if (!statsCaptured)
                    {
                        DrawStructures(structures, gridMap, cellSize, out stats.LivingStructures);
                        DrawPlants(plants, cellSize, out stats.LivingPlants);
                        DrawKinshipLines(gridMap, agents, inspector, cellSize);
                        DrawAgents(agents, cellSize, out stats.LivingAgents, out stats.LivingHerbivores, out stats.LivingOmnivores, out stats.LivingCarnivores);
                        statsCaptured = true;
                    }
                    else
                    {
                        DrawStructures(structures, gridMap, cellSize, out _);
                        DrawPlants(plants, cellSize, out _);
                        DrawKinshipLines(gridMap, agents, inspector, cellSize);
                        DrawAgents(agents, cellSize, out _, out _, out _, out _);
                    }

                    // Draw the Selection Box inside the world
                    float totalSeconds = (float)gameTime.TotalGameTime.TotalSeconds;
                    DrawSelectionMarker(_spriteBatch, inspector, cellSize, totalSeconds);

                    _spriteBatch.End();
                }
            }
        }

        return stats;
    }

    private void DrawSelectionMarker(SpriteBatch spriteBatch, Inspector inspector, int cellSize, float totalTime)
    {
        if (!inspector.IsEntitySelected) return;

        var gridPos = inspector.SelectedGridPos;

        // Calculate center of the selected cell
        Vector2 cellCenter = new Vector2(
            (gridPos.X * cellSize) + (cellSize / 2.0f),
            (gridPos.Y * cellSize) + (cellSize / 2.0f)
        );

        // Pulsating effect similar to ReproductionVisual
        // Use a sine wave to oscillate between 0 and 1
        float pulse = (MathF.Sin(totalTime * 5f) + 1.0f) * 0.5f;

        // Scale: Grows from 2x to 4x the cell size (Bigger)
        float scale = ((float)cellSize / _selectionRingTexture.Width) * (2f + (pulse * 2f));

        // Alpha: Fades out as it grows (1.0 -> 0.2)
        float alpha = 1.0f - (pulse * 0.8f);

        var ringCenter = new Vector2(_selectionRingTexture.Width / 2f, _selectionRingTexture.Height / 2f);

        spriteBatch.Draw(
            _selectionRingTexture,
            cellCenter,
            null,
            Color.Gold * alpha,
            0f,
            ringCenter,
            scale,
            SpriteEffects.None,
            0f
        );
    }

    private void DrawStructures(Structure[] structures, GridCell[,] gridMap, int cellSize, out int livingStructures)
    {
        var textureCenter = new Vector2(_structureTextures[0].Width / 2f, _structureTextures[0].Height / 2f);
        float halfCellSize = cellSize * 0.5f;
        Span<Structure> structurePopulationSpan = structures.AsSpan();
        livingStructures = structurePopulationSpan.Length;
        
        int gridWidth = gridMap.GetLength(0);
        int gridHeight = gridMap.GetLength(1);

        for (int i = 0; i < structurePopulationSpan.Length; i++)
        {
            ref Structure structure = ref structurePopulationSpan[i];

            // Determine neighbors
            int neighbors = 0;
            int x = structure.X;
            int y = structure.Y;

            // Top (0,-1)
            int ty = (y - 1 + gridHeight) % gridHeight;
            if (gridMap[x, ty].Type == EntityType.Structure) neighbors |= 1;

            // Right (1,0)
            int rx = (x + 1) % gridWidth;
            if (gridMap[rx, y].Type == EntityType.Structure) neighbors |= 2;

            // Bottom (0,1)
            int by = (y + 1) % gridHeight;
            if (gridMap[x, by].Type == EntityType.Structure) neighbors |= 4;

            // Left (-1,0)
            int lx = (x - 1 + gridWidth) % gridWidth;
            if (gridMap[lx, y].Type == EntityType.Structure) neighbors |= 8;

            Texture2D texture = _structureTextures[neighbors];
            float structScale = ((float)cellSize / texture.Width);

            // Calculate screen position
            Vector2 position = new Vector2(
                structure.X * cellSize + halfCellSize,
                structure.Y * cellSize + halfCellSize
            );
            _spriteBatch.Draw(
                texture,
                position,
                null,
                structure.Color,
                0f,
                textureCenter,
                structScale,
                SpriteEffects.None,
                0f
            );
        }
    }

    private void DrawPlants(Plant[] plants, int cellSize, out int livingPlants)
    {
        livingPlants = 0;
        // We assume all plant textures are same size (64x64)
        var textureCenter = new Vector2(_plantTextures[0].Width / 2f, _plantTextures[0].Height / 2f);
        float baseScale = (float)cellSize / _plantTextures[0].Width;
        float halfCellSize = cellSize * 0.5f;

        const float plantAgeGrowthFactor = 1.0f / Plant.MaturityAge;

        Span<Plant> plantPopulationSpan = plants.AsSpan();
        for (int i = 0; i < plantPopulationSpan.Length; i++)
        {
            ref Plant plant = ref plantPopulationSpan[i];
            if (!plant.IsAlive)
            {
                continue;
            }

            livingPlants++;

            // --- GROWTH LOGIC ---
            float ageRatio = Math.Min(plant.Age * plantAgeGrowthFactor, 1.0f);

            // Linear interpolation: Start at 30% size, end at 100% size
            float finalScale = baseScale * (0.3f + (0.7f * ageRatio));

            // Calculate screen position
            Vector2 position = new Vector2(
                plant.X * cellSize + halfCellSize,
                plant.Y * cellSize + halfCellSize
            );

            // Select variant based on index (deterministic variation)
            int variant = plant.Index % _plantTextures.Length;

            _spriteBatch.Draw(
                _plantTextures[variant],
                position,
                null,
                plant.Color,
                0f,
                textureCenter, // Draw from the center of the texture!
                finalScale,
                SpriteEffects.None,
                0f
            );
        }
    }

    private void DrawAgents(Agent[] agents, int cellSize, out int livingAgents, out int livingHerbivore, out int livingOmnivore, out int livingCarnivore)
    {
        livingAgents = 0;
        livingHerbivore = 0;
        livingOmnivore = 0;
        livingCarnivore = 0;

        // Calculate the center of our source texture (needed for pivot point)
        var textureCenter = new Vector2(_circleTexture.Width / 2f, _circleTexture.Height / 2f);
        var arrowCenter = new Vector2(_arrowTexture.Width / 2f, _arrowTexture.Height / 2f);
        var dotCenter = new Vector2(_dotTexture.Width / 2f, _dotTexture.Height / 2f);
        var ringCenter = new Vector2(_ringTexture.Width / 2f, _ringTexture.Height / 2f);

        float baseScale = (float)cellSize / _circleTexture.Width;
        float arrowScale = ((float)cellSize / _arrowTexture.Width) * 0.6f; // Slightly smaller than cell
        float dotScale = ((float)cellSize / _dotTexture.Width) * 0.4f; // Small dot
        float ringBaseScale = (float)cellSize / _ringTexture.Width;
        float halfCellSize = cellSize * 0.5f;

        const float agentAgeGrowthFactor = 1.0f / Agent.MaturityAge;

        Span<Agent> agentPopulationSpan = agents.AsSpan();

        // PASS 1: Draw Agent Bodies (Bottom Layer)
        for (int i = 0; i < agentPopulationSpan.Length; i++)
        {
            ref Agent agent = ref agentPopulationSpan[i];
            if (!agent.IsAlive)
            {
                continue;
            }

            livingAgents++;
            switch (agent.Diet)
            {
                case DietType.Herbivore:
                    livingHerbivore++;
                    break;
                case DietType.Omnivore:
                    livingOmnivore++;
                    break;
                case DietType.Carnivore:
                    livingCarnivore++;
                    break;
            }

            // --- GROWTH LOGIC ---
            float ageRatio = Math.Min(agent.Age * agentAgeGrowthFactor, 1.0f);
            float growthFactor = 0.3f + (0.7f * ageRatio);

            // Linear interpolation: Start at 30% size, end at 100% size
            float finalScale = baseScale * growthFactor;

            // Calculate screen position
            Vector2 position = new Vector2(
                agent.X * cellSize + halfCellSize,
                agent.Y * cellSize + halfCellSize
            );

            _spriteBatch.Draw(
                _circleTexture,
                position,
                null,
                agent.Color,
                0f,
                textureCenter, // Draw from the center of the texture!
                finalScale,
                SpriteEffects.None,
                0f
            );
        }

        // PASS 2: Draw Visual Effects (Top Layer)
        for (int i = 0; i < agentPopulationSpan.Length; i++)
        {
            ref Agent agent = ref agentPopulationSpan[i];
            if (!agent.IsAlive) continue;

            // Re-calculate position/growth for effects
            float ageRatio = Math.Min(agent.Age * agentAgeGrowthFactor, 1.0f);
            float growthFactor = 0.3f + (0.7f * ageRatio);

            Vector2 position = new Vector2(
                agent.X * cellSize + halfCellSize,
                agent.Y * cellSize + halfCellSize
            );

            float indicatorOffset = (cellSize * 0.85f) * growthFactor;
            float indicatorScaleFactor = 0.65f + (0.35f * ageRatio);

            // --- ATTACK VISUALIZATION ---
            if (agent.AttackVisualTimer > 0)
            {
                float alpha = agent.AttackVisualTimer / 15f; // Fade out
                float rotation = MathF.Atan2(agent.LastAttackDirY, agent.LastAttackDirX);

                Vector2 dir = new Vector2(agent.LastAttackDirX, agent.LastAttackDirY);
                if (dir != Vector2.Zero) dir.Normalize();

                Vector2 offset = dir * indicatorOffset;

                _spriteBatch.Draw(
                    _arrowTexture,
                    position + offset,
                    null,
                    agent.Color * alpha,
                    rotation,
                    arrowCenter,
                    arrowScale * indicatorScaleFactor,
                    SpriteEffects.None,
                    0f
                );
            }

            // --- FLEE VISUALIZATION ---
            if (agent.FleeVisualTimer > 0)
            {
                float alpha = agent.FleeVisualTimer / 15f; // Fade out

                Vector2 dir = new Vector2(agent.LastFleeDirX, agent.LastFleeDirY);
                if (dir != Vector2.Zero) dir.Normalize();

                Vector2 offset = dir * indicatorOffset;

                _spriteBatch.Draw(
                    _dotTexture,
                    position + offset,
                    null,
                    agent.Color * alpha, // Use agent color
                    0f,
                    dotCenter,
                    dotScale * indicatorScaleFactor,
                    SpriteEffects.None,
                    0f
                );
            }

            // --- REPRODUCTION VISUALIZATION ---
            if (agent.ReproductionVisualTimer > 0)
            {
                float t = 1.0f - (agent.ReproductionVisualTimer / 30f); // 0 to 1 over time
                float alpha = 1.0f - t; // Fade out

                float scale = ringBaseScale * (0.8f + (t * 2.2f));

                _spriteBatch.Draw(
                    _ringTexture,
                    position,
                    null,
                    Color.White * alpha,
                    0f,
                    ringCenter,
                    scale,
                    SpriteEffects.None,
                    0f
                );
            }
        }
    }

    private void DrawKinshipLines(GridCell[,] gridMap, Agent[] agents, Inspector inspector, int cellSize)
    {
        if (!inspector.IsEntitySelected || inspector.SelectedType != EntityType.Agent) return;

        var gridPos = inspector.SelectedGridPos;
        var cell = gridMap[gridPos.X, gridPos.Y];

        if (cell.Type != EntityType.Agent) return;

        ref Agent selectedAgent = ref agents[cell.Index];
        if (!selectedAgent.IsAlive) return;

        const int BorderThickness = 2;
        const float agentAgeGrowthFactor = 1.0f / Agent.MaturityAge;
        float halfCellSize = cellSize * 0.5f;

        // Calculate center and radius for selected agent
        Vector2 selectedCenter = new Vector2(
            (selectedAgent.X * cellSize) + halfCellSize,
            (selectedAgent.Y * cellSize) + halfCellSize
        );

        float selectedAgeRatio = Math.Min(selectedAgent.Age * agentAgeGrowthFactor, 1.0f);
        float selectedGrowth = 0.3f + (0.7f * selectedAgeRatio);
        float selectedRadius = (cellSize * selectedGrowth) * 0.5f;

        Span<Agent> agentPopulationSpan = agents.AsSpan();
        for (int i = 0; i < agentPopulationSpan.Length; i++)
        {
            ref Agent other = ref agentPopulationSpan[i];
            if (!other.IsAlive || i == cell.Index) continue;

            // Check Kinship
            if (selectedAgent.IsDirectlyRelatedTo(ref other))
            {
                Vector2 otherCenter = new Vector2(
                    (other.X * cellSize) + halfCellSize,
                    (other.Y * cellSize) + halfCellSize
                );

                float otherAgeRatio = Math.Min(other.Age * agentAgeGrowthFactor, 1.0f);
                float otherGrowth = 0.3f + (0.7f * otherAgeRatio);
                float otherRadius = (cellSize * otherGrowth) * 0.5f;

                // Calculate direction and distance
                Vector2 direction = otherCenter - selectedCenter;
                float distance = direction.Length();

                if (distance > 0.001f)
                {
                    direction /= distance; // Normalize

                    // Calculate start and end points at the hull
                    // Clamp offsets to avoid crossing if agents overlap
                    float startOffset = Math.Min(selectedRadius, distance * 0.5f);
                    float endOffset = Math.Min(otherRadius, distance * 0.5f);

                    Vector2 lineStart = selectedCenter + (direction * startOffset);
                    Vector2 lineEnd = otherCenter - (direction * endOffset);

                    // Draw Line
                    DrawLine(lineStart, lineEnd, selectedAgent.OriginalColor * 0.5f, BorderThickness);
                }
            }
        }
    }

    private void DrawLine(Vector2 start, Vector2 end, Color color, int thickness)
    {
        Vector2 edge = end - start;
        float angle = MathF.Atan2(edge.Y, edge.X);
        float length = edge.Length();

        _spriteBatch.Draw(
            _pixelTexture,
            start,
            null,
            color,
            angle,
            new Vector2(0, 0.5f), // Origin at middle-left of the 1x1 pixel for vertical centering
            new Vector2(length, thickness),
            SpriteEffects.None,
            0f
        );
    }
}
