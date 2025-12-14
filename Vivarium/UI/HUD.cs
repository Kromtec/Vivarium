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
        UIComponents.DrawPanel(spriteBatch, _panelRect, _pixelTexture);

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

        // Use UIComponents.DrawButton
        var mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();
        bool isHover = _geneButtonRect.Contains(mouse.Position);
        bool isPress = isHover && mouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;

        UIComponents.DrawButton(spriteBatch, _font, _geneButtonRect, "GENES", _pixelTexture, isHover, isPress, UITheme.ButtonColor);

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
}
