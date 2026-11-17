using UnityEngine;

namespace PoppyMenu
{
    internal class SettingsModule : PoppyModule
    {
        internal override string Name => "Settings";

        private static float _pendingScale = -1f;

        internal override void DrawMenu()
        {
            Widgets.SectionBegin("Keybinds");
            Widgets.KeybindRow("Open Menu", ModConfig.ToggleMenuKey, Rebind.IsActive
                ? "Press any key or mouse button (right/middle/side) to bind. Esc cancels."
                : "Bind any feature to any key over in the Keybinds tab.");
            Widgets.SectionEnd();

            Widgets.SectionBegin("Appearance");
            int theme = Widgets.Segmented("Theme", (int)ModConfig.MenuTheme.Value, Theme.ThemeNames);
            if (theme != (int)ModConfig.MenuTheme.Value) ModConfig.MenuTheme.Value = (ThemeKind)theme;

            ModConfig.UiAutoScale.Value = Widgets.Toggle("Scale with resolution", ModConfig.UiAutoScale.Value,
                "Sizes the menu by screen height: 1080p is 100%, 1440p 133%, 4K 200%.");

            // Applied when the slider is released, otherwise the window resizes under the cursor.
            float shown = _pendingScale > 0f ? _pendingScale : ModConfig.UiScale.Value;
            float picked = Widgets.Slider("Menu size", shown, 0.6f, 2f, "Multiplier on top of the automatic size.", "0%");
            if (Widgets.Dragging) _pendingScale = picked;
            else
            {
                float apply = _pendingScale > 0f ? _pendingScale : picked;
                _pendingScale = -1f;
                apply = Mathf.Round(apply * 20f) / 20f;
                if (!Mathf.Approximately(apply, ModConfig.UiScale.Value)) ModConfig.UiScale.Value = apply;
            }

            ModConfig.ShowHud.Value = Widgets.Toggle("Active-Effects HUD", ModConfig.ShowHud.Value,
                "Small list of active cheats while the menu is closed. Drag it to move.");
            Widgets.SectionEnd();

            Widgets.SectionBegin("Multiplayer");
            ModConfig.AllowClientCheats.Value = Widgets.Toggle("Allow Others To Use This (host)", ModConfig.AllowClientCheats.Value,
                "Off by default, so others can't use this menu in your game without permission. Turn on only if you trust the lobby.");
            ModConfig.RequireServerForCheats.Value = Widgets.Toggle("Host-Only (skip on client)", ModConfig.RequireServerForCheats.Value,
                "As a client, don't even request server-side actions, just skip them.");
            Widgets.SectionEnd();

            Widgets.SectionBegin("Maintenance");
            Widgets.Button("Refresh Catalogs", () => { Catalogs.Refresh(); Notify.Push("Catalogs refreshed"); });
            Widgets.Button("Reset Window Position", () => { MenuRoot.ResetPosition(); Notify.Push("Window reset"); });
            Widgets.SectionEnd();

            Widgets.SectionBegin("About");
            Widgets.Value("Poppy Menu Enhanced", "v" + PoppyPlugin.Version);
            Widgets.Note("Risk of Rain 2 cheat and debug menu. Single-player and private lobbies. Icons by Lucide (ISC).");
            Widgets.SectionEnd();
        }
    }
}
