using System.Collections.Generic;
using System.IO;
using BepInEx;
using Newtonsoft.Json;

namespace PoppyMenu
{
    internal static class MacroStore
    {
        internal static List<Macro> Macros = new List<Macro>();

        private static string FilePath => Path.Combine(Paths.ConfigPath, "PoppyMenu.macros.json");

        internal static void Load()
        {
            try { if (File.Exists(FilePath)) Macros = JsonConvert.DeserializeObject<List<Macro>>(File.ReadAllText(FilePath)) ?? new List<Macro>(); }
            catch (System.Exception e) { Log.Error("Macro load failed: " + e); Macros = new List<Macro>(); }

            // Repair entries from older or hand-edited files: null steps, missing names and ids.
            bool changed = Macros.RemoveAll(m => m == null) > 0;
            foreach (Macro m in Macros)
            {
                if (m.Steps == null) { m.Steps = new List<CustomStep>(); changed = true; }
                m.Steps.RemoveAll(s => s == null);
                if (m.Name == null) { m.Name = "Macro"; changed = true; }
                if (string.IsNullOrEmpty(m.Id)) { m.Id = NewId(); changed = true; }
            }
            if (changed) Save();
        }

        internal static string NewId() => System.Guid.NewGuid().ToString("N");

        internal const string BindPrefix = "macro:";
        internal static string BindKey(Macro m) => BindPrefix + m.Id;

        // Binds, Home shortcuts and macro steps used to point at macros by name. Point them at ids instead.
        internal static void MigrateReferences()
        {
            bool binds = false;
            foreach (Bind b in BindStore.Binds)
            {
                if (b == null || b.ActionId == null || !b.ActionId.StartsWith(BindPrefix)) continue;
                Macro m = Get(b.ActionId.Substring(BindPrefix.Length));
                if (m != null && b.ActionId != BindKey(m)) { b.ActionId = BindKey(m); binds = true; }
            }
            if (binds) BindStore.Save();

            bool macros = false;
            foreach (Macro owner in Macros)
                foreach (CustomStep s in owner.Steps)
                    macros |= MigrateStep(s);
            if (macros) Save();

            bool home = false;
            foreach (CustomStep s in HomeLayoutStore.Shortcuts)
                home |= MigrateStep(s);
            if (home) HomeLayoutStore.Save();
        }

        private static bool MigrateStep(CustomStep s)
        {
            if (s == null || s.Kind != StepKind.Macro) return false;
            Macro m = Get(s.Id);
            if (m == null || s.Id == m.Id) return false;
            s.Id = m.Id;
            return true;
        }

        internal static void Save()
        {
            try { File.WriteAllText(FilePath, JsonConvert.SerializeObject(Macros, Formatting.Indented)); }
            catch (System.Exception e) { Log.Error("Macro save failed: " + e); }
        }

        // Looks up by id first, then by name for references saved before macros had ids.
        internal static Macro Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            foreach (Macro m in Macros) if (m != null && m.Id == key) return m;
            foreach (Macro m in Macros) if (m != null && m.Name == key) return m;
            return null;
        }

        private const int MaxDepth = 8;
        private static int _depth;

        internal static void Run(string key)
        {
            Macro m = Get(key);
            if (m == null || m.Steps == null) return;
            // A macro that calls itself (directly or through another macro) would overflow the stack and crash the game.
            if (_depth >= MaxDepth) { Notify.Push("Macro loop stopped: " + m.Name); return; }
            _depth++;
            try { foreach (CustomStep s in m.Steps.ToArray()) StepRunner.Run(s); }
            finally { _depth--; }
        }
    }
}
