using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Main
{
    public class ConsoleToScreen : MonoBehaviour
    {
        const int maxLines = 50;
        const int maxLineLength = 120;

        private class LogEntry
        {
            public string message;
            public LogType type;
        }

        private readonly List<LogEntry> _logs = new();

        public int fontSize = 15;

        void OnEnable() { Application.logMessageReceived += Log; }
        void OnDisable() { Application.logMessageReceived -= Log; }

        public void Log(string logString, string stackTrace, LogType type)
        {
            var entry = new LogEntry { type = type };

            foreach (var line in logString.Split('\n'))
            {
                if (line.Length <= maxLineLength)
                {
                    _logs.Add(new LogEntry { message = line, type = type });
                    continue;
                }
                var lineCount = line.Length / maxLineLength + 1;
                for (int i = 0; i < lineCount; i++)
                {
                    var subLine = (i + 1) * maxLineLength <= line.Length
                        ? line.Substring(i * maxLineLength, maxLineLength)
                        : line.Substring(i * maxLineLength, line.Length - i * maxLineLength);
                    _logs.Add(new LogEntry { message = subLine, type = type });
                }
            }

            if (_logs.Count > maxLines)
                _logs.RemoveRange(0, _logs.Count - maxLines);
        }

        void OnGUI()
        {
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
               new Vector3(Screen.width / 1920.0f, Screen.height / 1080.0f, 1.0f));

            var style = new GUIStyle
            {
                fontSize = Math.Max(10, fontSize),
                richText = true
            };

            float y = 10;
            foreach (var log in _logs)
            {
                var color = GetColor(log.type);
                var coloredText = $"<color={color}>{log.message}</color>";
                var size = style.CalcSize(new GUIContent(log.message));
                GUI.Label(new Rect(10, y, size.x + 10, size.y), coloredText, style);
                y += size.y;
            }
        }

        private string GetColor(LogType type)
        {
            return type switch
            {
                LogType.Error or LogType.Exception or LogType.Assert => "red",
                LogType.Warning => "yellow",
                _ => "white"
            };
        }
    }
}
