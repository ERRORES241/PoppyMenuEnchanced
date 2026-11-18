using System.Collections.Generic;
using System.IO;
using BepInEx;
using Newtonsoft.Json;
using UnityEngine;

namespace PoppyMenu
{
    internal static class BindStore
    {
        internal static List<Bind> Binds = new List<Bind>();

        private static string FilePath => Path.Combine(Paths.ConfigPath, "PoppyMenu.binds.json");

        internal static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    Binds = JsonConvert.DeserializeObject<List<Bind>>(File.ReadAllText(FilePath)) ?? new List<Bind>();
            }
            catch (System.Exception e)
            {
                Log.Error("Bind load failed: " + e);
                Binds = new List<Bind>();
            }
            MigrateTabs();
        }

        // Tabs renamed in 2.7.1: Players is now Lobby, Fun moved into Combat, Keybinds into Settings.
        private static void MigrateTabs()
        {
            bool changed = false;
            foreach (Bind b in Binds)
            {
                if (b == null) continue;
                string to = b.ActionId == "tab.players" ? "tab.lobby" : b.ActionId == "tab.fun" ? "tab.combat" : b.ActionId == "tab.keybinds" ? "tab.settings" : null;
                if (to != null) { b.ActionId = to; changed = true; }
            }
            if (changed) Save();
        }

        internal static void Save()
        {
            try { File.WriteAllText(FilePath, JsonConvert.SerializeObject(Binds, Formatting.Indented)); }
            catch (System.Exception e) { Log.Error("Bind save failed: " + e); }
        }

        // One key per action: binding again replaces the old key.
        internal static void Set(string actionId, KeyCode key)
        {
            Binds.RemoveAll(b => b == null || b.ActionId == actionId);
            if (key != KeyCode.None) Binds.Add(new Bind { Key = key, ActionId = actionId });
            Save();
        }

        internal static void Unset(string actionId) { if (Binds.RemoveAll(b => b != null && b.ActionId == actionId) > 0) Save(); }

        internal static KeyCode KeyFor(string actionId)
        {
            foreach (Bind b in Binds)
                if (b != null && b.Key != KeyCode.None && b.ActionId == actionId) return b.Key;
            return KeyCode.None;
        }

        internal static void Poll()
        {
            if (Rebind.IsActive || MenuRoot.Visible || ListPicker.IsOpen || InputCapture.IsTypingInGame()) return;
            for (int i = 0; i < Binds.Count; i++)
            {
                Bind b = Binds[i];
                if (b != null && b.Key != KeyCode.None && Input.GetKeyDown(b.Key))
                    ActionRegistry.RunId(b.ActionId);
            }
        }
    }
}
