using System.Collections.Generic;
using UnityEngine;

namespace PoppyMenu
{
    // Small transition helper for IMGUI. Values are retargeted from where they are now, so a toggle
    // clicked twice quickly reverses smoothly instead of restarting. Time is real time: the menu
    // keeps animating while the game is paused or frozen.
    internal static class Anim
    {
        // Durations in seconds.
        internal const float Press = 0.16f, Toggle = 0.16f, Slide = 0.2f, Tooltip = 0.125f,
            DropdownIn = 0.18f, DropdownOut = 0.12f, Popup = 0.2f, Toast = 0.4f, ToastOut = 0.3f;

        // Curves (cubic-bezier control points).
        internal static readonly Curve EaseOut = new Curve(0.23f, 1f, 0.32f, 1f);     // entering, responding to input
        internal static readonly Curve EaseInOut = new Curve(0.77f, 0f, 0.175f, 1f);  // moving on screen
        internal static readonly Curve Ease = new Curve(0.25f, 0.1f, 0.25f, 1f);      // toasts

        // Reduce motion keeps fades and colour changes and drops sliding and scaling.
        internal static bool Reduce => ModConfig.ReduceMotion != null && ModConfig.ReduceMotion.Value;

        internal static float Now => Time.realtimeSinceStartup;

        internal sealed class Curve
        {
            private readonly float _x1, _y1, _x2, _y2;
            internal Curve(float x1, float y1, float x2, float y2) { _x1 = x1; _y1 = y1; _x2 = x2; _y2 = y2; }

            private static float B(float t, float a, float b) => 3f * a * (1f - t) * (1f - t) * t + 3f * b * (1f - t) * t * t + t * t * t;
            private static float dB(float t, float a, float b) => 3f * a * (1f - t) * (1f - t) + 6f * (b - a) * (1f - t) * t + 3f * (1f - b) * t * t;

            internal float Evaluate(float x)
            {
                if (x <= 0f) return 0f;
                if (x >= 1f) return 1f;
                float t = x;
                for (int i = 0; i < 6; i++)
                {
                    float d = dB(t, _x1, _x2);
                    if (Mathf.Abs(d) < 1e-5f) break;
                    t = Mathf.Clamp01(t - (B(t, _x1, _x2) - x) / d);
                }
                // Newton can stall on steep curves; finish with bisection.
                if (Mathf.Abs(B(t, _x1, _x2) - x) > 1e-4f)
                {
                    float lo = 0f, hi = 1f;
                    t = x;
                    for (int i = 0; i < 20; i++)
                    {
                        float v = B(t, _x1, _x2);
                        if (Mathf.Abs(v - x) < 1e-4f) break;
                        if (v < x) lo = t; else hi = t;
                        t = (lo + hi) * 0.5f;
                    }
                }
                return B(t, _y1, _y2);
            }
        }

        private sealed class Tween { internal float From, To, Start, Dur; internal Curve C; }
        private static readonly Dictionary<string, Tween> _tweens = new Dictionary<string, Tween>();

        private static float Eval(Tween t, float now)
        {
            if (t.Dur <= 0f) return t.To;
            float p = Mathf.Clamp01((now - t.Start) / t.Dur);
            return Mathf.LerpUnclamped(t.From, t.To, t.C.Evaluate(p));
        }

        // Current value of a transition toward target. The first call for a key returns the target
        // (nothing animates in from a default). Pass snap to jump without animating.
        internal static float To(string key, float target, float duration, Curve curve, bool snap = false)
        {
            float now = Now;
            if (!_tweens.TryGetValue(key, out Tween t))
            {
                _tweens[key] = new Tween { From = target, To = target, Start = now, Dur = 0f, C = curve };
                return target;
            }
            if (snap) { t.From = t.To = target; t.Dur = 0f; return target; }
            if (t.To != target)
            {
                t.From = Eval(t, now);
                t.To = target;
                t.Start = now;
                t.Dur = duration;
                t.C = curve;
            }
            return Eval(t, now);
        }

        // Eased 0..1 progress of something that started at `start`.
        internal static float Progress(float start, float duration, Curve curve) =>
            duration <= 0f ? 1f : curve.Evaluate(Mathf.Clamp01((Now - start) / duration));

        /* ─────── Press feedback ─────────────────────────────────────── */

        private static string _pressKey;

        // Call before the control consumes its MouseDown. Returns the scale to draw it at:
        // 0.97 while held, back to 1 over 160 ms on release.
        internal static float PressScale(string key, Rect r, float pressed = 0.97f)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition)) _pressKey = key;
            bool down = _pressKey == key && Input.GetMouseButton(0);
            if (_pressKey == key && !Input.GetMouseButton(0)) _pressKey = null;
            float s = To("press:" + key, down ? pressed : 1f, Press, EaseOut);
            return Reduce ? 1f : s;
        }

        // Scales GUI drawing around a pivot on Repaint only, so hit-testing stays exact.
        // Returns the matrix to restore with End.
        internal static Matrix4x4 Begin(Vector2 pivot, float scale)
        {
            Matrix4x4 saved = GUI.matrix;
            if (scale != 1f && Event.current.type == EventType.Repaint)
                GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), pivot);
            return saved;
        }

        internal static void End(Matrix4x4 saved) => GUI.matrix = saved;

        internal static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, c.a * a);
    }
}
