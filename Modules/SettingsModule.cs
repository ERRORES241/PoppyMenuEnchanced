using UnityEngine;

namespace PoppyMenu
{
    internal class SettingsModule : PoppyModule
    {
        internal override string Name => "Settings";

        private static float _pendingScale = -1f;

        // Settings / General
        internal static void DrawGeneral()
        {
            Widgets.SectionBegin("Menu");
            Widgets.KeybindRow("Menu key", ModConfig.ToggleMenuKey, Rebind.Listening == ModConfig.ToggleMenuKey
                ? "Press a key or mouse button. Esc cancels."
                : "To bind a feature, right-click its row.");
            int theme = Widgets.Segmented("Theme", (int)ModConfig.MenuTheme.Value, Theme.ThemeNames);
            if (theme != (int)ModConfig.MenuTheme.Value) ModConfig.MenuTheme.Value = (ThemeKind)theme;

            ModConfig.UiAutoScale.Value = Widgets.Toggle("Scale with resolution", ModConfig.UiAutoScale.Value,
                "1080p is 100%, 1440p is 133%, 4K is 200%.");

            // Applied when the slider is released, otherwise the window resizes under the cursor.
            float shown = _pendingScale > 0f ? _pendingScale : ModConfig.UiScale.Value;
            float picked = Widgets.Slider("Menu size", shown, 0.6f, 2f, "Applied on top of resolution scaling.", "0%");
            if (Widgets.Dragging) _pendingScale = picked;
            else
            {
                float apply = _pendingScale > 0f ? _pendingScale : picked;
                _pendingScale = -1f;
                apply = Mathf.Round(apply * 20f) / 20f;
                if (!Mathf.Approximately(apply, ModConfig.UiScale.Value)) ModConfig.UiScale.Value = apply;
            }

            ModConfig.ReduceMotion.Value = Widgets.Toggle("Reduce motion", ModConfig.ReduceMotion.Value,
                "Menu animations only fade, without sliding or scaling.");
            ModConfig.ShowHud.Value = Widgets.Toggle("Active list", ModConfig.ShowHud.Value,
                "Shows what's on while the menu is closed. Drag it to move.");
            Widgets.SectionEnd();

            Widgets.SectionBegin("Game");
            Safety.PreventProfileWriting = Widgets.Toggle("Don't save profile", Safety.PreventProfileWriting,
                "Unlocks and stats from this session stay out of your save.");
            Widgets.SectionEnd();

            Widgets.SectionBegin("Maintenance");
            Widgets.Button("Reload lists", "Use it if items or monsters from other mods are missing.", () => { Catalogs.Refresh(); Notify.Push("Lists reloaded"); });
            Widgets.Button("Reset window position", () => { MenuRoot.ResetPosition(); Notify.Push("Window moved back"); });
            Widgets.SectionEnd();

            Widgets.SectionBegin("About");
            Widgets.Value("Poppy Menu Enhanced", "v" + PoppyPlugin.Version);
            Widgets.Note("Cheat and debug menu for Risk of Rain 2. Meant for solo play and private lobbies. Icons: Lucide (ISC).");
            Widgets.SectionEnd();
        }

        // Lobby, before the player list
        internal static void DrawPermissions()
        {
            Widgets.SectionBegin("Permissions");
            ModConfig.AllowClientCheats.Value = Widgets.Toggle("Let clients use the menu", ModConfig.AllowClientCheats.Value,
                "When you host, players who have this mod can use it in your game. Off by default.");
            ModConfig.RequireServerForCheats.Value = Widgets.Toggle("Skip host actions as client", ModConfig.RequireServerForCheats.Value,
                "When you're not the host, actions that need the host are skipped instead of sent.");
            Widgets.SectionEnd();
        }
    }
}
