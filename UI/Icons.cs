using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PoppyMenu
{
    // Lucide icons (THIRD_PARTY_NOTICES.md), white 64x64 PNGs embedded as PoppyMenu.Icons.<name>.png.
    // Mipmaps + trilinear keep them clean when drawn at 14-24 px; colour comes from GUI.color.
    internal static class Icons
    {
        internal const string Crosshair = "crosshair", Eye = "eye", User = "user",
            Globe = "globe", Settings = "settings", Search = "search", X = "x",
            Left = "chevron-left", Right = "chevron-right", Down = "chevron-down", Minus = "minus", Plus = "plus",
            Play = "play", Check = "check", Star = "star", Package = "package", Users = "users", Keyboard = "keyboard";

        private static readonly Dictionary<string, Texture2D> _cache = new Dictionary<string, Texture2D>();

        internal static Texture2D Get(string name)
        {
            if (_cache.TryGetValue(name, out Texture2D tex)) return tex;
            tex = null;
            try
            {
                using (Stream s = typeof(Icons).Assembly.GetManifestResourceStream("PoppyMenu.Icons." + name + ".png"))
                {
                    if (s != null)
                    {
                        var bytes = new byte[s.Length];
                        int read = 0;
                        while (read < bytes.Length)
                        {
                            int n = s.Read(bytes, read, bytes.Length - read);
                            if (n <= 0) break;
                            read += n;
                        }
                        tex = new Texture2D(2, 2, TextureFormat.RGBA32, true)
                        {
                            filterMode = FilterMode.Trilinear,
                            wrapMode = TextureWrapMode.Clamp,
                            hideFlags = HideFlags.HideAndDontSave
                        };
                        if (!ImageConversion.LoadImage(tex, bytes, false)) tex = null;
                        else if (tex.mipmapCount <= 1)
                        {
                            // LoadImage may drop the mip chain; rebuild it so small icons stay smooth.
                            var mipped = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, true)
                            {
                                filterMode = FilterMode.Trilinear,
                                wrapMode = TextureWrapMode.Clamp,
                                hideFlags = HideFlags.HideAndDontSave
                            };
                            mipped.SetPixels32(tex.GetPixels32());
                            mipped.Apply(true, false);
                            Object.Destroy(tex);
                            tex = mipped;
                        }
                    }
                }
            }
            catch (System.Exception e) { Log.Error("Icon " + name + ": " + e.Message); tex = null; }
            if (tex == null) Log.Error("Icon missing: " + name);
            _cache[name] = tex;
            return tex;
        }

        internal static void Draw(Rect r, string name, Color color)
        {
            Texture2D tex = Get(name);
            if (tex == null || Event.current.type != EventType.Repaint) return;
            // Multiplied, not replaced: fades and dimmed rows set GUI.color around the icon.
            Color prev = GUI.color;
            GUI.color = prev * color;
            GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
            GUI.color = prev;
        }

        // Icon of the given logical size centred in r.
        internal static void DrawCentered(Rect r, string name, float size, Color color)
        {
            float s = Theme.S(size);
            Draw(new Rect(Mathf.Round(r.x + (r.width - s) * 0.5f), Mathf.Round(r.y + (r.height - s) * 0.5f), s, s), name, color);
        }
    }
}
