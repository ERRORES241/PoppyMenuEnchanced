using System;
using System.Collections.Generic;
using UnityEngine;

namespace PoppyMenu
{
    // Right-click menu of a row (Featured, keybinds) or of a rail icon (keybind that opens the tab).
    // Drawn by MenuRoot over the page like the dropdown list: Input before the content, Draw after it.
    internal static class RowMenu
    {
        private struct Item
        {
            internal string Text, Icon, Right;
            internal bool Danger, Disabled, Sep;
            internal Action Act;
        }

        private static readonly List<Item> _items = new List<Item>();
        private static string _title;
        private static bool _open;
        private static Vector2 _at; // screen space, where the click was
        private static float _openedAt, _closedAt = -10f, _closeFrom;

        private static string _bindingKey; // row (or "#tab:Name") waiting for a key

        internal static bool IsOpen => _open;

        /* ─────── Opening ───────────────────────────────────────────── */

        internal static void OpenRow(string key, string label, string bindId, string bindName, Vector2 screen)
        {
            _items.Clear();
            _title = label.Trim();
            bool featured = FeaturedStore.Has(key);
            Add(featured ? "Remove from Featured" : "Add to Featured", Icons.Star, null, () => FeaturedStore.Toggle(key));

            var binds = Binds(label, bindId, bindName);
            _items.Add(new Item { Sep = true });
            if (binds.Count == 0)
                _items.Add(new Item { Text = "This row has no keybind", Disabled = true });
            foreach (var (id, name) in binds) AddBind(key, id, binds.Count > 1 ? name : null, _title);
            Show(screen);
        }

        internal static void OpenTab(string tab, Vector2 screen)
        {
            _items.Clear();
            _title = tab + " tab";
            AddBind("#tab:" + tab, ActionRegistry.TabId(tab), null, _title);
            Show(screen);
        }

        // Bind / Change, plus Remove when a key is set. part: action name when the row has several.
        private static void AddBind(string key, string id, string part, string what)
        {
            KeyCode k = BindStore.KeyFor(id);
            string verb = k == KeyCode.None ? "Bind key" : "Change key";
            string notifyName = part ?? what;
            Add(part != null ? verb + ": " + part : verb, Icons.Keyboard, k == KeyCode.None ? null : k.ToString(), () =>
            {
                _bindingKey = key;
                if (key.StartsWith("#tab:")) Notify.Push("Press a key for the " + what + ". Esc cancels.");
                Rebind.Capture(kc => { BindStore.Set(id, kc); _bindingKey = null; Notify.Push(notifyName + ": " + kc); });
            });
            if (k != KeyCode.None)
                _items.Add(new Item { Text = part != null ? "Remove key: " + part : "Remove key", Icon = Icons.X, Danger = true, Act = () => BindStore.Unset(id) });
        }

        private static void Add(string text, string icon, string right, Action act) =>
            _items.Add(new Item { Text = text, Icon = icon, Right = right, Act = act });

        private static void Show(Vector2 screen)
        {
            _at = screen;
            _open = true;
            _openedAt = Anim.Now;
            _closedAt = -10f;
        }

        // Instant close (tab change, Esc, another right-click): no fade over what comes next.
        internal static void Close() { _open = false; _closedAt = -10f; }

        private static void CloseAnimated()
        {
            _closeFrom = Anim.Progress(_openedAt, Anim.DropdownIn, Anim.EaseOut);
            _open = false;
            _closedAt = Anim.Now;
        }

        /* ─────── Row badges ────────────────────────────────────────── */

        private static readonly List<(string id, string name)> _tmp = new List<(string, string)>();

        private static List<(string id, string name)> Binds(string label, string bindId, string bindName)
        {
            _tmp.Clear();
            if (bindId != null) _tmp.Add((bindId, bindName));
            else foreach (PoppyAction a in ActionRegistry.ForRow(label)) _tmp.Add((a.Id, a.Name));
            return _tmp;
        }

        internal static bool IsBinding(string key) => Rebind.IsActive && _bindingKey == key;

        // Key(s) shown next to a row label, or null.
        internal static string BadgeText(string key, string label, string bindId)
        {
            if (IsBinding(key)) return "Press a key";
            if (bindId != null)
            {
                KeyCode k = BindStore.KeyFor(bindId);
                return k == KeyCode.None ? null : k.ToString();
            }
            string text = null;
            foreach (PoppyAction a in ActionRegistry.ForRow(label))
            {
                KeyCode k = BindStore.KeyFor(a.Id);
                if (k != KeyCode.None) text = text == null ? k.ToString() : text + " " + k;
            }
            return text;
        }

        /* ─────── Layout, input, drawing ────────────────────────────── */

        private static float ItemH => Theme.S(30);
        private static float SepH => Theme.S(9);
        private static float TitleH => Theme.S(26);

