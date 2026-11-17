using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace PoppyMenu
{
    // Main window: icon rail on the left, top bar (title, breadcrumb, search, close),
    // sub-page segments, scrolling page content and an active-effects footer.
    // Position is kept in screen pixels, size in logical units (multiplied by Theme.Scale).
    internal static class MenuRoot
    {
        private const int WindowId = 0x5B7A00;
        internal static bool Visible;
        internal static int ActiveTab;

        private const float DefaultW = 640f, DefaultH = 560f;
        private const float MinW = 560f, MinH = 420f, MaxW = 1200f, MaxH = 1000f;
        private const float RailW = 60f, TopH = 56f, FootH = 30f;
        private const string SearchControl = "poppy_menu_search";

        private static Vector2 _pos = new Vector2(40, 60);
        private static Vector2 _size = new Vector2(DefaultW, DefaultH);
        private static Rect _screenRect;
        private static Vector2 _scroll;
        private static List<TabGroup> _groups;
        private static bool _posLoaded, _resizing;
        private static string _search = "";

        private static readonly Dictionary<string, string> TabIcons = new Dictionary<string, string>
        {
            { "Aimbot", Icons.Crosshair }, { "Visuals", Icons.Eye }, { "Movement", Icons.Footprints },
            { "Player", Icons.User }, { "World", Icons.Globe }, { "Fun", Icons.Sparkles }, { "Settings", Icons.Settings },
        };

        /* ═══════════════════════════════════════════════════════════════
         *  Public API
         * ═══════════════════════════════════════════════════════════════ */

        internal static void Draw(List<TabGroup> groups)
        {
            _groups = groups;

            if (!_posLoaded)
            {
                _pos = new Vector2(ModConfig.WindowX.Value, ModConfig.WindowY.Value);
                _size = new Vector2(Mathf.Clamp(ModConfig.WindowW.Value, MinW, MaxW), Mathf.Clamp(ModConfig.WindowH.Value, MinH, MaxH));
                _posLoaded = true;
            }

            if (Visible)
            {
                float w = Theme.S(_size.x), h = Theme.S(_size.y);
                _pos.x = Mathf.Clamp(_pos.x, 80f - w, Screen.width - 80f);
                _pos.y = Mathf.Clamp(_pos.y, 0f, Screen.height - 40f);
                _screenRect = new Rect(_pos.x, _pos.y, w, h);
                Rect moved = GUI.Window(WindowId, _screenRect, DrawWindow, "", GUIStyle.none);
                _pos = moved.position;
            }

            if (ModConfig.ShowHud.Value && PlayerContext.InGame)
                DrawHud();

            ListPicker.Draw();

            if (Visible && !string.IsNullOrEmpty(Widgets.CurrentTooltip))
                DrawTooltip();

            if (Event.current.type == EventType.Repaint)
            {
                Widgets.CurrentTooltip = Widgets.NextTooltip;
                Widgets.NextTooltip = null;
            }
        }

        internal static void SaveLayout()
        {
            ModConfig.WindowX.Value = _pos.x;
            ModConfig.WindowY.Value = _pos.y;
            ModConfig.WindowW.Value = _size.x;
            ModConfig.WindowH.Value = _size.y;
        }

        internal static bool ContainsPoint(Vector2 screenPoint) => Visible && _screenRect.Contains(screenPoint);

        internal static void ResetPosition()
        {
            _pos = new Vector2(40, 60);
            _size = new Vector2(DefaultW, DefaultH);
            SaveLayout();
        }

        internal static string CurrentTabName =>
            _groups != null && _groups.Count > 0
                ? _groups[Mathf.Clamp(ActiveTab, 0, _groups.Count - 1)].Name : "";

        internal static void SelectTabByName(string name)
        {
            if (_groups == null) return;
            for (int i = 0; i < _groups.Count; i++)
                if (_groups[i].Name == name) { OpenTab(i); Visible = true; return; }
            for (int i = 0; i < _groups.Count; i++)
                for (int j = 0; j < _groups[i].Pages.Count; j++)
                    if (_groups[i].Pages[j].Name == name) { OpenTab(i); _groups[i].Page = j; Visible = true; return; }
        }

        private static void OpenTab(int i)
        {
            Widgets.CloseDropdown();
            if (ActiveTab != i) _scroll = Vector2.zero;
            ActiveTab = i;
            _search = "";
        }

        /* ═══════════════════════════════════════════════════════════════
         *  Window
         * ═══════════════════════════════════════════════════════════════ */

        private static void DrawWindow(int id)
        {
            float W = _screenRect.width, H = _screenRect.height;
            Theme.Box(new Rect(0, 0, W, H), Theme.WindowBg);
            if (_groups == null || _groups.Count == 0) { GUI.DragWindow(); return; }
            ActiveTab = Mathf.Clamp(ActiveTab, 0, _groups.Count - 1);

            Widgets.DropdownInput(_screenRect.position, H);
            Rect grip = Widgets.GripRect(W, H);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && grip.Contains(Event.current.mousePosition))
            {
                _resizing = true;
                Event.current.Use();
            }

            DrawRail(H);
            float x0 = Theme.S(RailW);
            DrawTopBar(x0, W);
            float top = DrawSegments(x0);
            DrawContent(new Rect(x0, top, W - x0, H - top - Theme.S(FootH)));
            DrawFooter(x0, W, H);
            Widgets.DropdownDraw(_screenRect.position, H);
            HandleResize(grip);

            GUI.DragWindow(new Rect(x0, 0, W - x0, Theme.S(TopH)));
        }

        /* ─────── Icon rail ─────────────────────────────────────────── */

        private static void DrawRail(float H)
        {
            float rw = Theme.S(RailW);
            if (Event.current.type == EventType.Repaint)
            {
                float edge = Mathf.Max(1f, Theme.S(1));
                Theme.RailBg.Draw(new Rect(edge, edge, rw - edge, H - edge * 2f), false, false, false, false);
                Theme.Fill(new Rect(rw - Theme.S(14), edge, Theme.S(14), H - edge * 2f), Theme.Rail);
            }

            Rect logo = new Rect(Theme.S(13), Theme.S(12), Theme.S(34), Theme.S(34));
            Theme.Rounded(logo, 9f, Theme.Accent);
            GUI.Label(logo, "P", Theme.Logo);

            float y = logo.yMax + Theme.S(14);
            Vector2 m = Event.current.mousePosition;
            for (int i = 0; i < _groups.Count; i++)
            {
                Rect r = new Rect(Theme.S(10), y, Theme.S(40), Theme.S(40));
                bool active = i == ActiveTab && string.IsNullOrEmpty(_search);
                bool hover = r.Contains(m);
                if (active) Theme.Rounded(r, 10f, Theme.AccentSoft);
                else if (hover) Theme.Rounded(r, 10f, Theme.RowHover);
                if (!TabIcons.TryGetValue(_groups[i].Name, out string icon)) icon = Icons.Sparkles;
                Icons.DrawCentered(r, icon, 20, active ? Theme.Accent : hover ? Theme.Text : Theme.Faint);
                Widgets.Tooltip(r, _groups[i].Name);
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && hover)
                {
                    OpenTab(i);
                    GUI.FocusControl(null);
                    Event.current.Use();
                }
                y += Theme.S(44);
            }

            string text; Color col;
            if (!PlayerContext.InGame) { text = "MENU"; col = Theme.Faint; }
            else if (NetUtil.IsServer) { text = "HOST"; col = Theme.Ok; }
            else { text = "CLIENT"; col = Theme.Warn; }
            Rect pill = new Rect(Theme.S(8), H - Theme.S(12) - Theme.S(18), rw - Theme.S(16), Theme.S(18));
            Theme.Rounded(pill, 5f, new Color(col.r, col.g, col.b, 0.14f));
            Theme.Pill.normal.textColor = col;
            GUI.Label(pill, text, Theme.Pill);
        }

        /* ─────── Top bar ───────────────────────────────────────────── */

        private static void DrawTopBar(float x0, float W)
        {
            float th = Theme.S(TopH);
            TabGroup group = _groups[ActiveTab];
            group.Page = Mathf.Clamp(group.Page, 0, group.Pages.Count - 1);

            string title = Widgets.Searching ? "Search" : group.Name;
            string crumb = Widgets.Searching ? "" : group.Pages.Count > 1 ? "/  " + group.Pages[group.Page].Name : "";
            float tx = x0 + Theme.S(20);
            float tw = Theme.Title.CalcSize(new GUIContent(title)).x;
            GUI.Label(new Rect(tx, 0, tw, th), title, Theme.Title);
            if (crumb.Length > 0)
                GUI.Label(new Rect(tx + tw + Theme.S(10), 0, Theme.S(160), th), crumb, Theme.Crumb);

            float bh = Theme.S(30), by = Mathf.Round((th - bh) * 0.5f);
            Rect close = new Rect(W - Theme.S(14) - bh, by, bh, bh);
            Vector2 m = Event.current.mousePosition;
            if (close.Contains(m)) Theme.Rounded(close, 8f, Theme.Ctrl);
            Icons.DrawCentered(close, Icons.X, 16, close.Contains(m) ? Theme.Text : Theme.Sub);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && close.Contains(m))
            {
                Visible = false;
                SaveLayout();
                ListPicker.Close();
                Event.current.Use();
            }

            Rect search = new Rect(close.x - Theme.S(8) - Theme.S(190), by, Theme.S(190), bh);
            bool focused = GUI.GetNameOfFocusedControl() == SearchControl;
            if (focused && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                _search = "";
                GUI.FocusControl(null);
                Event.current.Use();
            }
            GUI.SetNextControlName(SearchControl);
            string next = GUI.TextField(search, _search ?? "", 64, Theme.Search);
            if (next != _search) { _search = next; _scroll = Vector2.zero; }
            Icons.Draw(new Rect(search.x + Theme.S(10), search.y + (bh - Theme.S(14)) * 0.5f, Theme.S(14), Theme.S(14)), Icons.Search, Theme.Faint);
            if (string.IsNullOrEmpty(_search) && !focused)
                GUI.Label(new Rect(search.x + Theme.S(30), search.y, search.width - Theme.S(36), bh), "Search features", Theme.Placeholder);

            if (Event.current.type == EventType.Repaint)
                Theme.Fill(new Rect(x0, th - 1f, W - x0, Mathf.Max(1f, Theme.S(1))), Theme.Line);

            Widgets.Filter = string.IsNullOrEmpty(_search) ? null : _search.Trim();
            if (Widgets.Filter != null && Widgets.Filter.Length == 0) Widgets.Filter = null;
        }

        /* ─────── Sub-pages ─────────────────────────────────────────── */

        private static float DrawSegments(float x0)
        {
            float top = Theme.S(TopH);
            TabGroup group = _groups[ActiveTab];
            if (Widgets.Searching || group.Pages.Count < 2) return top + Theme.S(4);

            var names = new string[group.Pages.Count];
            for (int i = 0; i < names.Length; i++) names[i] = group.Pages[i].Name;
            float[] widths = Widgets.SegmentWidths(names);
            float w = Theme.S(6);
            foreach (float f in widths) w += f;
            Rect bar = new Rect(x0 + Theme.S(20), top + Theme.S(12), w, Theme.S(32));
            int page = Widgets.SegmentedBar(bar, group.Page, names, widths);
            if (page != group.Page) { group.Page = page; _scroll = Vector2.zero; }
            return bar.yMax + Theme.S(4);
        }

        /* ─────── Content ───────────────────────────────────────────── */

        private static void DrawContent(Rect area)
        {
            GUILayout.BeginArea(new Rect(area.x + Theme.S(20), area.y, area.width - Theme.S(20) - Theme.S(12), area.height));
            _scroll = GUILayout.BeginScrollView(_scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);
            GUILayout.BeginVertical();

            if (Widgets.Searching)
            {
                Widgets.Matches = 0;
                foreach (TabGroup g in _groups)
                    foreach (PoppyModule p in g.Pages)
                        DrawPage(p, g.Pages.Count > 1 ? g.Name + "  /  " + p.Name : g.Name);
                if (Widgets.Matches == 0)
                {
                    GUILayout.Space(Theme.S(16));
                    GUILayout.Label("Nothing matches \"" + Widgets.Filter + "\".", Theme.Desc);
                }
            }
            else
            {
                if (!PlayerContext.InGame)
                {
                    GUILayout.Space(Theme.S(10));
                    GUILayout.Label("Start a run to use most features.", Theme.Desc, GUILayout.Height(Theme.S(16)));
                }
                TabGroup group = _groups[ActiveTab];
                DrawPage(group.Pages[group.Page], null);
            }

            GUILayout.Space(Theme.S(14));
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            Widgets.Filter = null;
        }

        private static void DrawPage(PoppyModule page, string title)
        {
            Widgets.BeginPage(title);
            try
            {
                page.DrawMenu();
                Widgets.EndPage();
            }
            catch (System.Exception e)
            {
                Widgets.EndPage();
                GUILayout.Label("This tab hit an error, see console.", Theme.Label);
                Log.Error($"{page.Name}.DrawMenu: {e}");
            }
        }

        /* ─────── Footer ────────────────────────────────────────────── */

        private static void DrawFooter(float x0, float W, float H)
        {
            float fh = Theme.S(FootH), y = H - fh;
            if (Event.current.type != EventType.Repaint) return;
            Theme.Fill(new Rect(x0, y, W - x0, Mathf.Max(1f, Theme.S(1))), Theme.Line);

            List<string> fx = ActiveEffects();
            float dot = Theme.S(6);
            float x = x0 + Theme.S(20);
            Theme.Rounded(new Rect(x, y + (fh - dot) * 0.5f, dot, dot), 3f, fx.Count > 0 ? Theme.Ok : Theme.Faint);

            const string hint = "to toggle";
            string key = ModConfig.ToggleMenuKey.Value.ToString();
            float hw = Theme.SmallRight.CalcSize(new GUIContent(hint)).x + Theme.S(4);
            float kw = Theme.Kbd.CalcSize(new GUIContent(key)).x + Theme.S(4);
            float right = W - Theme.S(30); // leave room for the resize grip
            GUI.Label(new Rect(right - hw, y, hw, fh), hint, Theme.SmallRight);
            float kx = right - hw - Theme.S(6) - kw;
            Theme.Kbd.Draw(new Rect(kx, y + (fh - Theme.S(18)) * 0.5f, kw, Theme.S(18)), new GUIContent(key), false, false, false, false);

            string status = fx.Count > 0 ? fx.Count + " active: " + string.Join(", ", fx) : "Idle";
            float sx = x + dot + Theme.S(8);
            GUI.Label(new Rect(sx, y, kx - Theme.S(12) - sx, fh), status, Theme.Small);
        }

        /* ─────── Resize ────────────────────────────────────────────── */

        private static void HandleResize(Rect grip)
        {
            Widgets.ResizeGrip(grip, _resizing);
            if (!_resizing) return;

            Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            _size.x = Mathf.Clamp((mouse.x - _pos.x + 3f) / Theme.Scale, MinW, MaxW);
            _size.y = Mathf.Clamp((mouse.y - _pos.y + 3f) / Theme.Scale, MinH, MaxH);
            if (!Input.GetMouseButton(0)) { _resizing = false; SaveLayout(); }
        }

        /* ─────── Tooltip ───────────────────────────────────────────── */

        private static void DrawTooltip()
        {
            var content = new GUIContent(Widgets.CurrentTooltip);
            Vector2 size = Theme.TipBg.CalcSize(content);
            float w = Mathf.Min(size.x, Theme.S(260));
            float h = Theme.TipBg.CalcHeight(content, w);
            Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            float x = mouse.x + Theme.S(14), y = mouse.y + Theme.S(10);
            if (x + w > Screen.width) x = mouse.x - w - Theme.S(8);
            if (y + h > Screen.height) y = mouse.y - h - Theme.S(8);

            GUI.Window(WindowId + 1, new Rect(x, y, w, h), _ =>
            {
                GUI.Label(new Rect(0, 0, w, h), Widgets.CurrentTooltip, Theme.TipBg);
            }, "", GUIStyle.none);
            GUI.BringWindowToFront(WindowId + 1);
        }

        /* ═══════════════════════════════════════════════════════════════
         *  HUD Overlay
         * ═══════════════════════════════════════════════════════════════ */

        private static bool _draggingHud;
        private static Vector2 _hudDragOffset;

        private static void DrawHud()
        {
            List<string> active = ActiveEffects();
            if (active.Count == 0) return;

            string key = ModConfig.ToggleMenuKey.Value.ToString();
            float pad = Theme.S(14), lineH = Theme.S(20), titleH = Theme.S(22), gap = Theme.S(4);
            float pw = Mathf.Ceil(Theme.LabelBold.CalcSize(new GUIContent("POPPY")).x);
            float kw = Mathf.Ceil(Theme.Kbd.CalcSize(new GUIContent(key)).x) + Theme.S(4);
            float maxW = pw + Theme.S(8) + kw;
            foreach (string e in active)
                maxW = Mathf.Max(maxW, Theme.S(16) + Mathf.Ceil(Theme.Small.CalcSize(new GUIContent(e)).x));

            float width = Mathf.Round(maxW + pad * 2f);
            float h = Mathf.Round(pad * 2f + titleH + gap + active.Count * lineH);
            Rect r = new Rect(ModConfig.HudX.Value, ModConfig.HudY.Value, width, h);

            Event ev = Event.current;
            if (ev.type == EventType.MouseDown && ev.button == 0 && r.Contains(ev.mousePosition))
            {
                _draggingHud = true;
                _hudDragOffset = ev.mousePosition - new Vector2(r.x, r.y);
            }
            else if (ev.type == EventType.MouseUp) _draggingHud = false;
            else if (ev.type == EventType.MouseDrag && _draggingHud)
            {
                ModConfig.HudX.Value = ev.mousePosition.x - _hudDragOffset.x;
                ModConfig.HudY.Value = ev.mousePosition.y - _hudDragOffset.y;
                r.x = ModConfig.HudX.Value;
                r.y = ModConfig.HudY.Value;
            }

            if (ev.type != EventType.Repaint) return;
            Theme.Rounded(r, 10f, new Color(Theme.Bg.r, Theme.Bg.g, Theme.Bg.b, 0.94f));
            float x = r.x + pad, y = r.y + pad;
            Color prev = Theme.LabelBold.normal.textColor;
            Theme.LabelBold.normal.textColor = Theme.Accent;
            GUI.Label(new Rect(x, y, pw + Theme.S(2), titleH), "POPPY", Theme.LabelBold);
            Theme.LabelBold.normal.textColor = prev;
            // The key badge is right-aligned, so the right edge matches the left padding.
            Theme.Kbd.Draw(new Rect(r.xMax - pad - kw, y + Mathf.Round((titleH - Theme.S(18)) * 0.5f), kw, Theme.S(18)), new GUIContent(key), false, false, false, false);
            y += titleH + gap;

            float dot = Theme.S(6);
            foreach (string e in active)
            {
                Theme.Rounded(new Rect(x + Theme.S(1), y + Mathf.Round((lineH - dot) * 0.5f), dot, dot), 3f, Theme.Ok);
                GUI.Label(new Rect(x + Theme.S(16), y, maxW - Theme.S(16), lineH), e, Theme.Small);
                y += lineH;
            }
        }

        /* ═══════════════════════════════════════════════════════════════
         *  Active Effects
         * ═══════════════════════════════════════════════════════════════ */

        private static readonly List<string> _cachedActiveList = new List<string>();
        private static float _nextActiveCheck;

        internal static List<string> ActiveEffects()
        {
            if (Time.realtimeSinceStartup < _nextActiveCheck)
                return _cachedActiveList;

            _nextActiveCheck = Time.realtimeSinceStartup + 0.2f;
            _cachedActiveList.Clear();

            if (PlayerModule.GodMode)         _cachedActiveList.Add("God Mode");
            if (Safety.Buddha)                _cachedActiveList.Add("Semi-Godmode");
            if (PlayerModule.InfiniteSkills)  _cachedActiveList.Add("Skills No CD");
            if (Aim.Enabled)                   _cachedActiveList.Add("Aimbot");
            if (Aim.MagicBullet)               _cachedActiveList.Add("Magic Bullet");
            if (MovementModule.Flight)         _cachedActiveList.Add("Flight");
            if (MovementModule.NoClip)         _cachedActiveList.Add("No-Clip");
            if (MovementModule.AlwaysSprint)   _cachedActiveList.Add("Always Sprint");
            if (MovementModule.JumpPack)       _cachedActiveList.Add("Jump Pack");
            if (MovementModule.Bhop)           _cachedActiveList.Add("BunnyHop");
            if (MovementModule.TeleportToPing) _cachedActiveList.Add("TP to Ping");
            if (FunModule.RailgunnerInstaCharge) _cachedActiveList.Add("Insta Ult Charge");
            if (FunModule.RailgunnerNoUltCooldown) _cachedActiveList.Add("No Ult Cooldown");
            if (FunModule.RailgunnerUltSpam)    _cachedActiveList.Add("Ult Spam");
            if (StatsModule.Active)            _cachedActiveList.Add("Stat Mods");
            if (ItemsModule.NoEquipmentCooldown) _cachedActiveList.Add("No Equip CD");
            if (TeleporterModule.InstaCharge)  _cachedActiveList.Add("Instant TP Charge");
            if (RenderModule.EspMobs || RenderModule.EspInteractables || RenderModule.EspTeleporter)
                _cachedActiveList.Add("ESP");
            if (WorldModule.FreezeMatch)       _cachedActiveList.Add("Match Frozen");
            if (WorldModule.FreezeTimer)       _cachedActiveList.Add("Timer Frozen");
            if (System.Math.Abs(WorldModule.TimeScale - 1f) > 0.001f && !WorldModule.FreezeMatch)
                _cachedActiveList.Add($"Time {WorldModule.TimeScale:0.##}x");

            return _cachedActiveList;
        }
    }
}
