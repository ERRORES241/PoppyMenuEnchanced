using System.Collections.Generic;
using UnityEngine;

namespace PoppyMenu
{
    internal static class Notify
    {
        private struct Toast { internal string Msg; internal float Until; }

        private static readonly List<Toast> _toasts = new List<Toast>();

        internal static void Push(string msg, float seconds = 2.5f)
        {
            if (string.IsNullOrEmpty(msg)) return;
            _toasts.Add(new Toast { Msg = msg, Until = Time.realtimeSinceStartup + seconds });
            if (_toasts.Count > 5) _toasts.RemoveAt(0);
        }

        internal static void Draw()
        {
            if (_toasts.Count == 0) return;
            float now = Time.realtimeSinceStartup;
            _toasts.RemoveAll(t => t.Until <= now);
            if (_toasts.Count == 0) return;

            GUIStyle text = Theme.LabelBold;
            float h = Theme.S(34), gap = Theme.S(8), y = Theme.S(42);
            for (int i = 0; i < _toasts.Count; i++)
            {
                var content = new GUIContent(_toasts[i].Msg);
                float w = Mathf.Min(Screen.width - Theme.S(40), text.CalcSize(content).x + Theme.S(46));
                Rect r = new Rect(Mathf.Round((Screen.width - w) * 0.5f), y, w, h);
                if (Event.current.type == EventType.Repaint)
                {
                    Theme.Rounded(r, 10f, new Color(Theme.Bg.r, Theme.Bg.g, Theme.Bg.b, 0.96f));
                    float dot = Theme.S(8);
                    Theme.Rounded(new Rect(r.x + Theme.S(16), r.y + (h - dot) * 0.5f, dot, dot), 4f, Theme.Accent);
                    GUI.Label(new Rect(r.x + Theme.S(32), r.y, w - Theme.S(44), h), content, text);
                }
                y += h + gap;
            }
        }
    }
}
