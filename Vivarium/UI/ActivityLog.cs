using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Vivarium.UI;

public static class ActivityLog
{
    private struct LogEntry
    {
        public string Message;
        public long Tick;
    }

    private static readonly List<LogEntry> _entries = new List<LogEntry>();
    private static long _targetAgentId = -1;
    private static bool _isLoggingEnabled = false;
    private static long _currentTick = 0;
    private const int MaxEntries = 8;

    public static void SetTarget(long agentId)
    {
        if (_targetAgentId != agentId)
        {
            _targetAgentId = agentId;
            _entries.Clear();
        }
    }

    public static void Enable(long currentTick)
    {
        _isLoggingEnabled = true;
        _currentTick = currentTick;
    }

    public static void Disable()
    {
        _isLoggingEnabled = false;
        _entries.Clear(); // Clear log when disabled
    }

    public static void Log(long agentId, string message)
    {
        if (!_isLoggingEnabled) return;
        if (agentId != _targetAgentId) return;

        // Prepend Agent ID to message
        string fullMessage = $"[#{agentId}] {message}";
        _entries.Add(new LogEntry { Message = fullMessage, Tick = _currentTick });

        if (_entries.Count > MaxEntries)
        {
            _entries.RemoveAt(0);
        }
    }

    public static void Draw(SpriteBatch sb, SpriteFont font, GraphicsDevice graphics)
    {
        if (_entries.Count == 0) return;

        int width = 600;
        int lineHeight = 20;
        int height = (MaxEntries * lineHeight) + 20;
        int x = (graphics.Viewport.Width - width) / 2;
        int y = graphics.Viewport.Height - height - 50;

        // Draw Background
        Texture2D pixel = new Texture2D(graphics, 1, 1);
        pixel.SetData(new[] { Color.White });

        Rectangle bgRect = new Rectangle(x, y, width, height);
        sb.Draw(pixel, bgRect, Color.Black * 0.7f);
        // Removed Border

        // Draw Entries
        // Newest at bottom
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];

            // Calculate opacity based on age in the list (index)
            // Index 0 (Oldest) -> Faded
            // Index Count-1 (Newest) -> Opaque

            // Align to bottom:
            // If we have fewer entries than MaxEntries, push them down.
            int offset = MaxEntries - _entries.Count;
            int drawIndex = i + offset;

            int textY = y + 10 + (drawIndex * lineHeight);

            // Opacity: 
            // Newest (i == Count-1) -> 1.0
            // Oldest (i == 0) -> Lower
            float opacity = 0.4f + (0.6f * ((float)(i + 1) / _entries.Count));

            sb.DrawString(font, $"> {entry.Message}", new Vector2(x + 10, textY), Color.White * opacity);
        }
    }
}
