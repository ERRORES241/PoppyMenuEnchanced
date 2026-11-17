using System.Collections.Generic;
using UnityEngine;

namespace PoppyMenu
{
    internal class PoppyController : MonoBehaviour
    {
        private readonly List<PoppyModule> _modules = new List<PoppyModule>();
        private readonly List<TabGroup> _groups = new List<TabGroup>();
        private bool _catalogsTried;
        private string _lastScene;
        private static bool _startupConfigApplied;

        private static string ActiveScene() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        private void Awake()
        {
            _groups.Add(new TabGroup("Aimbot", new AimbotModule()));
            _groups.Add(new TabGroup("Visuals", new RenderModule()));
            _groups.Add(new TabGroup("Movement", new MovementModule()));
            _groups.Add(new TabGroup("Player", new PlayerModule(), new StatsModule(), new ItemsModule()));
            _groups.Add(new TabGroup("World", new WorldModule(), new TeleporterModule(), new SpawnModule(), new RunModule(), new PlayersModule()));
            _groups.Add(new TabGroup("Fun", new FunModule()));
            _groups.Add(new TabGroup("Settings", new ConfigsModule(), new KeybindsModule(), new SettingsModule(), new MacrosModule()));

            foreach (TabGroup g in _groups) _modules.AddRange(g.Pages);

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
