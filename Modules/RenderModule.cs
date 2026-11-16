using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace PoppyMenu
{
    internal class RenderModule : PoppyModule
    {
        internal override string Name => "ESP";

        internal static bool EspMobs;
        internal static bool EspInteractables;
        internal static bool EspFilterChests = true;
        internal static bool EspFilterShrines = true;
        internal static bool EspFilterPrinters = true;
        internal static bool EspFilterScrappers = true;
        internal static bool EspFilterDrones = true;
        internal static bool EspFilterBarrels = true;
        internal static bool EspFilterOther = true;
        internal static bool EspTeleporter;

        internal static bool FovOverride;
        internal static float FovValue = 90f;

        internal static bool ShowNames = true;
        internal static bool ShowDistance = true;
        internal static bool ShowEnemyHealth = true;
        internal static bool ShowOutline = true;
        internal static float FontSize = 12f;
        internal static float MaxDistance;
        internal static float MarkerSize = 6f;

        internal static Color EnemyColor = Color.red;
        internal static Color InteractableColor = Color.cyan;
        internal static Color TeleporterColor = Color.yellow;

        private static GUIStyle _labelStyle;
        private static PressurePlateController[] _plates = new PressurePlateController[0];
        private static ShrineCleanseBehavior[] _cleansePools = new ShrineCleanseBehavior[0];
        private static Stage _lastStage;
        private const float DefaultFov = 60f; // CameraRigController.baseFov default
        private static bool _fovApplied;

        internal override void Tick()
        {
            if (!PlayerContext.InGame)
            {
                _lastStage = null;
                return;
            }

            // ── Camera FOV Changer ──
            if (FovOverride || _fovApplied)
            {
                float fov = FovOverride ? FovValue : DefaultFov;
                foreach (var cameraRig in CameraRigController.readOnlyInstancesList)
                {
                    if (cameraRig != null)
                        cameraRig.baseFov = fov;
                }
                _fovApplied = FovOverride;
            }

            Stage stage = Stage.instance;
            if (stage != _lastStage)
            {
                _lastStage = stage;
                RefreshStageObjects();
            }
        }

        internal static void RefreshStageObjects()
        {
            try
            {
                _plates = Object.FindObjectsOfType<PressurePlateController>();
                _cleansePools = Object.FindObjectsOfType<ShrineCleanseBehavior>();
            }
            catch { }
        }

        internal override void DrawMenu()
        {
            Widgets.SectionBegin("Camera & FOV");
            FovOverride = Widgets.Toggle("FOV Changer", FovOverride);
            if (FovOverride)
            {
                FovValue = Widgets.Slider("Field of View", FovValue, 60f, 140f);
            }
            Widgets.SectionEnd();

            Widgets.SectionBegin("ESP / Wallhack");
            EspMobs = Widgets.Toggle("Enemies", EspMobs);
            EspInteractables = Widgets.Toggle("Interactables", EspInteractables);
            if (EspInteractables)
            {
                EspFilterChests = Widgets.Toggle("  Chests & Terminals", EspFilterChests);
                EspFilterShrines = Widgets.Toggle("  Shrines", EspFilterShrines);
                EspFilterPrinters = Widgets.Toggle("  3D Printers", EspFilterPrinters);
                EspFilterScrappers = Widgets.Toggle("  Scrappers", EspFilterScrappers);
                EspFilterDrones = Widgets.Toggle("  Drones & Turrets", EspFilterDrones);
                EspFilterBarrels = Widgets.Toggle("  Barrels", EspFilterBarrels);
                EspFilterOther = Widgets.Toggle("  Other", EspFilterOther);
            }
            EspTeleporter = Widgets.Toggle("Teleporter & Portals", EspTeleporter);
            Widgets.Hint("Markers draw through walls while in a run.");
            Widgets.SectionEnd();

            Widgets.SectionBegin("Display");
            ShowNames = Widgets.Toggle("Show names", ShowNames);
            ShowDistance = Widgets.Toggle("Show distance", ShowDistance);
            ShowEnemyHealth = Widgets.Toggle("Show enemy health", ShowEnemyHealth);
            ShowOutline = Widgets.Toggle("Text outline", ShowOutline);
            FontSize = Widgets.Slider("Font size", FontSize, 8f, 24f);
            MaxDistance = Widgets.Slider("Max distance (0 = unlimited)", MaxDistance, 0f, 500f);
            MarkerSize = Widgets.Slider("Marker size", MarkerSize, 2f, 16f);
            Widgets.SectionEnd();
        }

        internal override void DrawOverlay()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (!PlayerContext.InGame) return;
            if (!EspMobs && !EspInteractables && !EspTeleporter) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 origin = PlayerContext.HasBody ? PlayerContext.Body.corePosition : cam.transform.position;

            try
            {
                try
                {
                    if (EspMobs)
                    {
                        CharacterBody me = PlayerContext.Body;
                        TeamIndex myTeam = me != null && me.teamComponent != null ? me.teamComponent.teamIndex : TeamIndex.Player;

                        var bodies = CharacterBody.readOnlyInstancesList;
                        for (int i = 0; i < bodies.Count; i++)
                        {
                            CharacterBody body = bodies[i];
                            if (body == null || body == me) continue;
                            if (body.healthComponent == null || !body.healthComponent.alive) continue;
                            if (body.teamComponent == null) continue;

                            TeamIndex t = body.teamComponent.teamIndex;
                            if (t == myTeam || t == TeamIndex.None || t == TeamIndex.Neutral || t == TeamIndex.Player) continue;

                            if (Culled(origin, body.corePosition, out float dist)) continue;
                            DrawMarker(cam, body.corePosition, EnemyMultiLineLabel(body, dist), EnemyColor);
                        }
                    }
                }
                catch (System.Exception e) { EspFail("enemies", e); }

                if (EspInteractables)
                {
                    try
                    {
                        // 1. PurchaseInteractions (Chests, Multishops, Printers, Shrines, Lunar Pods) via InstanceTracker
                        var purchases = InstanceTracker.GetInstancesList<PurchaseInteraction>();
                        if (purchases != null)
                        {
                            for (int i = 0; i < purchases.Count; i++)
                            {
                                PurchaseInteraction pi = purchases[i];
                                if (pi == null || !pi.available || pi.transform == null) continue;
                            
                                string goName = pi.gameObject.name;
                                if (Has(goName, "chest") || Has(goName, "multishop") || Has(goName, "terminal") || Has(goName, "lockbox") || Has(goName, "cradle"))
                                {
                                    if (!EspFilterChests) continue;
                                }
                                else if (Has(goName, "shrine"))
                                {
                                    if (!EspFilterShrines) continue;
                                }
                                else if (Has(goName, "duplicator") || Has(goName, "printer"))
                                {
                                    if (!EspFilterPrinters) continue;
                                }
                                else if (Has(goName, "scrapper"))
                                {
                                    if (!EspFilterScrappers) continue;
                                }
                                else if (Has(goName, "drone") || Has(goName, "turret"))
                                {
                                    if (!EspFilterDrones) continue;
                                }
                                else
                                {
                                    if (!EspFilterOther) continue;
                                }

                                if (Culled(origin, pi.transform.position, out float dist)) continue;

                                string label = GetInteractableMultiLineLabel(pi, dist, out Color itemCol);
                                DrawMarker(cam, pi.transform.position, label, itemCol);
                            }
                        }
                    }
                    catch (System.Exception e) { EspFail("purchases", e); }



                    try
                    {
                        // 2. Barrels via InstanceTracker
                        if (EspFilterBarrels)
                        {
                            var barrels = InstanceTracker.GetInstancesList<BarrelInteraction>();
                            if (barrels != null)
                            {
                                for (int i = 0; i < barrels.Count; i++)
                                {
                                    BarrelInteraction barrel = barrels[i];
                                if (barrel == null || barrel.opened || barrel.transform == null) continue;
                                if (Culled(origin, barrel.transform.position, out float dist)) continue;

                                string nameStr = barrel.GetDisplayName();
                                if (string.IsNullOrEmpty(nameStr)) nameStr = "Barrel";
                                string bName = ShowNames ? $"{nameStr} ($0)" : "";

                                Color col = new Color(0.85f, 0.82f, 0.55f);
                                if (barrel.name != null && barrel.name.IndexOf("Void", System.StringComparison.OrdinalIgnoreCase) >= 0)
                                    col = new Color(0.75f, 0.35f, 0.9f);

                                string dStr = ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "";
                                string label = BuildMultiLine(bName, "", dStr);
                                DrawMarker(cam, barrel.transform.position, label, col);
                            }
                        }
                        }
                    }
                    catch (System.Exception e) { EspFail("barrels", e); }

                    try
                    {
                        // 3. ChestBehaviors (Timed Chests, etc.) via InstanceTracker
                        var chests = InstanceTracker.GetInstancesList<ChestBehavior>();
                        if (chests != null)
                        {
                            for (int i = 0; i < chests.Count; i++)
                            {
                                ChestBehavior chest = chests[i];
                                if (chest == null || chest.transform == null) continue;
                                TimedChestController timed = chest.GetComponent<TimedChestController>();
                                if (timed == null) continue;
                                if (Culled(origin, timed.transform.position, out float dist)) continue;

                                string tName = ShowNames ? "Timed Chest" : "";
                                string dStr = ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "";
                                string label = BuildMultiLine(tName, "[Legendary]", dStr);
                                DrawMarker(cam, timed.transform.position, label, new Color(0.9f, 0.22f, 0.29f));
                            }
                        }
                    }
                    catch (System.Exception e) { EspFail("timed chests", e); }

                    try
                    {
                        // 4. Secret Pressure Plates
                        for (int i = 0; i < _plates.Length; i++)
                        {
                            PressurePlateController plate = _plates[i];
                            if (plate == null || plate.transform == null) continue;
                            if (Culled(origin, plate.transform.position, out float dist)) continue;

                            string pName = ShowNames ? "Secret Button" : "";
                            string dStr = ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "";
                            string label = BuildMultiLine(pName, "", dStr);
                            DrawMarker(cam, plate.transform.position, label, new Color(0.2f, 0.9f, 0.8f));
                        }
                    }
                    catch (System.Exception e) { EspFail("pressure plates", e); }

                    try
                    {
                        // 5. Pickups via InstanceTracker
                        if (EspFilterOther)
                        {
                            var pickups = InstanceTracker.GetInstancesList<GenericPickupController>();
                            if (pickups != null)
                            {
                                for (int i = 0; i < pickups.Count; i++)
                                {
                                    GenericPickupController gpc = pickups[i];
                                    if (gpc == null || gpc.transform == null) continue;
                                    if (Culled(origin, gpc.transform.position, out float dist)) continue;

                                    string label = GetPickupMultiLineLabel(gpc, dist, out Color itemCol);
                                    DrawMarker(cam, gpc.transform.position, label, itemCol);
                                }
                            }
                        }
                    }
                    catch (System.Exception e) { EspFail("pickups", e); }

                    try
                    {
                        // 5b. DLC2-4 interactables that are not PurchaseInteractions (Wok, Drone Scrapper, Wake, Hex altars...)
                        DrawSpecialInteractables(cam, origin);
                    }
                    catch (System.Exception e) { EspFail("special interactables", e); }

                    try
                    {
                        // 6. PickupPickerControllers (Scrappers, Void Potentials, Command Cubes)
                        var pickers = InstanceTracker.GetInstancesList<PickupPickerController>();
                        if (pickers != null)
                        {
                            for (int i = 0; i < pickers.Count; i++)
                            {
                                PickupPickerController picker = pickers[i];
                                if (picker == null || !picker.available || picker.transform == null) continue;
                                bool isScrapper = picker.gameObject.name.IndexOf("Scrapper", System.StringComparison.OrdinalIgnoreCase) >= 0;
                                if (isScrapper ? !EspFilterScrappers : !EspFilterOther) continue;
                            
                                // Skip if attached to GenericPickupController (some Command Cubes might have it, but usually they don't)
                                if (picker.GetComponent<GenericPickupController>() != null) continue;
                                if (Culled(origin, picker.transform.position, out float dist)) continue;

                                string objName = picker.gameObject.name;
                                string pName = picker.GetContextString(null);
                                Color pColor = new Color(0.85f, 0.4f, 0.1f); // Default Orange

                                if (objName.IndexOf("CommandCube", System.StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    PickupIndexNetworker networker = picker.GetComponent<PickupIndexNetworker>();
                                    if (networker != null)
                                    {
                                        string contentName = GetPickupContentInfo(networker.NetworkpickupState.pickupIndex, out Color itemCol, isInteractable: false, isUnchosenCommandCube: true);
                                        if (itemCol != Color.clear) pColor = itemCol;
                                        pName = ShowNames ? (!string.IsNullOrEmpty(contentName) ? contentName : "Command Cube") : "";
                                    }
                                }
                                else
                                {
                                    if (string.IsNullOrEmpty(pName))
                                    {
                                        if (objName.IndexOf("Scrapper", System.StringComparison.OrdinalIgnoreCase) >= 0) pName = "Scrapper";
                                        else if (objName.IndexOf("Void", System.StringComparison.OrdinalIgnoreCase) >= 0 || objName.IndexOf("OptionPickup", System.StringComparison.OrdinalIgnoreCase) >= 0) pName = "Void Potential";
                                        else pName = objName.Replace("(Clone)", "").Trim();
                                    }
                                    else if (!ShowNames)
                                    {
                                        pName = "";
                                    }

                                    if (objName.IndexOf("Void", System.StringComparison.OrdinalIgnoreCase) >= 0 || objName.IndexOf("OptionPickup", System.StringComparison.OrdinalIgnoreCase) >= 0)
                                        pColor = new Color(0.8f, 0.3f, 0.8f); // Purple for Void Potentials
                                }

                                string label = BuildMultiLine(pName, "", ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "");
                                DrawMarker(cam, picker.transform.position, label, pColor);
                            }
                        }
                    }
                    catch (System.Exception e) { EspFail("pickers", e); }

                    try
                    {
                        // 7. Cleansing Pools (Not in InstanceTracker)
                        if (_cleansePools != null)
                        {
                            for (int i = 0; i < _cleansePools.Length; i++)
                            {
                                ShrineCleanseBehavior pool = _cleansePools[i];
                                if (pool == null || pool.transform == null) continue;
                                if (Culled(origin, pool.transform.position, out float dist)) continue;

                                string pName = ShowNames ? Language.GetString(pool.contextToken) : "";
                                if (string.IsNullOrEmpty(pName) || pName == pool.contextToken) pName = "Cleansing Pool";
                            
                                string label = BuildMultiLine(pName, "", ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "");
                                DrawMarker(cam, pool.transform.position, label, new Color(0.9f, 0.9f, 0.9f));
                            }
                        }
                    }
                    catch (System.Exception e) { EspFail("cleansing pools", e); }

                    try
                    {
                        // 8. Bazaar Upgrade Interactions (Lunar Seers / Soup Cauldrons)
                        var bazaarUpgrades = InstanceTracker.GetInstancesList<BazaarUpgradeInteraction>();
                        if (bazaarUpgrades != null)
                        {
                            for (int i = 0; i < bazaarUpgrades.Count; i++)
                            {
                                var upgrade = bazaarUpgrades[i];
                                if (upgrade == null || !upgrade.available || upgrade.transform == null) continue;
                                if (Culled(origin, upgrade.transform.position, out float dist)) continue;

                                string pName = ShowNames ? Language.GetString(upgrade.displayNameToken) : "";
                                if (string.IsNullOrEmpty(pName) || pName == upgrade.displayNameToken) pName = "Bazaar Upgrade";
                            
                                string label = BuildMultiLine(pName, "", ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "");
                                DrawMarker(cam, upgrade.transform.position, label, new Color(0.3f, 0.5f, 0.9f));
                            }
                        }
                    }
                    catch (System.Exception e) { EspFail("bazaar", e); }

                    try
                    {
                        // 9. Geode / Void Seed
                        var geodes = InstanceTracker.GetInstancesList<GeodeController>();
                        if (geodes != null && EspFilterOther)
                        {
                            for (int i = 0; i < geodes.Count; i++)
                            {
                                var geode = geodes[i];
                                if (geode == null || !geode.Networkavailable || geode.transform == null) continue;
                                if (Culled(origin, geode.transform.position, out float dist)) continue;

                                string pName = ShowNames ? geode.GetDisplayName() : "";
                                if (string.IsNullOrEmpty(pName) || pName == geode.displayNameToken) pName = "Void Seed / Geode";

                                string label = BuildMultiLine(pName, "", ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "");
                                DrawMarker(cam, geode.transform.position, label, new Color(0.7f, 0.2f, 0.7f));
                            }
                        }
                    }
                    catch (System.Exception e) { EspFail("geodes", e); }

                    try
                    {
                        // 10. Drone Combiner (Col. Droneman)
                        var combiners = InstanceTracker.GetInstancesList<DroneCombinerController>();
                        if (combiners != null && EspFilterDrones)
                        {
                            for (int i = 0; i < combiners.Count; i++)
                            {
                                var combiner = combiners[i];
                                if (combiner == null || combiner.transform == null) continue;
                                if (Culled(origin, combiner.transform.position, out float dist)) continue;

                                string pName = ShowNames ? combiner.GetContextString(null) : "";
                                if (string.IsNullOrEmpty(pName)) pName = "Drone Combiner";

                                string label = BuildMultiLine(pName, "", ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "");
                                DrawMarker(cam, combiner.transform.position, label, new Color(0.8f, 0.8f, 0.8f));
                            }
                        }
                    }
                    catch (System.Exception e) { EspFail("drone combiners", e); }

                }

                try
                {
                    if (EspTeleporter)
                    {
                        if (TeleporterInteraction.instance != null)
                        {
                            Vector3 tp = TeleporterInteraction.instance.transform.position;
                            if (!Culled(origin, tp, out float dist))
                            {
                                string label = TeleporterMultiLineLabel(TeleporterInteraction.instance, dist);
                                DrawMarker(cam, tp, label, TeleporterColor);
                            }
                        }
                    }
                }
                catch (System.Exception e) { EspFail("teleporter", e); }

                try
                {
                    DrawPortalsAndObjectives(cam, origin);
                }
                catch (System.Exception e) { EspFail("portals", e); }
            }
            catch { }
        }

        // Case-insensitive search without allocating a lowered copy of the name every frame.
        private static bool Has(string s, string part) => s.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) >= 0;

        private static readonly HashSet<string> _espErrors = new HashSet<string>();

        private static void EspFail(string section, System.Exception e)
        {
            if (_espErrors.Add(section)) Log.Error("ESP " + section + ": " + e);
        }

        private static bool IsCommandArtifactActive()
        {
            try
            {
                return RunArtifactManager.instance != null && RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.Command);
            }
            catch
            {
                return false;
            }
        }

        private static string GetInteractableMultiLineLabel(PurchaseInteraction pi, float dist, out Color overrideColor)
        {
            overrideColor = InteractableColor;
            if (pi == null) return "";

            if (pi.gameObject.name.IndexOf("Void", System.StringComparison.OrdinalIgnoreCase) >= 0)
                overrideColor = new Color(0.8f, 0.3f, 0.8f); // Purple for Void Cradles
            else if (pi.gameObject.name.IndexOf("Blood", System.StringComparison.OrdinalIgnoreCase) >= 0)
                overrideColor = new Color(0.8f, 0.2f, 0.2f); // Red for Blood Shrines
            else if (pi.costType == CostTypeIndex.LunarCoin)
                overrideColor = new Color(0.4f, 0.6f, 0.9f); // Blueish for Lunar interactions

            bool isCloaked = (pi.gameObject.name.IndexOf("Stealthed", System.StringComparison.OrdinalIgnoreCase) >= 0 || pi.gameObject.name.IndexOf("Cloaked", System.StringComparison.OrdinalIgnoreCase) >= 0) || pi.displayNameToken == "CHEST1_STEALTHED_NAME";

            string costStr = "";
            if (pi.cost > 0 && pi.costType != CostTypeIndex.None)
            {
                if (pi.costType == CostTypeIndex.Money)
                    costStr = $" (${pi.cost})";
                else if (pi.costType == CostTypeIndex.LunarCoin)
                    costStr = $" ({pi.cost} Lunar)";
                else if (pi.costType == CostTypeIndex.PercentHealth)
                    costStr = $" ({pi.cost}% HP)";
                else
                    costStr = $" ({GameCostString(pi.costType, pi.cost)})";
            }

            string baseName = ShowNames ? (isCloaked ? "Cloaked Chest" : pi.GetDisplayName()) : "";
            if (string.IsNullOrEmpty(baseName) && ShowNames) baseName = pi.name.Replace("(Clone)", "").Trim();
            if (!string.IsNullOrEmpty(baseName)) baseName += costStr;

            if (isCloaked)
            {
                overrideColor = new Color(0.95f, 0.35f, 0.95f);
            }

            PickupIndex pickupIndex = PickupIndex.none;

            ChestBehavior chest = pi.GetComponent<ChestBehavior>();
            if (chest != null)
            {
                pickupIndex = chest.dropPickup;
            }
            else
            {
                ShopTerminalBehavior terminal = pi.GetComponent<ShopTerminalBehavior>();
                if (terminal != null)
                {
                    pickupIndex = terminal.CurrentPickup().pickupIndex;
                }
            }

            string contentStr = "";
            if (pickupIndex != PickupIndex.none)
            {
                string contentName = GetPickupContentInfo(pickupIndex, out Color itemColor, isInteractable: true);
                if (itemColor != Color.clear && !isCloaked) overrideColor = itemColor;

                if (!string.IsNullOrEmpty(contentName))
                {
                    contentStr = $"[{contentName}]";
                }
            }

            string distStr = ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "";

            return BuildMultiLine(baseName, contentStr, distStr);
        }

        private static readonly Color Dlc4Color = new Color(0.95f, 0.75f, 0.3f);
        private static readonly Color SpecialColor = new Color(0.8f, 0.8f, 0.8f);

        private void DrawSpecialInteractables(Camera cam, Vector3 origin)
        {
            List<IInteractable> list = InterfaceInstanceTracker<IInteractable>.GetInstancesList();
            if (list == null) return;
            Interactor me = PlayerContext.Body != null ? PlayerContext.Body.GetComponent<Interactor>() : null;

            for (int i = 0; i < list.Count; i++)
            {
                MonoBehaviour mb = list[i] as MonoBehaviour;
                if (mb == null || mb.transform == null) continue;

                string fallback = SpecialFallbackName(mb, out Color col, out bool shown);
                if (fallback == null || !shown) continue; // other passes cover the rest

                if (me != null)
                {
                    Interactability state;
                    try { state = list[i].GetInteractability(me); } catch { continue; }
                    if (state == Interactability.Disabled) continue;
                }
                if (Culled(origin, mb.transform.position, out float dist)) continue;

                string name = "";
                if (ShowNames)
                {
                    IDisplayNameProvider dn = mb.GetComponent<IDisplayNameProvider>();
                    try { name = dn != null ? dn.GetDisplayName() : (me != null ? list[i].GetContextString(me) : null); } catch { name = null; }
                    if (string.IsNullOrEmpty(name)) name = fallback;
                    else name = _richTag.Replace(name, "");
                }
                string dStr = ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "";
                DrawMarker(cam, mb.transform.position, BuildMultiLine(name, "", dStr), col);
            }
        }

        private static readonly Color PortalColor = new Color(0.35f, 0.8f, 1f);
        private static readonly Color ObjectiveColor = new Color(0.6f, 0.9f, 0.5f);

        // Whitelist only, taken from the game's prefabs: portals (all DLCs), DLC3 power vaults, Solus Heart offering
        // and Access Codes nodes. Other GenericInteraction / ProxyInteraction users (moon elevators, dialer buttons,
        // Captain beacons, pets) are left out on purpose.
        private void DrawPortalsAndObjectives(Camera cam, Vector3 origin)
        {
            bool objectives = EspInteractables && EspFilterOther;
            if (!EspTeleporter && !objectives) return;

            List<GenericInteraction> generic = InstanceTracker.GetInstancesList<GenericInteraction>();
            for (int i = 0; i < generic.Count; i++)
            {
                GenericInteraction gi = generic[i];
                if (gi == null || !gi.isActiveAndEnabled || gi.interactability == Interactability.Disabled) continue;

                GenericDisplayNameProvider dn = gi.GetComponent<GenericDisplayNameProvider>();
                string token = dn != null ? dn.displayToken : null;
                string ctx = gi.contextToken;
                bool portal = gi.GetComponent<SceneExitController>() != null
                    || (token != null && (token.StartsWith("PORTAL_") || token.StartsWith("ACCESSCODES_PORTAL")));

                string fallback;
                Color col;
                if (portal)
                {
                    if (!EspTeleporter) continue;
                    fallback = "Portal"; col = PortalColor;
                }
                else if (ctx == "PEDESTAL_POWERCUBE_CONTEXT") { fallback = "Power Cube Vault"; col = ObjectiveColor; }
                else if (ctx == "PEDESTAL_POWERORB_CONTEXT") { fallback = "Power Orb Vault"; col = ObjectiveColor; }
                else if (ctx == "PEDESTAL_POWERPYRAMID_CONTEXT") { fallback = "Power Pyramid Vault"; col = ObjectiveColor; }
                else if (ctx == "SOLUSHEART_OFFERING_ACCEPT") { fallback = "Solus Heart"; col = ObjectiveColor; }
                else continue;
                if (!portal && !objectives) continue;

                DrawNamed(cam, origin, gi.transform, dn, fallback, col);
            }

            if (!objectives) return;
            List<ProxyInteraction> proxies = InstanceTracker.GetInstancesList<ProxyInteraction>();
            Interactor me = proxies.Count > 0 && PlayerContext.Body != null ? PlayerContext.Body.GetComponent<Interactor>() : null;
            for (int i = 0; i < proxies.Count; i++)
            {
                ProxyInteraction px = proxies[i];
                if (px == null || !px.isActiveAndEnabled || px.GetComponent<AccessCodesNodeController>() == null) continue;
                if (me != null)
                {
                    Interactability state;
                    try { state = px.GetInteractability(me); } catch { continue; }
                    if (state == Interactability.Disabled) continue;
                }
                DrawNamed(cam, origin, px.transform, px.GetComponent<GenericDisplayNameProvider>(), "Access Codes Node", ObjectiveColor);
            }
        }

        private void DrawNamed(Camera cam, Vector3 origin, Transform t, GenericDisplayNameProvider dn, string fallback, Color col)
        {
            if (t == null || Culled(origin, t.position, out float dist)) return;
            string name = "";
            if (ShowNames)
            {
                try { name = dn != null && !string.IsNullOrEmpty(dn.displayToken) ? dn.GetDisplayName() : null; } catch { name = null; }
                if (string.IsNullOrEmpty(name) || name == dn?.displayToken) name = fallback;
                else name = _richTag.Replace(name, "");
            }
            string dStr = ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "";
            DrawMarker(cam, t.position, BuildMultiLine(name, "", dStr), col);
        }

        private static string SpecialFallbackName(MonoBehaviour mb, out Color col, out bool shown)
        {
            col = SpecialColor;
            shown = EspFilterOther;
            switch (mb)
            {
                // DLC2
                case WokController _: return "Wok";
                case AurelioniteHeartController _: return "Aurelionite Heart";
                // DLC3
                case DroneScrapperPickerController _: shown = EspFilterScrappers; return "Drone Scrapper";
                case TieredPickupPickerController _: shown = EspFilterDrones; return "Drone Assembly Station";
                case PowerPedestal _: return "Power Pedestal";
            }
            col = Dlc4Color;
            switch (mb)
            {
                case WakeController _: return "Wake";
                case WakeDonationController _: return "Wake Offering";
                case WakeStop _: return "Wake Stop";
                case TrialGongInteraction _: return "Trial Gong";
                case GrantHexesController _: return "Hex Altar";
                case DrainItemsAndDropMultiChoicePotentialController _: return "Drain Altar";
                default: return null;
            }
        }

        private static readonly System.Text.StringBuilder _costSb = new System.Text.StringBuilder();
        private static readonly System.Text.RegularExpressions.Regex _richTag = new System.Text.RegularExpressions.Regex("<[^>]*>");

        // Quality mod (Gorakh) appends <sprite name="QualityUncommon|Rare|Epic|Legendary"> to item names.
        // The game's TextMeshPro UI draws it as an icon; IMGUI would print the raw tag.
        private static readonly System.Text.RegularExpressions.Regex _qualitySprite =
            new System.Text.RegularExpressions.Regex("\\s*<sprite\\s+name\\s*=\\s*\"?Quality(\\w+)\"?[^>]*>");

        private static string CleanLabel(string label)
        {
            if (label.IndexOf('<') < 0) return label; // nothing to clean, no allocation
            label = _qualitySprite.Replace(label, " ($1)");
            return _richTag.Replace(label, "");
        }

        // Item, soul, wake flower, hex and other non-money costs use the game's own localized format.
        private static string GameCostString(CostTypeIndex type, int cost)
        {
            try
            {
                CostTypeDef def = CostTypeCatalog.GetCostTypeDef(type);
                if (def != null)
                {
                    _costSb.Clear();
                    def.BuildCostString(cost, _costSb);
                    string s = _richTag.Replace(_costSb.ToString(), "").Trim();
                    if (s.Length > 0) return s;
                }
            }
            catch { }
            return $"{cost} {type}";
        }

        private static string GetPickupMultiLineLabel(GenericPickupController gpc, float dist, out Color overrideColor)
        {
            overrideColor = InteractableColor;
            if (gpc == null) return "";

            bool isVoidPotential = gpc.gameObject.name.IndexOf("OptionPickup", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (isVoidPotential)
            {
                overrideColor = new Color(0.8f, 0.3f, 0.8f); // Purple
                string pName = "Void Potential";
                PickupPickerController picker = gpc.GetComponent<PickupPickerController>();
                if (picker != null)
                {
                    string contextStr = picker.GetContextString(null);
                    if (!string.IsNullOrEmpty(contextStr)) pName = contextStr;
                }
                return BuildMultiLine(ShowNames ? pName : "", "", ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "");
            }

            bool isUnchosenCommand = IsCommandArtifactActive() && (gpc.GetComponent<PickupPickerController>() != null);
            string contentName = GetPickupContentInfo(gpc.pickupIndex, out Color itemCol, isInteractable: false, isUnchosenCommandCube: isUnchosenCommand);
            if (itemCol != Color.clear) overrideColor = itemCol;

            string baseName = ShowNames ? (!string.IsNullOrEmpty(contentName) ? contentName : "Pickup") : "";
            string distStr = ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "";

            return BuildMultiLine(baseName, "", distStr);
        }

        private static string GetPickupContentInfo(PickupIndex pickupIndex, out Color contentColor, bool isInteractable = false, bool isUnchosenCommandCube = false)
        {
            contentColor = InteractableColor;
            if (pickupIndex == PickupIndex.none) return null;

            try
            {
                PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                if (pickupDef == null) return null;

                contentColor = pickupDef.baseColor != Color.clear ? pickupDef.baseColor : InteractableColor;

                bool isCommandActive = IsCommandArtifactActive();

                // If Artifact of Command is active and this is an interactable (chest/terminal) or an unchosen Command Cube
                if (isCommandActive && (isInteractable || isUnchosenCommandCube || (pickupDef.itemIndex == ItemIndex.None && pickupDef.equipmentIndex == EquipmentIndex.None)))
                {
                    return GetTierRarityName(pickupDef.itemTier);
                }

                // Otherwise, get the specific item or equipment name (for normal items or items already chosen from Command cubes)
                if (pickupDef.itemIndex != ItemIndex.None || pickupDef.equipmentIndex != EquipmentIndex.None)
                {
                    string name = Language.GetString(pickupDef.nameToken);
                    if (string.IsNullOrEmpty(name) || name == pickupDef.nameToken)
                        name = pickupDef.internalName;
                    return name;
                }

                if (pickupDef.itemTier != ItemTier.AssignedAtRuntime)
                {
                    return GetTierRarityName(pickupDef.itemTier);
                }

                string fallbackName = Language.GetString(pickupDef.nameToken);
                if (string.IsNullOrEmpty(fallbackName) || fallbackName == pickupDef.nameToken)
                    fallbackName = pickupDef.internalName;
                return fallbackName;
            }
            catch
            {
                return null;
            }
        }

        private static string GetTierRarityName(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Tier1: return "Common";
                case ItemTier.Tier2: return "Uncommon";
                case ItemTier.Tier3: return "Legendary";
                case ItemTier.Boss: return "Boss";
                case ItemTier.Lunar: return "Lunar";
                case ItemTier.VoidTier1:
                case ItemTier.VoidTier2:
                case ItemTier.VoidTier3:
                case ItemTier.VoidBoss: return "Void";
                case ItemTier.FoodTier: return "Food";
                case ItemTier.Cursed: return "Cursed";
                case ItemTier.Page: return "Page";
                case ItemTier.Hex: return "Hex";
                default: return "Equipment";
            }
        }

        private static bool Culled(Vector3 origin, Vector3 pos, out float dist)
        {
            dist = Vector3.Distance(origin, pos);
            if (MaxDistance > 0f && dist > MaxDistance) return true;
            return false;
        }

        private static string EnemyMultiLineLabel(CharacterBody body, float dist)
        {
            string nameStr = ShowNames ? body.GetDisplayName() : "";
            string hpStr = (ShowEnemyHealth && body.healthComponent != null) ? $"{Mathf.CeilToInt(body.healthComponent.combinedHealth)} hp" : "";
            string distStr = ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "";

            return BuildMultiLine(nameStr, hpStr, distStr);
        }

        private static string TeleporterMultiLineLabel(TeleporterInteraction tp, float dist)
        {
            string nameStr = ShowNames ? "Teleporter" : "";
            string stateStr = tp.isCharged ? "(Charged)" : (tp.isCharging ? $"(Charging {Mathf.FloorToInt(tp.chargeFraction * 100)}%)" : "");
            string distStr = ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "";

            return BuildMultiLine(nameStr, stateStr, distStr);
        }

        private static string BuildMultiLine(string line1, string line2, string line3)
        {
            if (string.IsNullOrEmpty(line2) && string.IsNullOrEmpty(line3)) return line1;
            if (string.IsNullOrEmpty(line1) && string.IsNullOrEmpty(line3)) return line2;
            if (string.IsNullOrEmpty(line1) && string.IsNullOrEmpty(line2)) return line3;

            if (string.IsNullOrEmpty(line2)) return line1 + "\n" + line3;
            if (string.IsNullOrEmpty(line3)) return line1 + "\n" + line2;
            if (string.IsNullOrEmpty(line1)) return line2 + "\n" + line3;

            return line1 + "\n" + line2 + "\n" + line3;
        }

        private void DrawMarker(Camera cam, Vector3 worldPos, string label, Color color)
        {
            Vector3 sp = cam.WorldToScreenPoint(worldPos);
            if (sp.z <= 0f) return;
            float y = Screen.height - sp.y;
            float half = MarkerSize * 0.5f;

            Theme.Fill(new Rect(sp.x - half, y - half, MarkerSize, MarkerSize), color);

            if (string.IsNullOrEmpty(label)) return;
            label = CleanLabel(label);

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperCenter,
                    fontStyle = FontStyle.Bold,
                    wordWrap = false
                };
            }

            int fSize = Mathf.Clamp(Mathf.RoundToInt(FontSize), 8, 30);
            _labelStyle.fontSize = fSize;

            GUIContent content = new GUIContent(label);
            Vector2 size = _labelStyle.CalcSize(content);

            float labelWidth = Mathf.Max(size.x + 24f, 400f);
            float labelHeight = size.y + 12f;

            Rect labelRect = new Rect(sp.x - labelWidth * 0.5f, y + half + 2f, labelWidth, labelHeight);

            if (ShowOutline)
            {
                _labelStyle.normal.textColor = Color.black;
                GUI.Label(new Rect(labelRect.x - 1f, labelRect.y, labelRect.width, labelRect.height), content, _labelStyle);
                GUI.Label(new Rect(labelRect.x + 1f, labelRect.y, labelRect.width, labelRect.height), content, _labelStyle);
                GUI.Label(new Rect(labelRect.x, labelRect.y - 1f, labelRect.width, labelRect.height), content, _labelStyle);
                GUI.Label(new Rect(labelRect.x, labelRect.y + 1f, labelRect.width, labelRect.height), content, _labelStyle);
            }

            _labelStyle.normal.textColor = color;
            GUI.Label(labelRect, content, _labelStyle);
        }
    }
}
