using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Configuration;
using UnityEngine;

namespace PoppyMenu
{
    // Row-based widgets: a section is a rounded panel of rows separated by 1 px lines.
    // A row is label + optional description on the left and its control on the right.
    // While the menu search box has text, rows whose label/description don't match are skipped,
    // and section titles / page headers are emitted lazily before the first match.
    internal static class Widgets
    {
        internal static string CurrentTooltip;
        internal static string NextTooltip;

        /* ═══════════════════════════════════════════════════════════════
         *  Page, section and search state
         * ═══════════════════════════════════════════════════════════════ */

        internal static string Filter;
        internal static bool Searching => !string.IsNullOrEmpty(Filter);
        internal static int Matches;
        internal static int OpenSections;

        // "Tab/Page" of the page being drawn; rows are keyed "Tab/Page/Label" for Featured.
        internal static string PageId;
        internal static string RowKey(string label) => PageId + "/" + label;

        // Featured tab: every page is drawn, but only rows whose key is in this set show up,
        // all inside the one section the Featured page opened.
        internal static HashSet<string> Only;
        private static bool Filtering => Searching || Only != null;

        private static string _pendingPage;
        private static string _sectionTitle;
        private static bool _inSection, _sectionOpen, _firstSectionOnPage;
        private static int _rowsInSection;
        private static float _rowWidth;
        private static int _fieldCounter;

        // Row state set by Show and used by Row: its key, whether it needs the host, whether it is dimmed.
        private static bool _sectionRun, _nextHost, _rowHost, _rowOff, _dimmed;
        private static string _rowKey, _rowLabel, _nextBindId, _nextBindName, _rowBindId, _rowBindName;
        private static Color _undimColor;

        // Called by MenuRoot before a page's DrawMenu. pageTitle is shown before the first match while searching.
        internal static void BeginPage(string pageTitle)
        {
            _pendingPage = Searching ? pageTitle : null;
            _fieldCounter = 0;
            _inSection = _sectionOpen = false;
            _firstSectionOnPage = true;
            _rowsInSection = 0;
            OpenSections = 0;
        }

        // Closes anything a throwing DrawMenu left open.
        internal static void EndPage()
        {
            Undim();
            while (OpenSections > 0) { GUILayout.EndVertical(); OpenSections--; }
            _inSection = _sectionOpen = false;
            _sectionRun = _nextHost = false;
            _nextBindId = null;
        }

        // run: the section's action rows (buttons, pickers) need a run and are dimmed outside one.
        // Toggles and sliders stay usable, so features can be set up from the main menu.
        internal static void SectionBegin(string title, bool run = false)
        {
            Undim();
            _sectionRun = run;
            if (Only != null) return;
            if (_sectionOpen) SectionEnd();
            _inSection = true;
            _sectionOpen = false;
            _sectionTitle = title;
            _rowsInSection = 0;
            if (!Searching) OpenSection();
        }

        internal static void SectionEnd()
        {
            Undim();
            _sectionRun = false;
            if (Only != null) return;
            if (_sectionOpen && OpenSections > 0) { GUILayout.EndVertical(); OpenSections--; }
            _inSection = _sectionOpen = false;
        }

        private static void OpenSection()
        {
            EmitPageHeader();
            GUILayout.Space(Theme.S(_firstSectionOnPage ? 8 : 16));
            _firstSectionOnPage = false;
            if (!string.IsNullOrEmpty(_sectionTitle))
            {
                GUILayout.Label(_sectionTitle.ToUpperInvariant(), Theme.SectionTitle, GUILayout.Height(Theme.S(14)));
                GUILayout.Space(Theme.S(7));
            }
            GUILayout.BeginVertical(Theme.PanelBg);
            OpenSections++;
            _sectionOpen = true;
            _rowsInSection = 0;
        }

        private static void EmitPageHeader()
        {
            if (_pendingPage == null) return;
            GUILayout.Space(Theme.S(Matches > 1 ? 18 : 6));
            GUILayout.Label(_pendingPage, Theme.LabelBold, GUILayout.Height(Theme.S(18)));
            _pendingPage = null;
            _firstSectionOnPage = true;
        }

        private static bool Match(string s) => !string.IsNullOrEmpty(s) && s.IndexOf(Filter, StringComparison.OrdinalIgnoreCase) >= 0;

        // The next row works only on the host (a server-side hook): it gets a HOST tag and is dimmed for clients.
        internal static void HostOnly() => _nextHost = true;

        // The next row binds this action instead of the ones ActionRegistry links to its label (macros).
        internal static void BindsTo(string actionId, string name) { _nextBindId = actionId; _nextBindName = name; }

        // Decides whether a row is drawn and opens its section on demand.
        // action: a one-shot row (button, picker), dimmed in run-only sections while there is no run.
        private static bool Show(string label, string desc, bool searchable = true, bool action = false)
        {
            Undim();
            bool host = _nextHost;
            _nextHost = false;
            string bindId = _nextBindId, bindName = _nextBindName;
            _nextBindId = null;
            _rowKey = _rowLabel = null;
            _rowOff = false;

            if (Only != null)
            {
                if (!searchable || !Only.Contains(RowKey(label))) return false;
                Matches++;
            }
            else if (Searching)
            {
                if (!searchable || (!Match(label) && !Match(desc))) return false;
                Matches++;
                EmitPageHeader();
            }
            if (_inSection && !_sectionOpen) OpenSection();

            if (searchable)
            {
                _rowKey = RowKey(label);
                _rowLabel = label;
            }
            _rowHost = host;
            _rowBindId = bindId;
            _rowBindName = bindName;
            _rowOff = (action && _sectionRun && !PlayerContext.InGame) || (host && PlayerContext.InGame && !NetUtil.IsServer);
            if (_rowOff && Repaint)
            {
                _undimColor = GUI.color;
                GUI.color = Anim.Alpha(_undimColor, 0.4f);
                _dimmed = true;
            }
            return true;
        }

