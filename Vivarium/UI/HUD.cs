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
    private readonly SettingsWindow _settingsWindow;

    private Rectangle _panelRect;
    private int _cursorY;
    private Rectangle _geneButtonRect;
    private Rectangle _settingsButtonRect;
    private bool _wasLeftButtonPressed = false;

    public Rectangle Bounds => _panelRect;

    public HUD(GraphicsDevice graphics, SpriteFont font, SimulationGraph simGraph, GenePoolWindow genePoolWindow, SettingsWindow settingsWindow)
    {
        _graphics = graphics;
        _font = font;
        _simGraph = simGraph;
        _genePoolWindow = genePoolWindow;
        _settingsWindow = settingsWindow;

        _pixelTexture = new Texture2D(graphics, 1, 1);
        _pixelTexture.SetData([Color.White]);
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
            else if (_settingsButtonRect.Contains(mouse.Position))
            {
                _settingsWindow.IsVisible = !_settingsWindow.IsVisible;
            }
        }

        _wasLeftButtonPressed = isPressed;
    }

    public bool IsMouseOver(Point mousePos)
    {
        return _panelRect.Contains(mousePos) || _geneButtonRect.Contains(mousePos) || _settingsButtonRect.Contains(mousePos);
    }

    public void Draw(SpriteBatch spriteBatch, long tickCount, int agents, int herbs, int omnis, int carnis, int plants, int structures)
    {
        // 1. Calculate Layout
        const int width = 300;
        const int startX = 20;
        const int startY = 20;
        int contentHeight = UITheme.Padding;

        const int graphHeight = 100;
        const int graphPadding = 10;

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

        // Buttons
        contentHeight += 40; // Space for buttons

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
        Rectangle graphBounds = new(leftX, _cursorY, _panelRect.Width - (UITheme.Padding * 2), graphHeight);
        _simGraph.SetBounds(graphBounds);
        _simGraph.Draw(spriteBatch);
        _cursorY += graphHeight + graphPadding;

        // -- POPULATION STATS --
        // -- STATS --
        spriteBatch.DrawString(_font, "POPULATION", new Vector2(leftX, _cursorY), UITheme.HeaderColor);
        _cursorY += 30;

        DrawStat(spriteBatch, "Agents", agents.ToString(), VivariumColors.Agent);
        DrawStat(spriteBatch, "- Herbivores", herbs.ToString(), VivariumColors.Herbivore);
        DrawStat(spriteBatch, "- Omnivores", omnis.ToString(), VivariumColors.Omnivore);
        DrawStat(spriteBatch, "- Carnivores", carnis.ToString(), VivariumColors.Carnivore);
        DrawStat(spriteBatch, "Plants", plants.ToString(), VivariumColors.Plant);
        DrawStat(spriteBatch, "Structures", structures.ToString(), VivariumColors.Structure);

        _cursorY += 20;

        // -- BUTTONS --
        int btnWidth = (_panelRect.Width - (UITheme.Padding * 3)) / 2;
        _geneButtonRect = new Rectangle(leftX, _cursorY, btnWidth, 30);
        _settingsButtonRect = new Rectangle(leftX + btnWidth + UITheme.Padding, _cursorY, btnWidth, 30);

        var mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();

        // Gene Button
        bool geneHover = _geneButtonRect.Contains(mouse.Position);
        bool genePress = geneHover && mouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
        UIComponents.DrawButton(spriteBatch, _font, _geneButtonRect, "GENETICS", _pixelTexture, geneHover, genePress);

        // Settings Button
        bool setHover = _settingsButtonRect.Contains(mouse.Position);
        bool setPress = setHover && mouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
        UIComponents.DrawButton(spriteBatch, _font, _settingsButtonRect, "SETTINGS", _pixelTexture, setHover, setPress);
    }

    private void DrawStat(SpriteBatch sb, string label, string value, Color valueColor)
    {
        sb.DrawString(_font, label, new Vector2(_panelRect.X + UITheme.Padding, _cursorY), UITheme.TextColorSecondary);
        Vector2 size = _font.MeasureString(value);
        sb.DrawString(_font, value, new Vector2(_panelRect.Right - UITheme.Padding - size.X, _cursorY), valueColor);
        _cursorY += UITheme.LineHeight;
    }
}
