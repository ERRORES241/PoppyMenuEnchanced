using System.Collections.Generic;
using UnityEngine;

namespace PoppyMenu
{
    internal enum ThemeKind { Midnight, Violet, Ror2Gold }

    // Palette, scale and styles. Everything is drawn in real screen pixels: sizes go through S(),
    // fonts are rebuilt at the scaled size, so text stays sharp (no GUI.matrix stretching).
    internal static class Theme
    {
        // ── Palette (current theme) ─────────────────────────────────────
        internal static Color Accent, AccentSoft, AccentText;
        internal static Color Bg, Rail, Panel, RowHover, Line, Ctrl, CtrlHover;
        internal static Color Text, Sub, Faint, On, Danger, Ok, Warn;

        internal static readonly string[] ThemeNames = { "Midnight", "Violet", "RoR2 gold" };

        // ── Scale ───────────────────────────────────────────────────────
        internal static float Scale { get; private set; } = 1f;
        internal static float S(float px) => Mathf.Round(px * Scale);
        internal static int Si(float px) => Mathf.RoundToInt(px * Scale);

        // ── Styles ──────────────────────────────────────────────────────
        internal static GUIStyle WindowBg, RailBg, PanelBg, HoverBg, TipBg, PopupBg;
        internal static GUIStyle Title, Crumb, Label, LabelBold, Desc, SectionTitle, Small, SmallRight, Value, ValueCenter, Logo, Kbd, Pill, RowPad;
        internal static GUIStyle Button, Primary, DangerBtn, Seg, SegOn, Search, Input, Placeholder, ActionLabel, ActionLabelAccent, ActionLabelDanger;

        private static bool _ready;
        private static ThemeKind _builtTheme = (ThemeKind)(-1);
        private static float _builtScale = -1f;
        private static Font _font;
        private static bool _fontTried;
        private static readonly Dictionary<long, Texture2D> _texCache = new Dictionary<long, Texture2D>();

        /* ═══════════════════════════════════════════════════════════════
         *  Palettes (match tools/ui-preview/new.html)
         * ═══════════════════════════════════════════════════════════════ */

        private static Color H(uint rgb, float a = 1f) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, a);

        private static void LoadPalette(ThemeKind kind)
        {
            switch (kind)
            {
                case ThemeKind.Violet:
                    Accent = H(0x7765FF); AccentSoft = H(0x7765FF, 0.18f); AccentText = Color.white;
                    Bg = H(0x131218); Rail = H(0x0D0C12); Panel = H(0x1A1922); RowHover = H(0x201F2A); Line = new Color(1, 1, 1, 0.06f);
                    Ctrl = H(0x262433); CtrlHover = H(0x2F2C3F); Text = H(0xECEAF4); Sub = H(0x8F8AA6); Faint = H(0x5E5A72);
                    Danger = H(0xFF5C7A); Ok = H(0x46D39A); Warn = H(0xF5C45E);
                    break;
                case ThemeKind.Ror2Gold:
                    Accent = H(0xE8B44A); AccentSoft = H(0xE8B44A, 0.14f); AccentText = H(0x1A1406);
                    Bg = H(0x12161C); Rail = H(0x0C0F14); Panel = H(0x182029); RowHover = H(0x1E2833); Line = H(0xA0BEDC, 0.08f);
                    Ctrl = H(0x223040); CtrlHover = H(0x2A3B4E); Text = H(0xE4EAF0); Sub = H(0x8798AA); Faint = H(0x556577);
                    Danger = H(0xE5534B); Ok = H(0x69C26A); Warn = H(0xE8B44A);
                    break;
                default:
                    Accent = H(0x4F8CFF); AccentSoft = H(0x4F8CFF, 0.16f); AccentText = Color.white;
                    Bg = H(0x111318); Rail = H(0x0B0D11); Panel = H(0x171A21); RowHover = H(0x1D212A); Line = new Color(1, 1, 1, 0.06f);
                    Ctrl = H(0x232733); CtrlHover = H(0x2B3040); Text = H(0xE6E8EE); Sub = H(0x8A90A0); Faint = H(0x5A6070);
                    Danger = H(0xFF5A5F); Ok = H(0x3DD68C); Warn = H(0xF2C14E);
                    break;
            }
            On = Accent;
        }

