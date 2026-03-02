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

        public int fontSize = 25;

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
                var lineCount = (line.Length + maxLineLength - 1) / maxLineLength;
                for (int i = 0; i < lineCount; i++)
                {
                    int start = i * maxLineLength;
                    int length = Math.Min(maxLineLength, line.Length - start);
                    var subLine = line[start..(start + length)];
                    _logs.Add(new LogEntry { message = subLine, type = type });
                }
            }

            if (_logs.Count > maxLines)
                _logs.RemoveRange(0, _logs.Count - maxLines);
        }

        void OnGUI()
        {
            // 适配分辨率：以 1080p 为基准进行等比缩放
            float scale = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                richText = true,
                wordWrap = true
            };

            // 绘制背景
            GUI.backgroundColor = new Color(0, 0, 0, 0.5f);
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            GUI.Box(new Rect(0, 0, width, height), "");

            float y = 10;
            for (int i = 0; i < _logs.Count; i++)
            {
                var log = _logs[i];
                var color = GetColor(log.type);
                var coloredText = $"<color={color}>{log.message}</color>";
                
                // 计算文本高度
                float textHeight = style.CalcHeight(new GUIContent(log.message), width - 20);
                GUI.Label(new Rect(10, y, width - 20, textHeight), coloredText, style);
                y += textHeight;

                if (y > height) break; // 超过屏幕高度停止绘制
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
