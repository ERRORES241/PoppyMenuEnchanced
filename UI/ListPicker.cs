using System;
using System.Collections.Generic;
using UnityEngine;

namespace PoppyMenu
{
    // Searchable list window for items, buffs, spawn cards and features.
    // Position in screen pixels, size in logical units (multiplied by Theme.Scale).
    internal static class ListPicker
    {
        internal struct Row
        {
            internal string Label;
            internal Color Color;
            internal Action OnClick;
            internal int Tag; // filter chip the row belongs to (1-based); 0 shows only under "All"
            internal Row(string label, Color color, Action onClick, int tag = 0) { Label = label; Color = color; OnClick = onClick; Tag = tag; }
        }

        private const int WindowId = 0x5B7A01;
        private const string SearchControl = "poppy_picker_search";
        private static bool _open, _focusSearch;
        private static string _title = "";
        private static string _search = "";
        private static Vector2 _scroll;
        private static List<Row> _rows = new List<Row>();
        private static Vector2 _pos = new Vector2(700, 90);
        private static Vector2 _size = new Vector2(380, 560);
        private static Rect _screenRect;
        private static bool _resizing;
        private static float _openedAt;

        // Filter chips under the search ("All" first). The choice survives switching lists inside an open picker.
        private static string[] _filters;
        private static int _filter;

        // Live lists (inventory) are rebuilt a few times a second, so removed items drop out.
        private static Func<List<Row>> _source;
        private static float _nextRefresh;

        internal static bool IsOpen => _open;

        internal static void Open(string title, List<Row> rows, string[] filters = null)
        {
            _source = null;
            Show(title, rows, filters);
        }

        internal static void OpenLive(string title, Func<List<Row>> source, string[] filters = null)
        {
            _source = source;
            _nextRefresh = Anim.Now + 0.3f;
            Show(title, source(), filters);
        }

        private static void Show(string title, List<Row> rows, string[] filters)
        {
            _title  = title;
            _rows   = rows ?? new List<Row>();
            _search = "";
            _scroll = Vector2.zero;
            if (!_open) { _openedAt = Anim.Now; _filter = 0; } // switching lists inside an open picker doesn't replay it
            _filters = filters;
            _open   = true;
            _focusSearch = true;
        }

        internal static void Close() => _open = false;

        internal static bool ContainsPoint(Vector2 screenPoint) => _open && _screenRect.Contains(screenPoint);

        internal static void Draw()
        {
            if (!_open) return;
            float w = Theme.S(_size.x), h = Theme.S(_size.y);
            _pos.x = Mathf.Clamp(_pos.x, 80f - w, Screen.width - 80f);
            _pos.y = Mathf.Clamp(_pos.y, 0f, Screen.height - 40f);
            _screenRect = new Rect(_pos.x, _pos.y, w, h);
            _pos = GUI.Window(WindowId, _screenRect, DrawWindow, "", GUIStyle.none).position;
        }

        // Opens with a 200 ms fade and grows from 96% around its centre (it isn't anchored to the row
        // that opened it). Closing is instant: Esc and the close button are dismissals.
        private static void DrawWindow(int id)
        {
            float a = Anim.Progress(_openedAt, Anim.Popup, Anim.EaseOut);
            if (a >= 1f) { DrawBody(); return; }
            Color prev = GUI.color;
            GUI.color = Anim.Alpha(prev, a);
            Matrix4x4 saved = Anim.Begin(new Vector2(_screenRect.width * 0.5f, _screenRect.height * 0.5f), Anim.Reduce ? 1f : Mathf.Lerp(0.96f, 1f, a));
            try { DrawBody(); }
            finally { Anim.End(saved); GUI.color = prev; }
        }

        private static void DrawBody()
        {
            float W = _screenRect.width, H = _screenRect.height;
            Theme.Box(new Rect(0, 0, W, H), Theme.WindowBg);

            Rect grip = Widgets.GripRect(W, H);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && grip.Contains(Event.current.mousePosition))
            {
                _resizing = true;
                Event.current.Use();
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Close();
                Event.current.Use();
                return;
            }

            float pad = Theme.S(16), th = Theme.S(50);
            Vector2 m = Event.current.mousePosition;

            // ── Header ──
            GUI.Label(new Rect(pad, 0, W - pad * 2f - Theme.S(36), th), _title, Theme.LabelBold);
            Rect close = new Rect(W - Theme.S(12) - Theme.S(28), (th - Theme.S(28)) * 0.5f, Theme.S(28), Theme.S(28));
            if (close.Contains(m)) Theme.Rounded(close, 8f, Theme.Ctrl);
            Icons.DrawCentered(close, Icons.X, 14, close.Contains(m) ? Theme.Text : Theme.Sub);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && close.Contains(m))
            {
                Close();
                Event.current.Use();
                return;
            }