        /* ═══════════════════════════════════════════════════════════════
         *  Texture Generation
         * ═══════════════════════════════════════════════════════════════ */

        internal static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            t.hideFlags = HideFlags.HideAndDontSave;
            return t;
        }

        /// <summary>Rounded-rectangle texture for 9-slicing via GUIStyle.border.</summary>
        internal static Texture2D RoundedRect(int w, int h, int r, Color fill) => RoundedRect(w, h, r, fill, 0, default);

        /// <summary>Rounded rectangle with an optional inner outline of the given width.</summary>
        internal static Texture2D RoundedRect(int w, int h, int r, Color fill, int outline, Color outlineColor)
        {
            long key;
            unchecked { key = HashKey(w, h, r, fill) * 31 + outline * 7919 + outlineColor.GetHashCode(); }
            if (_texCache.TryGetValue(key, out Texture2D cached) && cached != null) return cached;

            var tex = new Texture2D(w, h, TextureFormat.ARGB32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[w * h];
            Color oc = new Color(Mathf.Clamp01(outlineColor.r), Mathf.Clamp01(outlineColor.g), Mathf.Clamp01(outlineColor.b), Mathf.Clamp01(outlineColor.a));
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float outer = RoundMask(x, y, w, h, r, 0);
                    Color c = fill;
                    if (outline > 0)
                    {
                        float inner = RoundMask(x, y, w, h, Mathf.Max(0, r - outline), outline);
                        c = Over(oc, (1f - inner) * oc.a, fill);
                    }
                    c.a *= outer;
                    px[y * w + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply(false, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            _texCache[key] = tex;
            return tex;
        }

        // "Source over" blend of colour `top` with coverage k onto `bottom`.
        private static Color Over(Color top, float k, Color bottom)
        {
            float a = k + bottom.a * (1f - k);
            if (a <= 0f) return new Color(top.r, top.g, top.b, 0f);
            float wb = bottom.a * (1f - k);
            return new Color((top.r * k + bottom.r * wb) / a, (top.g * k + bottom.g * wb) / a, (top.b * k + bottom.b * wb) / a, a);
        }

        internal static Texture2D Circle(int size, Color fill)
        {
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color[size * size];
            float c = (size - 1) * 0.5f;
            for (int py = 0; py < size; py++)
                for (int px2 = 0; px2 < size; px2++)
                {
                    float dx = px2 - c, dy = py - c;
                    float a = Mathf.Clamp01(c + 0.5f - Mathf.Sqrt(dx * dx + dy * dy));
                    Color col = fill; col.a *= a;
                    px[py * size + px2] = col;
                }
            tex.SetPixels(px);
            tex.Apply(false, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }

        // Alpha of a pixel inside a rounded rect inset by `inset` px on every side.
        private static float RoundMask(int x, int y, int w, int h, int r, int inset)
        {
            float fx = x + 0.5f, fy = y + 0.5f;
            float l = inset, t = inset, rr = w - inset, b = h - inset;
            if (fx < l || fx > rr || fy < t || fy > b)
            {
                float ex = Mathf.Max(l - fx, 0f, fx - rr), ey = Mathf.Max(t - fy, 0f, fy - b);
                // Distance is measured from the pixel centre: a pixel half a pixel outside is fully outside.
                return Mathf.Clamp01(0.5f - Mathf.Max(ex, ey));
            }
            if (r <= 0) return 1f;
            float cx = Mathf.Clamp(fx, l + r, rr - r), cy = Mathf.Clamp(fy, t + r, b - r);
            float dx = fx - cx, dy = fy - cy;
            return Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
        }

        private static long HashKey(int w, int h, int r, Color c)
        {
            unchecked
            {
                long hash = 17;
                hash = hash * 397 + w;
                hash = hash * 397 + h;
                hash = hash * 397 + r;
                hash = hash * 397 + c.GetHashCode();
                return hash;
            }
        }

        // 9-sliceable rounded texture of radius r (already in pixels).
        private static Texture2D Round(int r, Color fill) => RoundedRect(r * 2 + 2, r * 2 + 2, r, fill);
        private static Texture2D RoundOutline(int r, Color fill, int outline, Color oc) => RoundedRect(r * 2 + 2, r * 2 + 2, r, fill, outline, oc);

        /* ═══════════════════════════════════════════════════════════════
         *  Drawing Helpers
         * ═══════════════════════════════════════════════════════════════ */

        internal static void Fill(Rect r, Color c)
        {
            Color prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        internal static void Box(Rect r, GUIStyle style)
        {
            if (Event.current.type == EventType.Repaint) style.Draw(r, GUIContent.none, false, false, false, false);
        }

        private static readonly GUIStyle _roundStyle = new GUIStyle();

        internal static void Rounded(Rect r, float radius, Color c)
        {
            if (Event.current.type != EventType.Repaint) return;
            int rp = Mathf.Max(1, Mathf.Min(Si(radius), (int)(Mathf.Min(r.width, r.height) * 0.5f)));
            _roundStyle.normal.background = Round(rp, c);
            int b = rp;
            _roundStyle.border.left = b; _roundStyle.border.right = b; _roundStyle.border.top = b; _roundStyle.border.bottom = b;
            _roundStyle.Draw(r, GUIContent.none, false, false, false, false);
        }

        // Same shape from one white texture tinted with GUI.color. For colours that animate:
        // Rounded caches a texture per colour, which would grow without bound mid-transition.
        internal static void RoundedTint(Rect r, float radius, Color c)
        {
            if (Event.current.type != EventType.Repaint) return;
            Color prev = GUI.color;
            GUI.color = new Color(prev.r * c.r, prev.g * c.g, prev.b * c.b, prev.a * c.a);
            Rounded(r, radius, Color.white);
            GUI.color = prev;
        }

        internal static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        /* ═══════════════════════════════════════════════════════════════
         *  Initialization
         * ═══════════════════════════════════════════════════════════════ */

        // Called every OnGUI: picks up theme and scale changes.
        internal static void EnsureInit()
        {
            ThemeKind kind = ModConfig.MenuTheme != null ? ModConfig.MenuTheme.Value : ThemeKind.Midnight;
            float auto = ModConfig.UiAutoScale == null || ModConfig.UiAutoScale.Value ? Mathf.Max(Screen.height, 480) / 1080f : 1f;
            float manual = ModConfig.UiScale != null ? Mathf.Clamp(ModConfig.UiScale.Value, 0.6f, 2f) : 1f;
            float scale = Mathf.Clamp(Mathf.Round(auto * manual * 20f) / 20f, 0.5f, 4f);

            if (_ready && kind == _builtTheme && Mathf.Approximately(scale, _builtScale)) return;
            _ready = true;
            _builtTheme = kind;
            _builtScale = scale;
            Scale = scale;
            LoadPalette(kind);
            BuildStyles();
        }

        private static Font UiFont()
        {
            if (_fontTried) return _font;
            _fontTried = true;
            try
            {
                var installed = new HashSet<string>(Font.GetOSInstalledFontNames());
                foreach (string name in new[] { "Segoe UI", "Arial" })
                    if (installed.Contains(name)) { _font = Font.CreateDynamicFontFromOSFont(name, 16); _fontLift = name == "Segoe UI" ? 0.15f : 0.06f; break; }
            }
            catch (System.Exception e) { Log.Error("UI font: " + e.Message); _font = null; }
            return _font;
        }

        // Segoe UI has a tall line box, so IMGUI centres its glyphs a few pixels low. Lift text by a share of the font size.
        private static float _fontLift;
        private static Vector2 Lift(int pixelSize) => new Vector2(0f, -Mathf.Round(pixelSize * _fontLift));

        private static GUIStyle TextStyle(int size, Color color, FontStyle fs = FontStyle.Normal, TextAnchor anchor = TextAnchor.MiddleLeft, bool wrap = false)
        {
            var s = new GUIStyle(GUI.skin.label)
            {
                font = UiFont() ?? GUI.skin.label.font,
                fontSize = Si(size), fontStyle = fs, alignment = anchor, wordWrap = wrap, richText = true,
                padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0),
                clipping = TextClipping.Clip
            };
            s.normal.textColor = color;
            s.hover.textColor = color;
            UiFont();
            s.contentOffset = Lift(s.fontSize);
            return s;
        }

        private static GUIStyle Btn(Color fill, Color hover, Color text, int fontSize = 12, FontStyle fs = FontStyle.Normal)
        {
            int r = Si(7);
            var s = TextStyle(fontSize, text, fs, TextAnchor.MiddleCenter);
            s.normal.background = Round(r, fill);
            s.hover.background = Round(r, hover);
            s.active.background = Round(r, hover);
            s.hover.textColor = text; s.active.textColor = text;
            s.border = new RectOffset(r, r, r, r);
            s.padding = new RectOffset(Si(12), Si(12), 0, 0);
            s.clipping = TextClipping.Clip;
            return s;
        }

        private static void BuildStyles()
        {
            _texCache.Clear();

            WindowBg = new GUIStyle { border = new RectOffset(Si(12), Si(12), Si(12), Si(12)) };
            WindowBg.normal.background = RoundOutline(Si(12), Bg, Mathf.Max(1, Si(1)), Line + new Color(0, 0, 0, 0.04f));
            RailBg = new GUIStyle { border = new RectOffset(Si(12), Si(12), Si(12), Si(12)) };
            RailBg.normal.background = Round(Si(12), Rail);
            PanelBg = new GUIStyle { border = new RectOffset(Si(10), Si(10), Si(10), Si(10)) };
            PanelBg.normal.background = Round(Si(10), Panel);
            HoverBg = new GUIStyle { border = new RectOffset(Si(8), Si(8), Si(8), Si(8)) };
            HoverBg.normal.background = Round(Si(8), RowHover);
            PopupBg = new GUIStyle { border = new RectOffset(Si(8), Si(8), Si(8), Si(8)) };
            PopupBg.normal.background = RoundOutline(Si(8), Color.Lerp(Panel, Ctrl, 0.5f), Mathf.Max(1, Si(1)), Line + new Color(1, 1, 1, 0.05f));
            TipBg = TextStyle(11, Text, wrap: true);
            TipBg.normal.background = Round(Si(6), new Color(0, 0, 0, 0.92f));
            TipBg.border = new RectOffset(Si(6), Si(6), Si(6), Si(6));
            TipBg.padding = new RectOffset(Si(8), Si(8), Si(5), Si(5));

            Title = TextStyle(17, Text, FontStyle.Bold);
            Crumb = TextStyle(13, Faint);
            Label = TextStyle(13, Text);
            LabelBold = TextStyle(13, Text, FontStyle.Bold);
            Desc = TextStyle(11, Sub, wrap: true);
            Desc.alignment = TextAnchor.UpperLeft;
            SectionTitle = TextStyle(11, Faint, FontStyle.Bold);
            Small = TextStyle(11, Faint);
            SmallRight = TextStyle(11, Faint, anchor: TextAnchor.MiddleRight);
            Value = TextStyle(12, Sub, anchor: TextAnchor.MiddleRight);
            ValueCenter = TextStyle(12, Text, FontStyle.Bold, TextAnchor.MiddleCenter);
            RowPad = new GUIStyle { padding = new RectOffset(Si(14), Si(14), Si(8), Si(8)) };
            Logo = TextStyle(16, AccentText, FontStyle.Bold, TextAnchor.MiddleCenter);
            Kbd = TextStyle(11, Sub, anchor: TextAnchor.MiddleCenter);
            Kbd.normal.background = RoundOutline(Si(5), new Color(0, 0, 0, 0), Mathf.Max(1, Si(1)), Line + new Color(1, 1, 1, 0.04f));
            Kbd.border = new RectOffset(Si(5), Si(5), Si(5), Si(5));
            Kbd.padding = new RectOffset(Si(6), Si(6), 0, 0);
            Pill = TextStyle(9, Ok, FontStyle.Bold, TextAnchor.MiddleCenter);
            Pill.normal.background = Round(Si(5), new Color(Ok.r, Ok.g, Ok.b, 0.14f));
            Pill.border = new RectOffset(Si(5), Si(5), Si(5), Si(5));

            Button = Btn(Ctrl, CtrlHover, Text, 12, FontStyle.Bold);
            Primary = Btn(Accent, Color.Lerp(Accent, Color.white, 0.12f), AccentText, 12, FontStyle.Bold);
            DangerBtn = Btn(new Color(0, 0, 0, 0), new Color(Danger.r, Danger.g, Danger.b, 0.12f), Danger, 12, FontStyle.Bold);
            int dr = Si(7);
            DangerBtn.normal.background = RoundOutline(dr, new Color(0, 0, 0, 0), Mathf.Max(1, Si(1)), new Color(Danger.r, Danger.g, Danger.b, 0.4f));
            DangerBtn.hover.background = RoundOutline(dr, new Color(Danger.r, Danger.g, Danger.b, 0.12f), Mathf.Max(1, Si(1)), new Color(Danger.r, Danger.g, Danger.b, 0.6f));
            DangerBtn.active.background = DangerBtn.hover.background;

            Seg = TextStyle(12, Sub, anchor: TextAnchor.MiddleCenter);
            Seg.hover.textColor = Text;
            SegOn = TextStyle(12, Text, FontStyle.Bold, TextAnchor.MiddleCenter);
            SegOn.normal.background = Round(Si(7), CtrlHover);
            SegOn.border = new RectOffset(Si(7), Si(7), Si(7), Si(7));

            Search = new GUIStyle(GUI.skin.textField)
            {
                font = UiFont() ?? GUI.skin.textField.font,
                fontSize = Si(12), alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(Si(30), Si(10), 0, 0), margin = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(Si(8), Si(8), Si(8), Si(8))
            };
            Search.contentOffset = Lift(Search.fontSize);
            Texture2D sTex = Round(Si(8), Ctrl), sFocus = RoundOutline(Si(8), Ctrl, Mathf.Max(1, Si(1)), new Color(Accent.r, Accent.g, Accent.b, 0.6f));
            Search.normal.background = sTex; Search.hover.background = sTex;
            Search.focused.background = sFocus; Search.onFocused.background = sFocus;
            Search.normal.textColor = Search.hover.textColor = Search.focused.textColor = Search.onFocused.textColor = Text;
            Placeholder = TextStyle(12, Faint);
            Input = new GUIStyle(Search) { padding = new RectOffset(Si(10), Si(10), 0, 0) };

            ActionLabel = TextStyle(13, Text);
            ActionLabelAccent = TextStyle(13, Accent, FontStyle.Bold);
            ActionLabelDanger = TextStyle(13, Danger);

            ApplyScrollbarSkin();
        }

        private static void ApplyScrollbarSkin()
        {
            try
            {
                GUISkin skin = GUI.skin;
                skin.verticalScrollbar.normal.background = Solid(new Color(0, 0, 0, 0));
                skin.verticalScrollbar.fixedWidth = Mathf.Max(3, Si(4));
                skin.verticalScrollbar.border = new RectOffset(0, 0, 0, 0);
                skin.verticalScrollbar.margin = new RectOffset(Si(4), Si(2), 0, 0);
                skin.verticalScrollbar.padding = new RectOffset(0, 0, 0, 0);

                int r = Mathf.Max(1, Si(2));
                skin.verticalScrollbarThumb.normal.background = RoundedRect(r * 2 + 2, r * 2 + 2, r, CtrlHover);
                skin.verticalScrollbarThumb.fixedWidth = Mathf.Max(3, Si(4));
                skin.verticalScrollbarThumb.border = new RectOffset(r, r, r, r);

                skin.verticalScrollbarUpButton.fixedHeight = 0; skin.verticalScrollbarDownButton.fixedHeight = 0;
                skin.horizontalScrollbar.fixedHeight = 0;
                skin.horizontalScrollbarThumb.fixedHeight = 0;
            }
            catch { }
        }
    }
}
