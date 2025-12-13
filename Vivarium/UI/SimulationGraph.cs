using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Vivarium.UI;

public class SimulationGraph
{
    private readonly Queue<float> _agentHistory = new();
    private readonly Queue<float> _plantHistory = new();
    private readonly int _maxHistoryPoints = 200;

    private Texture2D _pixelTexture;
    private Rectangle _bounds;
    private float _timer;
    private const float UpdateInterval = 0.1f;

    public SimulationGraph(GraphicsDevice graphicsDevice, SpriteFont font)
    {
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    public void SetBounds(Rectangle bounds)
    {
        _bounds = bounds;
    }

    public void Update(GameTime gameTime, int agentCount, int plantCount)
    {
        _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_timer >= UpdateInterval)
        {
            _timer = 0;

            _agentHistory.Enqueue(agentCount);
            _plantHistory.Enqueue(plantCount);

            if (_agentHistory.Count > _maxHistoryPoints) _agentHistory.Dequeue();
            if (_plantHistory.Count > _maxHistoryPoints) _plantHistory.Dequeue();
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Draw Graph Background (Darker slot)
        spriteBatch.Draw(_pixelTexture, _bounds, Color.Black * 0.3f);

        if (_agentHistory.Count < 2) return;

        float maxVal = Math.Max(_agentHistory.Max(), _plantHistory.Max());
        if (maxVal < 10) maxVal = 10;

        DrawSeries(spriteBatch, _plantHistory, Vivarium.Visuals.VivariumColors.Plant, maxVal);
        DrawSeries(spriteBatch, _agentHistory, Vivarium.Visuals.VivariumColors.Agent, maxVal);

        // Draw Axes
        DrawLine(spriteBatch, new Vector2(_bounds.Left, _bounds.Top), new Vector2(_bounds.Left, _bounds.Bottom), UITheme.BorderColor);
        DrawLine(spriteBatch, new Vector2(_bounds.Left, _bounds.Bottom), new Vector2(_bounds.Right, _bounds.Bottom), UITheme.BorderColor);
    }

    private void DrawSeries(SpriteBatch spriteBatch, Queue<float> data, Color color, float maxValue)
    {
        var points = data.ToArray();
        float stepX = (float)_bounds.Width / (_maxHistoryPoints - 1);

        for (int i = 0; i < points.Length - 1; i++)
        {
            float x1 = _bounds.X + (i * stepX);
            float y1 = _bounds.Bottom - (points[i] / maxValue * _bounds.Height);

            float x2 = _bounds.X + ((i + 1) * stepX);
            float y2 = _bounds.Bottom - (points[i + 1] / maxValue * _bounds.Height);

            DrawLine(spriteBatch, new Vector2(x1, y1), new Vector2(x2, y2), color, 2f);
        }
    }

    private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness = 1f)
    {
        Vector2 edge = end - start;
        float angle = (float)Math.Atan2(edge.Y, edge.X);
        float length = edge.Length();

        spriteBatch.Draw(_pixelTexture, start, null, color, angle, Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0);
    }
}