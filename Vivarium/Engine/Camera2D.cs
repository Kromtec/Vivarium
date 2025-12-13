using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Vivarium.Engine;

public class Camera2D
{
    private readonly GraphicsDevice _graphicsDevice;

    public Vector2 Position { get; set; }

    public float Zoom { get; set; } = 1.0f;

    public float MinZoom { get; set; } = 0.1f;
    public float MaxZoom { get; set; } = 5.0f;

    private Vector2 _lastMousePosition;
    private bool _isDragging;

    private int _previousScrollValue;

    public Camera2D(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        Position = Vector2.Zero;
    }

    public Matrix GetTransformation()
    {
        var viewport = _graphicsDevice.Viewport;

        return Matrix.CreateTranslation(new Vector3(-Position.X, -Position.Y, 0)) *
               Matrix.CreateScale(Zoom) *
               Matrix.CreateTranslation(new Vector3(viewport.Width * 0.5f, viewport.Height * 0.5f, 0));
    }

    public Vector2 ScreenToWorld(Vector2 screenPos)
    {
        return Vector2.Transform(screenPos, Matrix.Invert(GetTransformation()));
    }

    public void HandleInput(MouseState mouseState, KeyboardState keyboardState, bool processInput = true, Rectangle? worldBounds = null)
    {
        int scrollDelta = mouseState.ScrollWheelValue - _previousScrollValue;
        _previousScrollValue = mouseState.ScrollWheelValue;

        if (processInput)
        {
            if (scrollDelta != 0)
            {
                Zoom += scrollDelta * 0.001f * Zoom;
                Zoom = MathHelper.Clamp(Zoom, MinZoom, MaxZoom);
            }

            Vector2 currentMousePos = new Vector2(mouseState.X, mouseState.Y);

            if (mouseState.RightButton == ButtonState.Pressed || mouseState.MiddleButton == ButtonState.Pressed)
            {
                if (!_isDragging)
                {
                    _isDragging = true;
                    _lastMousePosition = currentMousePos;
                }
                else
                {
                    Vector2 delta = _lastMousePosition - currentMousePos;

                    Position += delta / Zoom;

                    _lastMousePosition = currentMousePos;
                }
            }
            else
            {
                _isDragging = false;
            }
        }
        else
        {
            _isDragging = false;
        }

        float speed = 10f / Zoom;
        if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up)) Position -= new Vector2(0, speed);
        if (keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down)) Position += new Vector2(0, speed);
        if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left)) Position -= new Vector2(speed, 0);
        if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right)) Position += new Vector2(speed, 0);

        // Wrap Position if world bounds are provided
        if (worldBounds.HasValue)
        {
            float w = worldBounds.Value.Width;
            float h = worldBounds.Value.Height;

            // Wrap X
            while (Position.X < 0) Position = new Vector2(Position.X + w, Position.Y);
            while (Position.X >= w) Position = new Vector2(Position.X - w, Position.Y);

            // Wrap Y
            while (Position.Y < 0) Position = new Vector2(Position.X, Position.Y + h);
            while (Position.Y >= h) Position = new Vector2(Position.X, Position.Y - h);
        }
    }

    public void CenterOnGrid(int gridWidth, int gridHeight, int cellSize)
    {
        float worldWidth = gridWidth * cellSize;
        float worldHeight = gridHeight * cellSize;

        Position = new Vector2(worldWidth / 2f, worldHeight / 2f);
    }
}