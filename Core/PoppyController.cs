using System.Collections.Generic;
using UnityEngine;

namespace PoppyMenu
{
    internal class PoppyController : MonoBehaviour
    {
        private readonly List<PoppyModule> _modules = new List<PoppyModule>();
        private readonly List<TabGroup> _groups = new List<TabGroup>();

        // Rail order. ActionRegistry registers an "open tab" action for each.
        internal static readonly string[] TabNames = { "Featured", "Player", "Items", "Combat", "Visuals", "World", "Lobby", "Settings" };
        private bool _catalogsTried;
        private string _lastScene;
        private static bool _startupConfigApplied;

        private static string ActiveScene() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        private void Awake()
        {
            // Modules hold the feature logic (Tick, overlays); pages only arrange their sections.
            _modules.AddRange(new PoppyModule[]
            {
                new PlayerModule(), new MovementModule(), new StatsModule(), new ItemsModule(), new AimbotModule(), new FunModule(),
                new RenderModule(), new TeleporterModule(), new SpawnModule(), new WorldModule(), new RunModule(), new PlayersModule(),
                new SettingsModule(), new MacrosModule(), new ConfigsModule(),
            });

            _groups.Add(new TabGroup("Featured", new Page("Featured")));
            _groups.Add(new TabGroup("Player",
                new Page("Character", PlayerModule.DrawCharacter),
                new Page("Movement", MovementModule.DrawMovement),
                new Page("Stats", StatsModule.DrawStats)));
            _groups.Add(new TabGroup("Items",
                new Page("Give", ItemsModule.DrawGive, PlayerModule.DrawEconomy),
                new Page("Inventory", ItemsModule.DrawInventory)));
            _groups.Add(new TabGroup("Combat",
                new Page("Aimbot", AimbotModule.DrawAimbot),
                new Page("Weapons", AimbotModule.DrawWeapon, FunModule.DrawRailgunner)));
            _groups.Add(new TabGroup("Visuals", new Page("ESP", RenderModule.DrawVisuals)));
            _groups.Add(new TabGroup("World",
                new Page("Stage", TeleporterModule.DrawStage, WorldModule.DrawShrines),
                new Page("Enemies", SpawnModule.DrawSpawn, WorldModule.DrawEnemies),
                new Page("Run", WorldModule.DrawTime, RunModule.DrawRun)));
            _groups.Add(new TabGroup("Lobby", new Page("Players", SettingsModule.DrawPermissions, PlayersModule.DrawPlayers)));
            _groups.Add(new TabGroup("Settings",
                new Page("General", SettingsModule.DrawGeneral),
                new Page("Macros", MacrosModule.DrawMacros),
                new Page("Configs", ConfigsModule.DrawConfigs)));

            gameObject.AddComponent<CursorOverlay>();
        }

        private void Start()
        {
            if (!_startupConfigApplied)
            {
                _startupConfigApplied = true;
                try { ConfigsModule.ApplyStartupConfig(); } catch (System.Exception e) { Log.Error(e); }
            }
        }

        private void Update()
        {
            PlayerContext.Refresh();

            if (PlayerContext.InGame)
            {
                if (!Catalogs.Ready && !_catalogsTried)
                {
                    _catalogsTried = true;
                    try { Catalogs.Refresh(); } catch (System.Exception e) { Log.Error(e); }
                    _lastScene = ActiveScene();
                }
                else if (Catalogs.Ready)
                {
                    string scene = ActiveScene();
                    if (scene != _lastScene)
                    {
                        _lastScene = scene;
                        try { Catalogs.RefreshSpawnCards(); } catch (System.Exception e) { Log.Error(e); }
                    }
                }
            }

            Rebind.Poll();
            if (Rebind.IsActive && !MenuRoot.Visible) Rebind.Cancel();

            if (!PlayerContext.InGame) WorldModule.RestoreTime();

            HandleHotkeys();

            InputCapture.Sync(MenuRoot.Visible || ListPicker.IsOpen);

            if (PlayerContext.InGame)
            {
                foreach (PoppyModule m in _modules)
                {
                    try { m.Tick(); }
                    catch (System.Exception e) { Log.Error($"{m.Name}.Tick: {e}"); }
                }
            }
        }

        private void HandleHotkeys()
        {
            if (Rebind.IsActive || Rebind.AppliedFrame == Time.frameCount) return;
            if (InputCapture.IsTypingInGame()) return;

            if (Input.GetKeyDown(ModConfig.ToggleMenuKey.Value))
            {
                MenuRoot.Visible = !MenuRoot.Visible;
                if (!MenuRoot.Visible)
                {
                    ListPicker.Close();
                    MenuRoot.SaveLayout();
                }
            }

            BindStore.Poll();
        }

        private void OnGUI()
        {
            Theme.EnsureInit();

            if (PlayerContext.InGame)
            {
                foreach (PoppyModule m in _modules)
                {
                    try { m.DrawOverlay(); }
                    catch {  }
                }
            }

            Notify.Draw();

            MenuRoot.Draw(_groups);
        }

        private bool _menuWasVisible;

        private void LateUpdate()
        {
            // Persist every toggle once the menu closes (button, hotkey or reset), not only when a preset loads.
            if (_menuWasVisible && !MenuRoot.Visible)
            {
                try { ModConfig.Save(); } catch (System.Exception e) { Log.Error(e); }
            }
            _menuWasVisible = MenuRoot.Visible;

            if (MenuRoot.Visible || ListPicker.IsOpen)
                Cursor.lockState = CursorLockMode.None;
        }

        private void OnApplicationQuit()
        {
            // Hotkey and macro toggles can change settings while the menu is closed.
            try { ModConfig.Save(); } catch (System.Exception e) { Log.Error(e); }
        }

        private void OnDestroy()
        {
            MenuRoot.Visible = false;
            ListPicker.Close();
            try { InputCapture.Shutdown(); } catch { }
            try { Aim.Shutdown(); } catch { }
            try { Safety.Shutdown(); } catch { }
            foreach (PoppyModule m in _modules)
            {
                try { m.OnUnload(); } catch { }
            }
        }
    }
}
