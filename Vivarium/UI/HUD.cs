using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Vivarium.Visuals;

namespace Vivarium.UI;

public class HUD
{
    private readonly GraphicsDevice _graphics;
    private readonly SpriteFont _font;
    private readonly Texture2D _pixelTexture;
    private readonly SimulationGraph _simGraph;
    private readonly GenePoolWindow _genePoolWindow;

    private Rectangle _panelRect;
    private int _cursorY;
    private Rectangle _geneButtonRect;
    private bool _wasLeftButtonPressed = false;

    public Rectangle Bounds => _panelRect;

    public HUD(GraphicsDevice graphics, SpriteFont font, SimulationGraph simGraph, GenePoolWindow genePoolWindow)
    {
        _graphics = graphics;
        _font = font;
        _simGraph = simGraph;
        _genePoolWindow = genePoolWindow;

        _pixelTexture = new Texture2D(graphics, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    public void UpdateInput()
    {
        var mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();
        bool isPressed = mouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;

        if (isPressed && !_wasLeftButtonPressed)
        {
            if (_geneButtonRect.Contains(mouse.Position))
            {
                _genePoolWindow.IsVisible = !_genePoolWindow.IsVisible;
            }
        }

        _wasLeftButtonPressed = isPressed;
    }

    public bool IsMouseOver(Point mousePos)
    {
        return _panelRect.Contains(mousePos) || _geneButtonRect.Contains(mousePos);
    }

    public void Draw(SpriteBatch spriteBatch, long tickCount, int agents, int herbs, int omnis, int carnis, int plants, int structures)
    {
        // 1. Calculate Layout
        int width = 300;
        int startX = 20;
        int startY = 20;
        int contentHeight = UITheme.Padding;

        int graphHeight = 100;
        int graphPadding = 10;
        
        // Header
        contentHeight += 30;
        contentHeight += UITheme.LineHeight;
        
        // Graph
        contentHeight += graphPadding;
        contentHeight += graphHeight;
        contentHeight += graphPadding;
        
        // Stats
        contentHeight += 30;
        contentHeight += UITheme.LineHeight * 6;
        contentHeight += UITheme.Padding;

        _panelRect = new Rectangle(startX, startY, width, contentHeight);

        // 2. Draw Background
        spriteBatch.Draw(_pixelTexture, new Rectangle(_panelRect.X + UITheme.ShadowOffset, _panelRect.Y + UITheme.ShadowOffset, _panelRect.Width, _panelRect.Height), UITheme.ShadowColor);
        spriteBatch.Draw(_pixelTexture, _panelRect, UITheme.PanelBgColor);
        DrawBorder(spriteBatch, _panelRect, UITheme.BorderThickness, UITheme.BorderColor);

        // 3. Draw Content
        _cursorY = _panelRect.Y + UITheme.Padding;
        int leftX = _panelRect.X + UITheme.Padding;
        int rightX = _panelRect.X + _panelRect.Width - UITheme.Padding;

        // -- TITLE --
        spriteBatch.DrawString(_font, "SIMULATION STATUS", new Vector2(leftX, _cursorY), UITheme.HeaderColor);
        _cursorY += 30;

        // -- TIME --
        System.TimeSpan simTime = System.TimeSpan.FromSeconds(tickCount / VivariumGame.FramesPerSecond);
        string timeString = $"{simTime:hh\\:mm\\:ss}";
        string tickString = $"T: {tickCount}";
        
        spriteBatch.DrawString(_font, "Time Elapsed", new Vector2(leftX, _cursorY), UITheme.TextColorSecondary);
        Vector2 timeSize = _font.MeasureString(timeString);
        spriteBatch.DrawString(_font, timeString, new Vector2(rightX - timeSize.X, _cursorY), UITheme.TextColorPrimary);
        _cursorY += UITheme.LineHeight;

        // -- GRAPH --
        _cursorY += graphPadding;
        Rectangle graphBounds = new Rectangle(leftX, _cursorY, _panelRect.Width - (UITheme.Padding * 2), graphHeight);
        _simGraph.SetBounds(graphBounds);
        _simGraph.Draw(spriteBatch);
        _cursorY += graphHeight + graphPadding;

        // -- POPULATION STATS --
        _cursorY += 5;
        spriteBatch.DrawString(_font, "POPULATION", new Vector2(leftX, _cursorY + 3), UITheme.HeaderColor);
        
        // Gene Button
        int buttonWidth = 60;
        int buttonHeight = 24;
        
        Vector2 headerSize = _font.MeasureString("POPULATION");
        int buttonY = _cursorY + (int)((headerSize.Y - buttonHeight) / 2);
        
        _geneButtonRect = new Rectangle(rightX - buttonWidth, buttonY, buttonWidth, buttonHeight);
        
        spriteBatch.Draw(_pixelTexture, _geneButtonRect, UITheme.ButtonColor);
        DrawBorder(spriteBatch, _geneButtonRect, 1, UITheme.BorderColor);
        
        Vector2 btnTextSize = _font.MeasureString("GENES");
        Vector2 btnTextPos = new Vector2(
            _geneButtonRect.X + (_geneButtonRect.Width - btnTextSize.X) / 2,
            _geneButtonRect.Y + (_geneButtonRect.Height - btnTextSize.Y) / 2 + 3
        );
        spriteBatch.DrawString(_font, "GENES", btnTextPos, Color.White);

        _cursorY += 30;

        DrawStatRow(spriteBatch, "Total Agents", agents.ToString(), VivariumColors.Agent, leftX, rightX);
        DrawStatRow(spriteBatch, "  Herbivores", herbs.ToString(), VivariumColors.Herbivore, leftX, rightX);
        DrawStatRow(spriteBatch, "  Omnivores", omnis.ToString(), VivariumColors.Omnivore, leftX, rightX);
        DrawStatRow(spriteBatch, "  Carnivores", carnis.ToString(), VivariumColors.Carnivore, leftX, rightX);
        DrawStatRow(spriteBatch, "Plants", plants.ToString(), VivariumColors.Plant, leftX, rightX);
        DrawStatRow(spriteBatch, "Structures", structures.ToString(), VivariumColors.Structure, leftX, rightX);
    }

    private void DrawStatRow(SpriteBatch sb, string label, string value, Color valueColor, int leftX, int rightX)
    {
        sb.DrawString(_font, label, new Vector2(leftX, _cursorY), UITheme.TextColorSecondary);
        Vector2 valSize = _font.MeasureString(value);
        sb.DrawString(_font, value, new Vector2(rightX - valSize.X, _cursorY), valueColor);
        _cursorY += UITheme.LineHeight;
    }

    private void DrawBorder(SpriteBatch sb, Rectangle r, int thickness, Color c)
    {
        sb.Draw(_pixelTexture, new Rectangle(r.X, r.Y, r.Width, thickness), c); // Top
        sb.Draw(_pixelTexture, new Rectangle(r.X, r.Y + r.Height - thickness, r.Width, thickness), c); // Bottom
        sb.Draw(_pixelTexture, new Rectangle(r.X, r.Y, thickness, r.Height), c); // Left
        sb.Draw(_pixelTexture, new Rectangle(r.X + r.Width - thickness, r.Y, thickness, r.Height), c); // Right
    }
}