        private static void Undim()
        {
            if (!_dimmed) return;
            GUI.color = _undimColor;
            _dimmed = false;
        }

        /* ═══════════════════════════════════════════════════════════════
         *  Row core
         * ═══════════════════════════════════════════════════════════════ */

        private static readonly GUIContent _tmp = new GUIContent();
        private static GUIContent C(string s) { _tmp.text = s; return _tmp; }

        private static bool Repaint => Event.current.type == EventType.Repaint;

        // Reserves a row, draws divider, hover, label and description. Returns the row rect.
        private static Matrix4x4 _rowMatrix;
        private static bool _rowScaled;

        // pressKey: the whole row is a button. It shrinks to 98% while held (the divider stays put);
        // the caller ends the scale with EndRowPress after drawing its control.
        private static Rect Row(string label, string desc, float ctrlW, bool hoverable = true, GUIStyle labelStyle = null, string pressKey = null)
        {
            float padX = Theme.S(14), padY = Theme.S(9), gap = Theme.S(12);
            float width = _rowWidth > 1f ? _rowWidth : Theme.S(420);
            float textW = Mathf.Max(Theme.S(60), width - padX * 2f - ctrlW - (ctrlW > 0 ? gap : 0));
            float lh = Theme.S(18);
            float dh = string.IsNullOrEmpty(desc) ? 0f : Theme.Desc.CalcHeight(C(desc), textW) + Theme.S(2);
            float h = Mathf.Max(Theme.S(44), padY * 2f + lh + dh);

            Rect r = GUILayoutUtility.GetRect(0f, h, GUILayout.ExpandWidth(true));
            if (Repaint && r.width > 1f) _rowWidth = r.width;
            float press = pressKey != null && !_rowOff ? Anim.PressScale("row:" + pressKey, r, 0.98f) : 1f;

            Event ev = Event.current;
            if (_rowKey != null && ev.type == EventType.MouseDown && ev.button == 1 && r.Contains(ev.mousePosition))
            {
                RowMenu.OpenRow(_rowKey, _rowLabel, _rowBindId, _rowBindName, GUIUtility.GUIToScreenPoint(ev.mousePosition));
                ev.Use();
            }

            if (Repaint)
            {
                if (_sectionOpen && _rowsInSection > 0)
                    Theme.Fill(new Rect(r.x + padX, r.y, r.width - padX * 2f, Mathf.Max(1f, Theme.S(1))), Theme.Line);
                if (press != 1f) { _rowMatrix = Anim.Begin(r.center, press); _rowScaled = true; }
                if (hoverable && !_rowOff && r.Contains(Event.current.mousePosition))
                {
                    float i = Theme.S(3);
                    Theme.HoverBg.Draw(new Rect(r.x + i, r.y + i, r.width - i * 2f, r.height - i * 2f), false, false, false, false);
                }

                textW = Mathf.Max(Theme.S(60), r.width - padX * 2f - ctrlW - (ctrlW > 0 ? gap : 0));
                dh = string.IsNullOrEmpty(desc) ? 0f : Theme.Desc.CalcHeight(C(desc), textW) + Theme.S(2);
                float ty = r.y + (r.height - lh - dh) * 0.5f;
                GUIStyle ls = labelStyle ?? Theme.Label;
                GUI.Label(new Rect(r.x + padX, ty, textW, lh), label, ls);
                if (dh > 0f) GUI.Label(new Rect(r.x + padX, ty + lh + Theme.S(1), textW, dh), desc, Theme.Desc);
                if (_rowKey != null || _rowHost)
                    DrawBadges(r.x + padX + Mathf.Min(TextW(ls, label), textW) + Theme.S(7), ty, lh, r.x + padX + textW);
            }
            _rowsInSection++;
            return r;
        }

        // After the label: HOST tag, the bound key (or "Press a key" while binding) and a star on Featured rows.
        private static void DrawBadges(float x, float y, float lh, float maxX)
        {
            float bh = Theme.S(16), by = Mathf.Round(y + (lh - bh) * 0.5f);
            if (_rowHost)
            {
                float w = TextW(Theme.Pill, "HOST") + Theme.S(10);
                if (x + w > maxX) return;
                Rect t = new Rect(x, by, w, bh);
                Theme.Rounded(t, 4f, new Color(Theme.Warn.r, Theme.Warn.g, Theme.Warn.b, 0.14f));
                Theme.Pill.normal.textColor = Theme.Warn;
                GUI.Label(t, "HOST", Theme.Pill);
                x += w + Theme.S(5);
            }
            if (_rowKey == null) return;

            string keys = RowMenu.BadgeText(_rowKey, _rowLabel, _rowBindId);
            if (keys != null)
            {
                float w = TextW(Theme.Kbd, keys) + Theme.S(2);
                if (x + w > maxX) return;
                bool waiting = RowMenu.IsBinding(_rowKey);
                Rect k = new Rect(x, by, w, bh);
                if (waiting) Theme.Rounded(k, 4f, Theme.AccentSoft);
                Color prev = Theme.Kbd.normal.textColor;
                if (waiting) Theme.Kbd.normal.textColor = Theme.Accent;
                Theme.Kbd.Draw(k, C(keys), false, false, false, false);
                Theme.Kbd.normal.textColor = prev;
                x += w + Theme.S(5);
            }
            if (Only == null && FeaturedStore.Has(_rowKey))
            {
                float s = Theme.S(12);
                if (x + s <= maxX) Icons.Draw(new Rect(x, Mathf.Round(y + (lh - s) * 0.5f), s, s), Icons.Star, Theme.Accent);
            }
        }

