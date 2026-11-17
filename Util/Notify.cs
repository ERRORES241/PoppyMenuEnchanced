using System.Collections.Generic;
using UnityEngine;

namespace PoppyMenu
{
    // Toasts at the top centre. They drop in from above and fade in (400 ms, ease), leave the same way
    // they came (300 ms), and the ones below slide up to close the gap. Reduce motion keeps only the fades.
    internal static class Notify
    {
        private sealed class Toast
        {
            internal string Msg;
            internal float Born, Until, LeaveAt = -1f;
            internal float Y, FromY, MoveAt;
            internal bool Placed;
        }

        private const int MaxToasts = 5;
        private static readonly List<Toast> _toasts = new List<Toast>();

        internal static void Push(string msg, float seconds = 2.5f)
        {
            if (string.IsNullOrEmpty(msg)) return;
            float now = Anim.Now;
            _toasts.Add(new Toast { Msg = msg, Born = now, Until = now + seconds });

            // Too many: the oldest one still on screen leaves early instead of vanishing.
            int staying = 0;
            foreach (Toast t in _toasts) if (t.LeaveAt < 0f) staying++;
            for (int i = 0; i < _toasts.Count && staying > MaxToasts; i++)
                if (_toasts[i].LeaveAt < 0f) { _toasts[i].LeaveAt = now; staying--; }
        }

        internal static void Draw()
        {
            if (_toasts.Count == 0) return;
            float now = Anim.Now;
            foreach (Toast t in _toasts)
                if (t.LeaveAt < 0f && now >= t.Until) t.LeaveAt = now;
            _toasts.RemoveAll(t => t.LeaveAt >= 0f && now - t.LeaveAt >= Anim.ToastOut);
            if (_toasts.Count == 0 || Event.current.type != EventType.Repaint) return;

            GUIStyle text = Theme.LabelBold;
            float h = Theme.S(34), gap = Theme.S(8), y = Theme.S(42);
            bool reduce = Anim.Reduce;
            Color prev = GUI.color;
            for (int i = 0; i < _toasts.Count; i++)
            {
                Toast t = _toasts[i];
                bool leaving = t.LeaveAt >= 0f;

                // Slot position: a leaving toast keeps its place until it is gone.
                if (!t.Placed) { t.Y = t.FromY = y; t.MoveAt = now - Anim.Toast; t.Placed = true; }
                else if (!leaving && !Mathf.Approximately(t.Y, y))
                {
                    t.FromY = Mathf.LerpUnclamped(t.FromY, t.Y, Anim.Progress(t.MoveAt, Anim.Toast, Anim.Ease));
                    t.Y = y;
                    t.MoveAt = now;
                }
                float slotY = reduce ? t.Y : Mathf.LerpUnclamped(t.FromY, t.Y, Anim.Progress(t.MoveAt, Anim.Toast, Anim.Ease));

                float a = Anim.Progress(t.Born, Anim.Toast, Anim.Ease);
                if (leaving) a *= 1f - Anim.Progress(t.LeaveAt, Anim.ToastOut, Anim.Ease);
                float dy = reduce ? 0f : -(h + gap) * (1f - a);

                var content = new GUIContent(t.Msg);
                float w = Mathf.Min(Screen.width - Theme.S(40), text.CalcSize(content).x + Theme.S(46));
                Rect r = new Rect(Mathf.Round((Screen.width - w) * 0.5f), Mathf.Round(slotY + dy), w, h);
                GUI.color = Anim.Alpha(prev, a);
                Theme.Rounded(r, 10f, new Color(Theme.Bg.r, Theme.Bg.g, Theme.Bg.b, 0.96f));
                float dot = Theme.S(8);
                Theme.Rounded(new Rect(r.x + Theme.S(16), r.y + (h - dot) * 0.5f, dot, dot), 4f, Theme.Accent);
                GUI.Label(new Rect(r.x + Theme.S(32), r.y, w - Theme.S(44), h), content, text);

                if (!leaving) y += h + gap;
            }
            GUI.color = prev;
        }
    }
}
