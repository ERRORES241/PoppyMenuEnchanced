using BepInEx;
using UnityEngine;

namespace PoppyMenu
{
    [BepInPlugin(Guid, "PoppyMenuEnchanced", Version)]
    [BepInDependency("com.bepis.r2api.networking")]
    public class PoppyPlugin : BaseUnityPlugin
    {
        internal const string Guid = "com.poppy.poppymenuenchanced";
        internal const string Version = "2.6";

        internal static PoppyPlugin Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            Log.Init(Logger);
            ModConfig.Init(Config);
            NetUtil.Init();
            InputCapture.Init();
            Aim.Init();
            Safety.Init();
            WorldModule.Init();
            ItemsModule.Init();
            MovementModule.Init();
            ConsoleCommands.Init();
            BindStore.Load();
            MacroStore.Load();
            HomeLayoutStore.Load();

            var go = new GameObject("PoppyMenu");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<PoppyController>();

            Log.Message($"PoppyMenuEnchanced v{Version} loaded. Press {ModConfig.ToggleMenuKey.Value} to open.");
        }
    }
}
