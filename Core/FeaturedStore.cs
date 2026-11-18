using System.Collections.Generic;
using System.IO;
using BepInEx;
using Newtonsoft.Json;

namespace PoppyMenu
{
    // Rows shown on the Featured tab, as "Tab/Page/Label" keys (Widgets.RowKey).
    internal static class FeaturedStore
    {
        internal static HashSet<string> Keys = new HashSet<string>();

        private static string FilePath => Path.Combine(Paths.ConfigPath, "PoppyMenu.featured.json");

        // First launch: the rows most people reach for.
        private static readonly string[] Defaults =
        {
            "Player/Character/God mode", "Player/Character/No skill cooldowns", "Player/Character/Heal to full",
            "Player/Movement/Flight", "Items/Give/Give item", "Combat/Aimbot/Aimbot", "Visuals/ESP/Enemies",
            "World/Stage/Skip stage", "World/Enemies/Kill all enemies",
        };

        internal static void Load()
        {
            try
            {
                Keys = File.Exists(FilePath)
                    ? new HashSet<string>(JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(FilePath)) ?? new List<string>())
                    : new HashSet<string>(Defaults);
            }
            catch (System.Exception e) { Log.Error("Featured load failed: " + e); Keys = new HashSet<string>(Defaults); }
        }

        private static void Save()
        {
            try { File.WriteAllText(FilePath, JsonConvert.SerializeObject(new List<string>(Keys), Formatting.Indented)); }
            catch (System.Exception e) { Log.Error("Featured save failed: " + e); }
        }

        internal static bool Has(string key) => key != null && Keys.Contains(key);

        internal static void Toggle(string key)
        {
            if (key == null) return;
            if (!Keys.Remove(key)) Keys.Add(key);
            Save();
        }
    }
}
