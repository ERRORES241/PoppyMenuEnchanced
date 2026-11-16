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
        }

        internal static void Save()
        {
            try { File.WriteAllText(FilePath, JsonConvert.SerializeObject(Macros, Formatting.Indented)); }
            catch (System.Exception e) { Log.Error("Macro save failed: " + e); }
        }

        internal static Macro Get(string name)
        {
            foreach (Macro m in Macros) if (m != null && m.Name == name) return m;
            return null;
        }

        private const int MaxDepth = 8;
        private static int _depth;

        internal static void Run(string name)
        {
            Macro m = Get(name);
            if (m == null || m.Steps == null) return;
            // A macro that calls itself (directly or through another macro) would overflow the stack and crash the game.
            if (_depth >= MaxDepth) { Notify.Push("Macro loop stopped: " + name); return; }
            _depth++;
            try { foreach (CustomStep s in m.Steps.ToArray()) StepRunner.Run(s); }
            finally { _depth--; }
        }
    }
}