        private static Rect Layout(Vector2 origin, float W, float H, out bool left, out bool up)
        {
            float pad = Theme.S(4), w = Theme.S(180);
            float h = pad * 2f + TitleH;
            w = Mathf.Max(w, Theme.Small.CalcSize(new GUIContent(_title)).x + Theme.S(24));
            foreach (Item it in _items)
            {
                if (it.Sep) { h += SepH; continue; }
                h += ItemH;
                float tw = Theme.Label.CalcSize(new GUIContent(it.Text)).x + Theme.S(44);
                if (it.Right != null) tw += Theme.Kbd.CalcSize(new GUIContent(it.Right)).x + Theme.S(14);
                w = Mathf.Max(w, tw);
            }
            Vector2 p = _at - origin;
            left = p.x + w > W - Theme.S(6);
            up = p.y + h > H - Theme.S(6);
            float x = left ? p.x - w : p.x, y = up ? p.y - h : p.y;
            return new Rect(Mathf.Round(Mathf.Max(Theme.S(4), x)), Mathf.Round(Mathf.Max(Theme.S(4), y)), w, h);
        }

        // Item at a window-space point, or -1.
        private static int HitItem(Rect box, Vector2 m)
        {
            if (!box.Contains(m)) return -1;
            float y = box.y + Theme.S(4) + TitleH;
            for (int i = 0; i < _items.Count; i++)
            {
                float h = _items[i].Sep ? SepH : ItemH;
                if (m.y >= y && m.y < y + h) return _items[i].Sep || _items[i].Disabled ? -1 : i;
                y += h;
            }
            return -1;
        }

        // Call at the start of the window: while open, clicks go to the menu.
        internal static void Input(Vector2 origin, float W, float H)
        {
            if (!_open) return;
            Event e = Event.current;
            if (e.type == EventType.MouseDown)
            {
                Rect box = Layout(origin, W, H, out _, out _);
                if (e.button == 1 && !box.Contains(e.mousePosition)) { Close(); return; } // let the row under it open its own
                int i = e.button == 0 ? HitItem(box, e.mousePosition) : -1;
                if (box.Contains(e.mousePosition) && i < 0 && e.button == 0) { e.Use(); return; }
                CloseAnimated();
                if (i >= 0) _items[i].Act?.Invoke();
                e.Use();
            }
            else if (e.type == EventType.ScrollWheel) CloseAnimated();
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                Close(); // keyboard: no animation
                e.Use();
            }
        }

        // Call after the content. Opens like the dropdown: fade in and grow from 95% out of the clicked corner.
        internal static void Draw(Vector2 origin, float W, float H)
        {
            if (Event.current.type != EventType.Repaint || _items.Count == 0) return;
            float a;
            if (_open) a = Anim.Progress(_openedAt, Anim.DropdownIn, Anim.EaseOut);
            else if (Anim.Now - _closedAt < Anim.DropdownOut) a = _closeFrom * (1f - Anim.Progress(_closedAt, Anim.DropdownOut, Anim.EaseOut));
            else return;

            Rect box = Layout(origin, W, H, out bool left, out bool up);
            Vector2 pivot = new Vector2(left ? box.xMax : box.x, up ? box.yMax : box.y);
            Color prev = GUI.color;
            GUI.color = Anim.Alpha(prev, a);
            Matrix4x4 saved = Anim.Begin(pivot, Anim.Reduce ? 1f : Mathf.Lerp(0.95f, 1f, a));

            Theme.PopupBg.Draw(box, false, false, false, false);
            float pad = Theme.S(4), x = box.x + pad, iw = box.width - pad * 2f;
            GUI.Label(new Rect(x + Theme.S(10), box.y + pad, iw - Theme.S(20), TitleH), _title, Theme.Small);
            float y = box.y + pad + TitleH;
            Vector2 m = Event.current.mousePosition;
            for (int i = 0; i < _items.Count; i++)
            {
                Item it = _items[i];
                if (it.Sep)
                {
                    Theme.Fill(new Rect(x + Theme.S(6), Mathf.Round(y + SepH * 0.5f), iw - Theme.S(12), Mathf.Max(1f, Theme.S(1))), Theme.Line);
                    y += SepH;
                    continue;
                }
                Rect r = new Rect(x, y, iw, ItemH);
                bool hover = _open && !it.Disabled && r.Contains(m);
                if (hover) Theme.Rounded(r, 6f, Theme.CtrlHover);
                Color ic = it.Disabled ? Theme.Faint : it.Danger ? Theme.Danger : hover ? Theme.Text : Theme.Sub;
                if (it.Icon != null) Icons.DrawCentered(new Rect(r.x + Theme.S(8), r.y, Theme.S(16), ItemH), it.Icon, 14, ic);
                GUIStyle st = it.Disabled ? Theme.Small : it.Danger ? Theme.ActionLabelDanger : Theme.Label;
                GUI.Label(new Rect(r.x + Theme.S(32), r.y, r.width - Theme.S(40), ItemH), it.Text, st);
                if (it.Right != null)
                {
                    float kw = Theme.Kbd.CalcSize(new GUIContent(it.Right)).x + Theme.S(2);
                    Theme.Kbd.Draw(new Rect(r.xMax - Theme.S(8) - kw, r.y + (ItemH - Theme.S(18)) * 0.5f, kw, Theme.S(18)), new GUIContent(it.Right), false, false, false, false);
                }
                y += ItemH;
            }

            Anim.End(saved);
            GUI.color = prev;
        }
    }
}