        private static void EndRowPress()
        {
            if (!_rowScaled) return;
            Anim.End(_rowMatrix);
            _rowScaled = false;
        }

        // GUI.Button that shrinks to 96% while held.
        private static bool PressButton(Rect b, string text, GUIStyle st, string key)
        {
            float s = _rowOff ? 1f : Anim.PressScale("btn:" + key, b, 0.96f);
            Matrix4x4 m = Anim.Begin(b.center, s);
            bool hit = GUI.Button(b, text, st) && !_rowOff;
            Anim.End(m);
            return hit;
        }

        // Control area of width w, vertically centred, right-aligned in the row.
        private static Rect Ctrl(Rect row, float w, float h) =>
            new Rect(row.xMax - Theme.S(14) - w, Mathf.Round(row.y + (row.height - h) * 0.5f), w, h);

        private static bool Clicked(Rect r)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition))
            {
                e.Use();
                return !_rowOff;
            }
            return false;
        }

        private static float TextW(GUIStyle s, string text) => s.CalcSize(C(text)).x;

        /* ═══════════════════════════════════════════════════════════════
         *  Text rows
         * ═══════════════════════════════════════════════════════════════ */

        // Plain text row (status lines, stats). Not searchable.
        internal static void Label(string text)
        {
            if (!Show(text, null, false)) return;
            Row(text, null, 0f, false);
        }

        // "Name ........ value" read-only row. Not searchable.
        internal static void Value(string label, string value)
        {
            if (!Show(label, null, false)) return;
            float w = Mathf.Min(Theme.S(220), TextW(Theme.Value, value) + Theme.S(2));
            Rect r = Row(label, null, w, false);
            if (Repaint) GUI.Label(Ctrl(r, w, Theme.S(20)), value, Theme.Value);
        }

        // Explanatory text in a section, in the description colour.
        internal static void Note(string text)
        {
            if (!Show(text, null, false)) return;
            float padX = Theme.S(14);
            float width = _rowWidth > 1f ? _rowWidth : Theme.S(420);
            float h = Theme.Desc.CalcHeight(C(text), width - padX * 2f) + Theme.S(20);
            Rect r = GUILayoutUtility.GetRect(0f, h, GUILayout.ExpandWidth(true));
            if (Repaint)
            {
                if (_sectionOpen && _rowsInSection > 0)
                    Theme.Fill(new Rect(r.x + padX, r.y, r.width - padX * 2f, Mathf.Max(1f, Theme.S(1))), Theme.Line);
                GUI.Label(new Rect(r.x + padX, r.y + Theme.S(10), r.width - padX * 2f, r.height - Theme.S(16)), text, Theme.Desc);
            }
            _rowsInSection++;
        }

        // Small caption inside a section (sub-group).
        internal static void Header(string text)
        {
            if (!Show(text, null, false)) return;
            Rect r = GUILayoutUtility.GetRect(0f, Theme.S(30), GUILayout.ExpandWidth(true));
            if (Repaint)
                GUI.Label(new Rect(r.x + Theme.S(14), r.y + Theme.S(10), r.width - Theme.S(28), Theme.S(16)), text.ToUpperInvariant(), Theme.SectionTitle);
            _rowsInSection = 0;
        }

        internal static void Separator()
        {
            if (Filtering) return;
            GUILayout.Space(Theme.S(6));
        }

        /* ═══════════════════════════════════════════════════════════════
         *  Action rows (whole row is the button)
         * ═══════════════════════════════════════════════════════════════ */

        internal enum Kind { Normal, Primary, Danger }

        // Action row: the whole row runs the action; a small play button on the right says so.
        internal static bool Action(string text, string desc = null, Kind kind = Kind.Normal) => ActionRow(text, desc, kind, false);

        // opensList: the row opens a picker, so it ends with a chevron instead of the play button.
        // id: stable label for search, Featured and keybinds when the shown text changes (ConfirmButton).
        private static bool ActionRow(string text, string desc, Kind kind, bool opensList, string id = null)
        {
            if (!Show(id ?? text, desc, true, true)) return false;
            float bw = opensList ? Theme.S(16) : Theme.S(28);
            GUIStyle st = kind == Kind.Primary ? Theme.ActionLabelAccent : kind == Kind.Danger ? Theme.ActionLabelDanger : Theme.ActionLabel;
            Rect r = Row(text, desc, bw, true, st, id ?? text);
            if (Repaint)
            {
                bool hover = r.Contains(Event.current.mousePosition);
                if (opensList)
                    Icons.Draw(Ctrl(r, bw, bw), Icons.Right, hover ? Theme.Text : Theme.Faint);
                else
                {
                    Rect b = Ctrl(r, bw, bw);
                    Color icon;
                    if (kind == Kind.Primary) { Theme.Rounded(b, 7f, Theme.Accent); icon = Theme.AccentText; }
                    else if (kind == Kind.Danger) { Theme.DangerBtn.Draw(b, GUIContent.none, hover, false, false, false); icon = Theme.Danger; }
                    else { Theme.Rounded(b, 7f, hover ? Theme.CtrlHover : Theme.Ctrl); icon = hover ? Theme.Text : Theme.Sub; }
                    Icons.DrawCentered(b, Icons.Play, 12, icon);
                }
            }
            EndRowPress();
            return Clicked(r);
        }

        internal static bool Button(string text) => Action(text);
        internal static void Button(string text, Action onClick) { if (Action(text)) onClick?.Invoke(); }
        internal static void Button(string text, string desc, Action onClick) { if (Action(text, desc)) onClick?.Invoke(); }
        internal static void PrimaryButton(string text, Action onClick) { if (Action(text, null, Kind.Primary)) onClick?.Invoke(); }
        internal static void DangerButton(string text, Action onClick) { if (Action(text, null, Kind.Danger)) onClick?.Invoke(); }

        private static string _armed;

        internal static void ConfirmButton(string id, string text, Action onConfirm, string desc = null)
        {
            bool armed = _armed == id;
            if (ActionRow(armed ? "Click again: " + text : text, desc, Kind.Danger, false, text))
            {
                if (armed) { _armed = null; onConfirm?.Invoke(); }
                else _armed = id;
            }
        }

        // Rows are built only when the picker opens, not on every OnGUI call.
        internal static void PickerButton(string text, string title, Func<List<ListPicker.Row>> rows, string desc = null)
        {
            if (ActionRow(text.TrimEnd('.'), desc, Kind.Normal, true)) ListPicker.Open(title, rows());
        }

        // Row with a chevron that opens a list of its own making (ItemPicker).
        internal static void ListButton(string text, Action open, string desc = null)
        {
            if (ActionRow(text, desc, Kind.Normal, true)) open?.Invoke();
        }

        // Label + description with several buttons on the right.
        internal static void Buttons(string label, string desc, params (string text, Action onClick)[] buttons)
        {
            if (!Show(label, desc, true, true)) return;
            float gap = Theme.S(6), h = Theme.S(28);
            float w = 0f;
            var widths = new float[buttons.Length];
            for (int i = 0; i < buttons.Length; i++) { widths[i] = TextW(Theme.Button, buttons[i].text) + Theme.S(4); w += widths[i] + (i > 0 ? gap : 0); }
            Rect r = Row(label, desc, w);
            Rect c = Ctrl(r, w, h);
            float x = c.x;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (PressButton(new Rect(x, c.y, widths[i], h), buttons[i].text, Theme.Button, label + "/" + buttons[i].text)) buttons[i].onClick?.Invoke();
                x += widths[i] + gap;
            }
        }

        // Equal-width buttons across the row, no label.
        internal static void ButtonGrid(params (string text, Action onClick)[] buttons)
        {
            if (Filtering) return;
            Undim();
            if (_inSection && !_sectionOpen) OpenSection();
            Rect r = GUILayoutUtility.GetRect(0f, Theme.S(44), GUILayout.ExpandWidth(true));
            if (Repaint && _sectionOpen && _rowsInSection > 0)
                Theme.Fill(new Rect(r.x + Theme.S(14), r.y, r.width - Theme.S(28), Mathf.Max(1f, Theme.S(1))), Theme.Line);
            float pad = Theme.S(14), gap = Theme.S(6), h = Theme.S(28);
            float w = (r.width - pad * 2f - gap * (buttons.Length - 1)) / buttons.Length;
            for (int i = 0; i < buttons.Length; i++)
                if (PressButton(new Rect(r.x + pad + i * (w + gap), r.y + (r.height - h) * 0.5f, w, h), buttons[i].text, Theme.Button, "grid/" + buttons[i].text))
                    buttons[i].onClick?.Invoke();
            _rowsInSection++;
        }

        /* ═══════════════════════════════════════════════════════════════
         *  Toggle
         * ═══════════════════════════════════════════════════════════════ */

        internal static bool Toggle(string label, bool value, string desc = null) => Toggle(label, value, KeyCode.None, desc);

        internal static bool Toggle(string label, bool value, KeyCode key, string desc = null)
        {
            if (!Show(label, desc)) return value;
            float sw = Theme.S(34), sh = Theme.S(20);
            string keyText = key != KeyCode.None ? key.ToString() : null;
            float kw = keyText != null ? TextW(Theme.Kbd, keyText) + Theme.S(2) : 0f;
            Rect r = Row(label, desc, sw + (kw > 0 ? kw + Theme.S(10) : 0f));
            if (Repaint)
            {
                // Knob slides and colours cross-fade over 160 ms; with Reduce motion only the colours fade.
                Rect t = Ctrl(r, sw, sh);
                float a = Anim.To("toggle:" + label, value ? 1f : 0f, Anim.Toggle, Anim.EaseOut);
                float ka = Anim.Reduce ? (value ? 1f : 0f) : a;
                Theme.RoundedTint(t, 10f, Color.Lerp(Theme.CtrlHover, Theme.On, a));
                float k = Theme.S(14), m = Theme.S(3);
                Rect knob = new Rect(Mathf.Round(Mathf.Lerp(t.x + m, t.xMax - k - m, ka)), t.y + (sh - k) * 0.5f, k, k);
                Theme.RoundedTint(knob, 7f, Color.Lerp(Theme.Sub, Color.white, a));
                if (kw > 0)
                    Theme.Kbd.Draw(new Rect(t.x - Theme.S(10) - kw, t.y, kw, sh), C(keyText), false, false, false, false);
            }
            return Clicked(r) ? !value : value;
        }

        /* ═══════════════════════════════════════════════════════════════
         *  Slider
         * ═══════════════════════════════════════════════════════════════ */

        internal static float Slider(string label, float value, float min, float max, string desc = null, string format = "0.##")
        {
            if (!Show(label, desc)) return value;
            float vw = Theme.S(48), tw = Theme.S(150), gap = Theme.S(10);
            Rect r = Row(label, desc, vw + gap + tw);
            Rect c = Ctrl(r, vw + gap + tw, Theme.S(20));
            Rect track = new Rect(c.xMax - tw, c.y, tw, c.height);
            Rect hit = new Rect(track.x - Theme.S(7), r.y, track.width + Theme.S(14), r.height);

            int id = GUIUtility.GetControlID(FocusType.Passive);

            // Ctrl+click types a value. Above the slider's maximum is allowed, below its minimum is not.
            // "0%" sliders show a fraction as percent, so the typed number is a percent too.
            bool percent = format.EndsWith("%") && !format.EndsWith("'%'");
            Rect field = new Rect(c.x, Mathf.Round(r.y + (r.height - Theme.S(28)) * 0.5f), c.width, Theme.S(28));
            string shown = (percent ? value * 100f : value).ToString("0.##", CultureInfo.InvariantCulture);
            if (NumberField("num:" + (_rowKey ?? label), field, shown, false, out double? typed)) return value;
            if (typed.HasValue) return Mathf.Max(min, (float)(percent ? typed.Value / 100.0 : typed.Value));

            Event e = Event.current;
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (e.button == 0 && !_rowOff && hit.Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        value = Mathf.Lerp(min, max, Mathf.Clamp01((e.mousePosition.x - track.x) / track.width));
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        value = Mathf.Lerp(min, max, Mathf.Clamp01((e.mousePosition.x - track.x) / track.width));
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id) { GUIUtility.hotControl = 0; e.Use(); }
                    break;
            }

            if (Repaint)
            {
                float t = Mathf.InverseLerp(min, max, value);
                GUI.Label(new Rect(c.x, c.y, vw, c.height), value.ToString(format, CultureInfo.InvariantCulture), Theme.Value);
                float th = Mathf.Max(2f, Theme.S(4));
                Rect bar = new Rect(track.x, track.y + (track.height - th) * 0.5f, track.width, th);
                Theme.Rounded(bar, 2f, Theme.CtrlHover);
                if (t > 0.001f) Theme.Rounded(new Rect(bar.x, bar.y, Mathf.Max(th, bar.width * t), th), 2f, Theme.Accent);
                float k = Theme.S(14);
                Theme.Rounded(new Rect(bar.x + bar.width * t - k * 0.5f, track.y + (track.height - k) * 0.5f, k, k), 7f, Color.white);
            }
            return value;
        }

        // True while any slider is being dragged.
        internal static bool Dragging => GUIUtility.hotControl != 0;

        /* ═══════════════════════════════════════════════════════════════
         *  Steppers
         * ═══════════════════════════════════════════════════════════════ */

        private static int Stepper(Rect c, int value, int step, int min, int max, string key)
        {
            // Ctrl+click types a value, kept within the stepper's range.
            if (NumberField("num:" + key, c, value.ToString(CultureInfo.InvariantCulture), true, out double? typed)) return value;
            if (typed.HasValue) return (int)Math.Max(min, Math.Min(max, Math.Round(typed.Value)));

            Theme.Rounded(c, 7f, Theme.Ctrl);
            float bw = Theme.S(28);
            Rect minus = new Rect(c.x, c.y, bw, c.height), plus = new Rect(c.xMax - bw, c.y, bw, c.height);
            Vector2 m = Event.current.mousePosition;
            if (Repaint)
            {
                Icons.DrawCentered(minus, Icons.Minus, 14, minus.Contains(m) ? Theme.Text : Theme.Sub);
                Icons.DrawCentered(plus, Icons.Plus, 14, plus.Contains(m) ? Theme.Text : Theme.Sub);
                GUI.Label(new Rect(c.x + bw, c.y, c.width - bw * 2f, c.height), value.ToString("N0", CultureInfo.InvariantCulture), Theme.ValueCenter);
            }
            if (Clicked(minus)) value = (int)Math.Max(min, Math.Min(max, (long)value - step));
            if (Clicked(plus)) value = (int)Math.Max(min, Math.Min(max, (long)value + step));
            return value;
        }

        internal static int IntStepper(string label, int value, int step, int min = int.MinValue, int max = int.MaxValue, string desc = null)
        {
            if (!Show(label, desc)) return value;
            float w = Theme.S(120), h = Theme.S(28);
            Rect r = Row(label, desc, w);
            return Stepper(Ctrl(r, w, h), value, step, min, max, _rowKey ?? label);
        }

        // Stepper and its action button in one row: "Money   [- 1,000 +] [Give]".
        internal static int StepperAction(string label, int value, int step, int min, int max, string buttonText, Action<int> onClick, string desc = null)
        {
            if (!Show(label, desc, true, true)) return value;
            float sw = Theme.S(120), h = Theme.S(28), gap = Theme.S(6);
            float bw = Mathf.Max(Theme.S(56), TextW(Theme.Button, buttonText) + Theme.S(4));
            Rect r = Row(label, desc, sw + gap + bw);
            Rect c = Ctrl(r, sw + gap + bw, h);
            value = Stepper(new Rect(c.x, c.y, sw, h), value, step, min, max, _rowKey ?? label);
            if (PressButton(new Rect(c.xMax - bw, c.y, bw, h), buttonText, Theme.Button, label + "/" + buttonText)) onClick?.Invoke(value);
            return value;
        }

        /* ═══════════════════════════════════════════════════════════════
         *  Typed numbers: Ctrl+click a slider or a stepper. Enter or a click
         *  elsewhere applies, Esc cancels.
         * ═══════════════════════════════════════════════════════════════ */

        private const string EditControl = "poppy_num_edit";
        private static string _editKey, _editText;
        private static bool _editFocus;
        private static int _editFrame, _editSeen;

        internal static void CancelEdit()
        {
            if (_editKey == null) return;
            _editKey = null;
            GUIUtility.keyboardControl = 0;
        }

        // Call after the page: a field whose row was not drawn (other page, row hidden) is dropped.
        internal static void EndEditFrame()
        {
            if (_editKey != null && Repaint && _editSeen != Time.frameCount) CancelEdit();
        }

        // True while the text field stands in for the control. typed: the value, on the event it is applied.
        private static bool NumberField(string key, Rect field, string text, bool integer, out double? typed)
        {
            typed = null;
            Event e = Event.current;
            if (_editKey != key)
            {
                if (e.type != EventType.MouseDown || e.button != 0 || !(e.control || e.command) || _rowOff || !field.Contains(e.mousePosition))
                    return false;
                _editKey = key;
                _editText = text;
                _editFocus = true;
                _editFrame = Time.frameCount;
                e.Use();
            }
            _editSeen = Time.frameCount;

            bool apply = false;
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)) { apply = true; e.Use(); }
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { e.Use(); CancelEdit(); return false; }
            // A click anywhere else, this window or not (raw input: another control may have used the event).
            else if (e.type == EventType.Repaint && Time.frameCount != _editFrame && UnityEngine.Input.GetMouseButtonDown(0) && !field.Contains(e.mousePosition))
                apply = true;
            if (apply)
            {
                if (TryParseNumber(_editText, integer, out double v)) typed = v;
                CancelEdit();
                return false;
            }

            GUI.SetNextControlName(EditControl);
            _editText = GUI.TextField(field, _editText ?? "", 24, Theme.Input);
            if (_editFocus && Repaint) { GUI.FocusControl(EditControl); _editFocus = false; }
            return true;
        }

        // Accepts what the control shows: "2.5x", "+40%", "1,000", "2,5".
        private static bool TryParseNumber(string s, bool integer, out double v)
        {
            s = (s ?? "").Trim().Replace(" ", "").Replace("\u00A0", "").TrimStart('+').TrimEnd('x', 'X', '%', 's', 'm', '\u00B0');
            if (integer || s.Contains(".")) s = s.Replace(",", "");
            else s = s.Replace(',', '.');
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) && !double.IsNaN(v) && !double.IsInfinity(v);
        }

        /* ═══════════════════════════════════════════════════════════════
         *  Dropdown. The list is drawn by MenuRoot on top of the page
         *  (DropdownInput before the content, DropdownDraw after it).
         * ═══════════════════════════════════════════════════════════════ */

        private static string _ddKey, _ddPickedKey;
        private static string[] _ddOptions;
        private static int _ddIndex, _ddPicked;
        private static Rect _ddAnchor; // screen space

        private static float _ddOpenedAt, _ddClosedAt = -10f, _ddCloseFrom;

        internal static bool DropdownOpen => _ddKey != null;

        // Instant close (tab change): the list must not fade out over another page.
        internal static void CloseDropdown() { _ddKey = null; _ddClosedAt = -10f; }

        // Closing after a click or scroll: fades out from wherever the opening got to.
        private static void CloseDropdownAnimated()
        {
            _ddCloseFrom = Anim.Progress(_ddOpenedAt, Anim.DropdownIn, Anim.EaseOut);
            _ddKey = null;
            _ddClosedAt = Anim.Now;
        }

        // "Priority   [Closest to Crosshair  v]". Returns the selected index.
        internal static int Dropdown(string label, int index, string[] options, string desc = null)
        {
            if (!Show(label, desc)) return index;
            string key = label;
            if (_ddPickedKey == key) { index = _ddPicked; _ddPickedKey = null; }
            index = Mathf.Clamp(index, 0, options.Length - 1);

            float tw = 0f;
            foreach (string o in options) tw = Mathf.Max(tw, TextW(Theme.Label, o));
            float iw = Theme.S(14), h = Theme.S(28);
            float w = Theme.S(12) + tw + Theme.S(10) + iw + Theme.S(10);
            Rect r = Row(label, desc, w);
            Rect c = Ctrl(r, w, h);
            bool open = _ddKey == key;

            if (Repaint)
            {
                bool hover = c.Contains(Event.current.mousePosition);
                Theme.Rounded(c, 7f, open || hover ? Theme.CtrlHover : Theme.Ctrl);
                GUI.Label(new Rect(c.x + Theme.S(12), c.y, tw + Theme.S(4), h), options[index], Theme.Label);
                Icons.DrawCentered(new Rect(c.xMax - Theme.S(10) - iw, c.y, iw, h), Icons.Down, 14, open || hover ? Theme.Text : Theme.Sub);
                if (open)
                {
                    Vector2 p = GUIUtility.GUIToScreenPoint(c.position);
                    _ddAnchor = new Rect(p.x, p.y, c.width, c.height);
                    _ddIndex = index;
                }
            }
            if (Clicked(c))
            {
                Vector2 p = GUIUtility.GUIToScreenPoint(c.position);
                _ddAnchor = new Rect(p.x, p.y, c.width, c.height);
                _ddKey = key;
                _ddOptions = options;
                _ddIndex = index;
                _ddOpenedAt = Anim.Now;
                _ddClosedAt = -10f;
            }
            return index;
        }

        private static Rect DropdownList(Vector2 origin, float windowH)
        {
            float itemH = Theme.S(30), pad = Theme.S(4);
            float w = _ddAnchor.width;
            foreach (string o in _ddOptions) w = Mathf.Max(w, TextW(Theme.Label, o) + Theme.S(48));
            float h = pad * 2f + itemH * _ddOptions.Length;
            float x = _ddAnchor.xMax - origin.x - w;
            float y = _ddAnchor.yMax - origin.y + Theme.S(4);
            if (y + h > windowH - Theme.S(8)) y = _ddAnchor.y - origin.y - Theme.S(4) - h;
            return new Rect(x, y, w, h);
        }

        // Call at the start of the window: a click anywhere goes to the open list (pick or dismiss).
        internal static void DropdownInput(Vector2 origin, float windowH)
        {
            if (_ddKey == null) return;
            Event e = Event.current;
            if (e.type == EventType.MouseDown)
            {
                Rect list = DropdownList(origin, windowH);
                if (list.Contains(e.mousePosition))
                {
                    int i = Mathf.FloorToInt((e.mousePosition.y - list.y - Theme.S(4)) / Theme.S(30));
                    if (i >= 0 && i < _ddOptions.Length) { _ddPickedKey = _ddKey; _ddPicked = i; }
                }
                CloseDropdownAnimated();
                e.Use();
            }
            else if (e.type == EventType.ScrollWheel) CloseDropdownAnimated();
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                CloseDropdown(); // keyboard: no animation
                e.Use();
            }
        }

        // Call after the content so the list covers it.
        internal static void DropdownDraw(Vector2 origin, float windowH)
        {
            if (!Repaint || _ddOptions == null) return;
            bool open = _ddKey != null;
            float a;
            if (open) a = Anim.Progress(_ddOpenedAt, Anim.DropdownIn, Anim.EaseOut);
            else if (Anim.Now - _ddClosedAt < Anim.DropdownOut) a = _ddCloseFrom * (1f - Anim.Progress(_ddClosedAt, Anim.DropdownOut, Anim.EaseOut));
            else return;

            // Opens out of its trigger: fade in and grow from 95% around the corner nearest the trigger.
            Rect list = DropdownList(origin, windowH);
            bool above = list.y < _ddAnchor.y - origin.y;
            Vector2 pivot = new Vector2(list.xMax, above ? list.yMax : list.y);
            Color prev = GUI.color;
            GUI.color = Anim.Alpha(prev, a);
            Matrix4x4 saved = Anim.Begin(pivot, Anim.Reduce ? 1f : Mathf.Lerp(0.95f, 1f, a));

            Theme.PopupBg.Draw(list, false, false, false, false);
            float itemH = Theme.S(30), pad = Theme.S(4);
            Vector2 m = Event.current.mousePosition;
            for (int i = 0; i < _ddOptions.Length; i++)
            {
                Rect it = new Rect(list.x + pad, list.y + pad + i * itemH, list.width - pad * 2f, itemH);
                bool sel = i == _ddIndex;
                if (open && it.Contains(m)) Theme.Rounded(it, 6f, Theme.CtrlHover);
                GUI.Label(new Rect(it.x + Theme.S(10), it.y, it.width - Theme.S(36), itemH), _ddOptions[i], sel ? Theme.LabelBold : Theme.Label);
                if (sel) Icons.DrawCentered(new Rect(it.xMax - Theme.S(26), it.y, Theme.S(16), itemH), Icons.Check, 14, Theme.Accent);
            }

            Anim.End(saved);
            GUI.color = prev;
        }

        // Segmented choice inside a row. Returns the selected index.
        internal static int Segmented(string label, int index, string[] options, string desc = null)
        {
            if (!Show(label, desc)) return index;
            float pad = Theme.S(3), h = Theme.S(28);
            var widths = new float[options.Length];
            float w = pad * 2f;
            for (int i = 0; i < options.Length; i++) { widths[i] = TextW(Theme.SegOn, options[i]) + Theme.S(22); w += widths[i]; }
            Rect r = Row(label, desc, w);
            Rect c = Ctrl(r, w, h);
            return SegmentedBar(c, index, options, widths, label);
        }

        private static GUIStyle _segOnText, _segOnSrc;

        // SegOn without its background: the label of the selected segment, drawn over the sliding pill.
        private static GUIStyle SegOnText()
        {
            if (_segOnSrc == Theme.SegOn && _segOnText != null) return _segOnText;
            _segOnSrc = Theme.SegOn;
            _segOnText = new GUIStyle(Theme.SegOn);
            foreach (GUIStyleState st in new[] { _segOnText.normal, _segOnText.hover, _segOnText.active, _segOnText.focused,
                                                 _segOnText.onNormal, _segOnText.onHover, _segOnText.onActive, _segOnText.onFocused })
                st.background = null;
            return _segOnText;
        }

        // The selected pill slides to the new segment over 200 ms (ease-in-out: it moves on screen);
        // with Reduce motion it jumps.
        internal static int SegmentedBar(Rect c, int index, string[] options, float[] widths, string key = null)
        {
            float pad = Theme.S(3);
            Theme.Rounded(c, 9f, Theme.Rail);
            int picked = index;
            float x = c.x + pad, ax = x, aw = 0f;
            for (int i = 0; i < options.Length; i++)
            {
                Rect b = new Rect(x, c.y + pad, widths[i], c.height - pad * 2f);
                if (i == index) { ax = x; aw = widths[i]; }
                else if (GUI.Button(b, options[i], Theme.Seg)) picked = i;
                x += widths[i];
            }

            string k = "seg:" + (key ?? string.Join("|", options));
            bool snap = Anim.Reduce;
            float px = Anim.To(k + ":x", ax - c.x, Anim.Slide, Anim.EaseInOut, snap);
            float pw = Anim.To(k + ":w", aw, Anim.Slide, Anim.EaseInOut, snap);
            Rect active = new Rect(ax, c.y + pad, aw, c.height - pad * 2f);
            if (Repaint) Theme.SegOn.Draw(new Rect(Mathf.Round(c.x + px), active.y, Mathf.Round(pw), active.height), false, false, false, false);
            if (aw > 0f && GUI.Button(active, options[index], SegOnText())) picked = index;
            return picked;
        }

        internal static float[] SegmentWidths(string[] options)
        {
            var widths = new float[options.Length];
            for (int i = 0; i < options.Length; i++) widths[i] = TextW(Theme.SegOn, options[i]) + Theme.S(28);
            return widths;
        }

        /* ═══════════════════════════════════════════════════════════════
         *  Keybind
         * ═══════════════════════════════════════════════════════════════ */

        internal static void KeybindRow(string label, ConfigEntry<KeyCode> entry, string desc = null)
        {
            if (!Show(label, desc)) return;
            bool listening = Rebind.Listening == entry;
            string text = listening ? "Press a key" : entry.Value == KeyCode.None ? "Not set" : entry.Value.ToString();
            float xw = Theme.S(22), h = Theme.S(28);
            float w = TextW(Theme.Label, text) + Theme.S(12) + xw + Theme.S(4);
            Rect r = Row(label, desc, w);
            Rect c = Ctrl(r, w, h);
            Rect x = new Rect(c.xMax - xw - Theme.S(4), c.y, xw, h);
            Rect main = new Rect(c.x, c.y, c.width - xw - Theme.S(4), h);
            if (Repaint)
            {
                Vector2 m = Event.current.mousePosition;
                Theme.Rounded(c, 7f, listening ? Theme.AccentSoft : main.Contains(m) ? Theme.CtrlHover : Theme.Ctrl);
                GUIStyle st = listening ? Theme.ActionLabelAccent : Theme.Label;
                GUI.Label(new Rect(c.x + Theme.S(12), c.y, main.width - Theme.S(12), h), text, st);
                Icons.DrawCentered(x, Icons.X, 12, x.Contains(m) ? Theme.Text : Theme.Faint);
            }
            if (Clicked(x))
            {
                entry.Value = KeyCode.None;
                if (listening) Rebind.Cancel();
            }
            else if (Clicked(main))
            {
                if (listening) Rebind.Cancel(); else Rebind.Capture(entry);
            }
        }

        /* ═══════════════════════════════════════════════════════════════
         *  Free-form rows for module-specific layouts (lists, editors)
         * ═══════════════════════════════════════════════════════════════ */

        // Starts a horizontal row inside the current section. Returns false while searching (skip the row).
        internal static bool BeginRow()
        {
            if (Filtering) return false;
            Undim();
            if (_inSection && !_sectionOpen) OpenSection();
            if (_sectionOpen && _rowsInSection > 0)
            {
                Rect line = GUILayoutUtility.GetRect(0f, Mathf.Max(1f, Theme.S(1)), GUILayout.ExpandWidth(true));
                if (Repaint) Theme.Fill(new Rect(line.x + Theme.S(14), line.y, line.width - Theme.S(28), line.height), Theme.Line);
            }
            GUILayout.BeginHorizontal(Theme.RowPad, GUILayout.MinHeight(Theme.S(44)));
            return true;
        }

        internal static void EndRow()
        {
            GUILayout.EndHorizontal();
            _rowsInSection++;
        }

        internal static bool SmallButton(string text, Kind kind = Kind.Normal, float width = 0f)
        {
            GUIStyle st = kind == Kind.Primary ? Theme.Primary : kind == Kind.Danger ? Theme.DangerBtn : Theme.Button;
            Rect r = width > 0f
                ? GUILayoutUtility.GetRect(C(text), st, GUILayout.Height(Theme.S(28)), GUILayout.Width(Theme.S(width)))
                : GUILayoutUtility.GetRect(C(text), st, GUILayout.Height(Theme.S(28)));
            return PressButton(r, text, st, "small/" + text + "/" + _fieldCounter);
        }

        internal static string TextField(string value, string placeholder = null, float width = 0f)
        {
            string name = "poppy_tf_" + (_fieldCounter++);
            GUI.SetNextControlName(name);
            string result = width > 0f
                ? GUILayout.TextField(value ?? "", Theme.Input, GUILayout.Height(Theme.S(28)), GUILayout.Width(Theme.S(width)))
                : GUILayout.TextField(value ?? "", Theme.Input, GUILayout.Height(Theme.S(28)), GUILayout.ExpandWidth(true));
            if (Repaint && string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(placeholder) && GUI.GetNameOfFocusedControl() != name)
            {
                Rect r = GUILayoutUtility.GetLastRect();
                GUI.Label(new Rect(r.x + Theme.S(10), r.y, r.width - Theme.S(20), r.height), placeholder, Theme.Placeholder);
            }
            return result;
        }

        // A panel with a single line of text, for pages that have nothing to show yet.
        internal static void EmptyState(string text)
        {
            if (Filtering) return;
            SectionBegin(null);
            Note(text);
            SectionEnd();
        }

        internal static void RowLabel(string text) =>
            GUILayout.Label(text, Theme.Label, GUILayout.Height(Theme.S(28)), GUILayout.ExpandWidth(true));

        internal static void RowSpace() => GUILayout.Space(Theme.S(6));

        /* ═══════════════════════════════════════════════════════════════
         *  Resize grip (bottom-right corner of a window)
         * ═══════════════════════════════════════════════════════════════ */

        // Hit area in the window's bottom-right corner.
        internal static Rect GripRect(float w, float h) => new Rect(w - Theme.S(22), h - Theme.S(22), Theme.S(22), Theme.S(22));

        // Six dots in a corner triangle: faint at rest, brighter on hover, accent while dragging.
        internal static void ResizeGrip(Rect grip, bool dragging)
        {
            if (!Repaint) return;
            bool hover = grip.Contains(Event.current.mousePosition);
            Color c = dragging ? Theme.Accent : hover ? Theme.Text : Theme.Faint;
            if (hover && !dragging) Theme.Rounded(new Rect(grip.x + Theme.S(2), grip.y + Theme.S(2), grip.width - Theme.S(5), grip.height - Theme.S(5)), 6f, Theme.Ctrl);

            float dot = Mathf.Max(2f, Theme.S(2.5f)), step = Theme.S(4.5f);
            float right = grip.xMax - Theme.S(6) - dot, bottom = grip.yMax - Theme.S(6) - dot;
            for (int row = 0; row < 3; row++)
                for (int col = 0; col <= row; col++)
                    Theme.Rounded(new Rect(right - col * step, bottom - (2 - row) * step, dot, dot), 2f, c);
        }

        /* ═══════════════════════════════════════════════════════════════
         *  Tooltip
         * ═══════════════════════════════════════════════════════════════ */

        internal static void Tooltip(Rect r, string text)
        {
            if (Repaint && r.Contains(Event.current.mousePosition)) NextTooltip = text;
        }
    }
}