            // ── Search ──
            Rect search = new Rect(pad, th, W - pad * 2f, Theme.S(32));
            GUI.SetNextControlName(SearchControl);
            string next = GUI.TextField(search, _search ?? "", 64, Theme.Search);
            if (next != _search) { _search = next; _scroll = Vector2.zero; }
            if (_focusSearch && Event.current.type == EventType.Repaint) { GUI.FocusControl(SearchControl); _focusSearch = false; }
            Icons.Draw(new Rect(search.x + Theme.S(10), search.y + (search.height - Theme.S(14)) * 0.5f, Theme.S(14), Theme.S(14)), Icons.Search, Theme.Faint);
            if (string.IsNullOrEmpty(_search) && GUI.GetNameOfFocusedControl() != SearchControl)
                GUI.Label(new Rect(search.x + Theme.S(30), search.y, search.width - Theme.S(36), search.height), "Search", Theme.Placeholder);

            string filter = (_search ?? "").Trim();
            bool hasFilter = filter.Length > 0;

            if (_source != null && Event.current.type == EventType.Layout && Anim.Now >= _nextRefresh)
            {
                _rows = _source() ?? new List<Row>();
                _nextRefresh = Anim.Now + 0.3f;
            }

            // ── Filter chips ──
            float listTop = search.yMax + Theme.S(10);
            int chip = _filters != null ? _filter : 0;
            if (_filters != null) listTop = DrawChips(pad, listTop, W - pad * 2f) + Theme.S(8);

            // ── List ──
            float footH = Theme.S(30);
            Rect listRect = new Rect(Theme.S(8), listTop, W - Theme.S(8) - Theme.S(6), H - listTop - footH);
            float rowH = Theme.S(32);

            int matched = 0;
            for (int i = 0; i < _rows.Count; i++)
                if (Passes(_rows[i], chip, hasFilter, filter)) matched++;

            Rect view = new Rect(0, 0, listRect.width - Theme.S(12), matched * rowH);
            _scroll = GUI.BeginScrollView(listRect, _scroll, view, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            int first = Mathf.Max(0, Mathf.FloorToInt(_scroll.y / rowH) - 1);
            int last = Mathf.CeilToInt((_scroll.y + listRect.height) / rowH) + 1;
            int n = 0;
            for (int i = 0; i < _rows.Count; i++)
            {
                Row row = _rows[i];
                if (!Passes(row, chip, hasFilter, filter)) continue;
                int idx = n++;
                if (idx < first || idx > last) continue;

                Rect r = new Rect(0, idx * rowH, view.width, rowH);
                bool hover = r.Contains(Event.current.mousePosition);
                if (hover) Theme.Rounded(r, 7f, Theme.RowHover);
                float dot = Theme.S(8);
                Theme.Rounded(new Rect(r.x + Theme.S(10), r.y + (rowH - dot) * 0.5f, dot, dot), 4f, row.Color);
                if (Event.current.type == EventType.Repaint)
                    GUI.Label(new Rect(r.x + Theme.S(28), r.y, r.width - Theme.S(36), rowH), row.Label, Theme.Label);
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && hover)
                {
                    Event.current.Use();
                    row.OnClick?.Invoke();
                }
            }
            GUI.EndScrollView();

            // ── Footer ──
            if (Event.current.type == EventType.Repaint)
            {
                Theme.Fill(new Rect(0, H - footH, W, Mathf.Max(1f, Theme.S(1))), Theme.Line);
                string foot = matched == 0 ? "No results" : matched + " of " + _rows.Count;
                GUI.Label(new Rect(pad, H - footH, W - pad * 2f, footH), foot + "   ·   Esc to close", Theme.Small);
            }

            HandleResize(grip);
            GUI.DragWindow(new Rect(0, 0, W - Theme.S(48), th));
        }

        private static bool Passes(Row row, int chip, bool hasFilter, string filter) =>
            (chip == 0 || row.Tag == chip) &&
            (!hasFilter || row.Label.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);

        // Wrapping row of chips; returns the bottom edge.
        private static float DrawChips(float x0, float y, float width)
        {
            float h = Theme.S(24), gap = Theme.S(6), x = x0;
            Vector2 m = Event.current.mousePosition;
            for (int i = 0; i < _filters.Length; i++)
            {
                float w = Theme.Seg.CalcSize(new GUIContent(_filters[i])).x + Theme.S(18);
                if (x > x0 && x + w > x0 + width) { x = x0; y += h + gap; }
                Rect r = new Rect(x, y, w, h);
                bool on = i == _filter;
                if (Event.current.type == EventType.Repaint)
                {
                    if (on) Theme.SegOn.Draw(r, new GUIContent(_filters[i]), false, false, false, false);
                    else
                    {
                        Theme.Rounded(r, 7f, r.Contains(m) ? Theme.CtrlHover : Theme.Ctrl);
                        GUI.Label(r, _filters[i], Theme.Seg);
                    }
                }
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && r.Contains(m))
                {
                    _filter = i;
                    _scroll = Vector2.zero;
                    Event.current.Use();
                }
                x += w + gap;
            }
            return y + h;
        }

        private static void HandleResize(Rect grip)
        {
            Widgets.ResizeGrip(grip, _resizing);
            if (!_resizing) return;

            Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            _size.x = Mathf.Clamp((mouse.x - _pos.x + 3f) / Theme.Scale, 300f, 800f);
            _size.y = Mathf.Clamp((mouse.y - _pos.y + 3f) / Theme.Scale, 300f, 900f);
            if (!Input.GetMouseButton(0)) _resizing = false;
        }
    }
}
