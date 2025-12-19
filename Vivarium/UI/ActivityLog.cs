using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Vivarium.UI;

public static class ActivityLog
{
    private struct LogEntry
    {
        public string Message;
        public long Tick;
    }

    private static readonly List<LogEntry> _entries = [];
    private static long _targetAgentId = -1;
    private static bool _isLoggingEnabled = false;
    private static long _currentTick = 0;
    private const int MaxEntries = 8;

    public static bool IsLoggingEnabled => _isLoggingEnabled;
    public static long TargetAgentId => _targetAgentId;

    [InterpolatedStringHandler]
    public readonly ref struct LogHandler
    {
        private readonly StringBuilder _builder;
        public bool Enabled { get; }

        public LogHandler(int literalLength, int formattedCount, long agentId, out bool handlerIsValid)
        {
            Enabled = ActivityLog.IsLoggingEnabled && ActivityLog.TargetAgentId == agentId;
            handlerIsValid = Enabled;
            if (Enabled)
            {
                _builder = new StringBuilder(literalLength + (formattedCount * 20));
            }
            else
            {
                _builder = null;
            }
        }

        public void AppendLiteral(string s) => _builder.Append(s);
        public void AppendFormatted<T>(T t) => _builder.Append(t);
        public void AppendFormatted<T>(T t, string format) => _builder.AppendFormat(null, "{0:" + format + "}", t);

        public string GetFormattedText() => _builder.ToString();
    }

    public static void Log(long agentId, [InterpolatedStringHandlerArgument("agentId")] ref LogHandler handler)
    {
        if (handler.Enabled)
        {
            Log(agentId, handler.GetFormattedText());
        }
    }

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

        const int width = 600;
        const int lineHeight = 20;
        const int height = (MaxEntries * lineHeight) + 20;
        int x = (graphics.Viewport.Width - width) / 2;
        int y = graphics.Viewport.Height - height - 50;

        // Draw Background
        Texture2D pixel = new(graphics, 1, 1);
        pixel.SetData([Color.White]);

        Rectangle bgRect = new(x, y, width, height);
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
