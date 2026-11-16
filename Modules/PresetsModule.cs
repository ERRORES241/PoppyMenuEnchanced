using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Newtonsoft.Json;
using UnityEngine;

namespace PoppyMenu
{
    internal class ConfigsModule : PoppyModule
    {
        internal override string Name => "Configs";

        private static string _newConfigName = "";
        private static string _pendingDelete;
        // Rename text per profile. Renaming happens on the button, not on every keystroke,
        // so a half-typed name can never overwrite another profile.
        private static readonly Dictionary<string, string> _renameText = new Dictionary<string, string>();

        private static string ConfigsFolder => Path.Combine(Paths.ConfigPath, "PoppyMenuConfigs");

        private static void EnsureFolderExists()
        {
            if (!Directory.Exists(ConfigsFolder))
                Directory.CreateDirectory(ConfigsFolder);
        }

        // Cached: DrawMenu runs several times per frame and must not hit the disk each time.
        private static List<ConfigProfile> _profileCache;

        internal static List<ConfigProfile> GetSavedProfiles()
        {
            if (_profileCache != null) return _profileCache;
            EnsureFolderExists();
            List<ConfigProfile> list = new List<ConfigProfile>();
            string[] files = Directory.GetFiles(ConfigsFolder, "*.json");
            foreach (string file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    ConfigProfile profile = JsonConvert.DeserializeObject<ConfigProfile>(json);
                    if (profile != null)
                    {
                        if (string.IsNullOrEmpty(profile.Name))
                            profile.Name = Path.GetFileNameWithoutExtension(file);
                        list.Add(profile);
                    }
                }
                catch { }
            }
            _profileCache = list;
            return list;
        }

        internal static void SaveProfile(ConfigProfile profile)
        {
            EnsureFolderExists();
            if (profile == null || string.IsNullOrWhiteSpace(profile.Name)) return;

            string safeName = SanitizeFileName(profile.Name.Trim());
            string path = Path.Combine(ConfigsFolder, safeName + ".json");
            try { File.WriteAllText(path, JsonConvert.SerializeObject(profile, Formatting.Indented)); }
            catch (System.Exception e) { Log.Error("Save config failed: " + e); Notify.Push("Save failed"); }
            _profileCache = null;
        }

        internal static void DeleteProfile(string name)
        {
            EnsureFolderExists();
            if (string.IsNullOrWhiteSpace(name)) return;
            string safeName = SanitizeFileName(name.Trim());
            string path = Path.Combine(ConfigsFolder, safeName + ".json");
            try { if (File.Exists(path)) File.Delete(path); }
            catch (System.Exception e) { Log.Error("Delete config failed: " + e); }
            _profileCache = null;
        }

        internal static bool RenameProfile(ConfigProfile profile, string newName)
        {
            if (profile == null || string.IsNullOrWhiteSpace(newName)) return false;
            newName = newName.Trim();
            string target = SanitizeFileName(newName);
            if (target == SanitizeFileName(profile.Name ?? "")) { profile.Name = newName; SaveProfile(profile); return true; }
            foreach (ConfigProfile other in GetSavedProfiles())
            {
                if (other != profile && SanitizeFileName(other.Name ?? "").Equals(target, StringComparison.OrdinalIgnoreCase))
                {
                    Notify.Push("A config named " + newName + " already exists");
                    return false;
                }
            }
            DeleteProfile(profile.Name);
            profile.Name = newName;
            SaveProfile(profile);
            return true;
        }

        internal static void SetDefaultStartup(ConfigProfile target)
        {
            List<ConfigProfile> profiles = GetSavedProfiles();
            foreach (ConfigProfile p in profiles)
            {
                p.IsDefaultStartup = (p.Name == target.Name && target.IsDefaultStartup);
                SaveProfile(p);
            }
        }

        internal static void ApplyStartupConfig()
        {
            List<ConfigProfile> profiles = GetSavedProfiles();
            foreach (ConfigProfile p in profiles)
            {
                if (p.IsDefaultStartup)
                {
                    p.Apply();
                    Notify.Push("Loaded startup config: " + p.Name);
                    break;
                }
            }
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }

        internal override void DrawMenu()
        {
            Widgets.SectionBegin("Configuration Manager");
            Widgets.Hint("Save, load, rename, or delete your custom feature & ESP profiles.");

            GUILayout.BeginHorizontal();
            _newConfigName = GUILayout.TextField(_newConfigName ?? "", Theme.Search);
            if (GUILayout.Button("+ Save Current", Theme.Primary, GUILayout.Width(130)))
            {
                string name = string.IsNullOrWhiteSpace(_newConfigName) ? "Config_" + DateTime.Now.ToString("HHmmss") : _newConfigName.Trim();
                ConfigProfile p = ConfigProfile.CaptureCurrent(name);
                SaveProfile(p);
                _newConfigName = "";
                Notify.Push("Saved config: " + p.Name);
            }
            GUILayout.EndHorizontal();
            Widgets.SectionEnd();

            List<ConfigProfile> profiles = GetSavedProfiles();
            if (profiles.Count == 0)
            {
                Widgets.SectionBegin("Saved Configs");
                Widgets.Label("No saved configs yet. Enter a name above and click '+ Save Current'.");
                Widgets.SectionEnd();
                return;
            }

            foreach (ConfigProfile p in profiles)
            {
                ConfigProfile profile = p;
                Widgets.SectionBegin(null);

                GUILayout.BeginHorizontal();
                string key = profile.Name ?? "";
                if (!_renameText.TryGetValue(key, out string text)) text = key;
                text = GUILayout.TextField(text, Theme.Search);
                _renameText[key] = text;
                if (text != key && !string.IsNullOrWhiteSpace(text)
                    && GUILayout.Button("Rename", Theme.Button, GUILayout.Width(70)))
                {
                    if (RenameProfile(profile, text))
                    {
                        _renameText.Remove(key);
                        Notify.Push("Renamed to " + profile.Name);
                    }
                }

                if (GUILayout.Button("Load", Theme.Primary, GUILayout.Width(64)))
                {
                    profile.Apply();
                    Notify.Push("Loaded config: " + profile.Name);
                }

                if (GUILayout.Button("Overwrite", Theme.Button, GUILayout.Width(80)))
                {
                    ConfigProfile updated = ConfigProfile.CaptureCurrent(profile.Name);
                    updated.IsDefaultStartup = profile.IsDefaultStartup;
                    SaveProfile(updated);
                    Notify.Push("Overwrote config: " + profile.Name);
                }

                if (GUILayout.Button("Delete", Theme.Danger_, GUILayout.Width(64)))
                {
                    _pendingDelete = profile.Name;
                }
                GUILayout.EndHorizontal();

                bool isDefault = profile.IsDefaultStartup;
                bool newDefault = Widgets.Toggle("Load on startup (Default profile)", isDefault);
                if (newDefault != isDefault)
                {
                    profile.IsDefaultStartup = newDefault;
                    SetDefaultStartup(profile);
                }

                Widgets.SectionEnd();
            }

            if (!string.IsNullOrEmpty(_pendingDelete))
            {
                DeleteProfile(_pendingDelete);
                Notify.Push("Deleted config: " + _pendingDelete);
                _pendingDelete = null;
            }
        }
    }

    internal class ConfigProfile
    {
        public string Name { get; set; }
        public bool IsDefaultStartup { get; set; }

        public bool AimActive { get; set; }
        public bool AimTargetWeakPoints { get; set; }
        public bool AimNoSpread { get; set; }
        public bool AimNoRecoil { get; set; }
        public bool AimSticky { get; set; }
        public bool AimBossesOnly { get; set; }
        public bool AimCheckLos { get; set; }
        public bool AimMagicBullet { get; set; }
        public bool AimUseFov { get; set; }
        public bool AimDrawFov { get; set; }
        public float AimFovRadius { get; set; }
        public float AimMaxRange { get; set; }

        public bool EspMobs { get; set; }
        public bool EspInteractables { get; set; }
        public bool EspTeleporter { get; set; }
        public bool EspShowNames { get; set; }
        public bool EspShowDistance { get; set; }
        public bool EspShowHealth { get; set; }
        public bool EspShowOutline { get; set; }
        public float EspFontSize { get; set; }
        public float EspMaxDistance { get; set; }
        public float EspMarkerSize { get; set; }

        public bool MovementFlight { get; set; }
        public bool MovementNoclip { get; set; }
        public bool MovementAlwaysSprint { get; set; }
        public bool MovementJumpPack { get; set; }
        public bool MovementBhop { get; set; }
        public bool MovementTeleportToPing { get; set; }

        public bool GodMode { get; set; }
        public bool BuddhaMode { get; set; }
        public bool InfiniteSkills { get; set; }
        public bool NoEquipCooldown { get; set; }

        public bool StatDmgOn { get; set; }
        public float StatDmgMult { get; set; }
        public bool StatAtkOn { get; set; }
        public float StatAtkMult { get; set; }
        public bool StatMoveOn { get; set; }
        public float StatMoveMult { get; set; }
        public bool StatArmorOn { get; set; }
        public float StatArmorBonus { get; set; }
        public bool StatCritOn { get; set; }
        public float StatCritBonus { get; set; }
        public bool StatHpOn { get; set; }
        public float StatHpMult { get; set; }
        public bool GuaranteedShrineChance { get; set; }

        // Added in 2.7.0. Nullable: missing in older presets, then left unchanged on load.
        public int? AimSorting { get; set; }
        public bool? AimHighlight { get; set; }
        public bool? EspFilterChests { get; set; }
        public bool? EspFilterShrines { get; set; }
        public bool? EspFilterPrinters { get; set; }
        public bool? EspFilterScrappers { get; set; }
        public bool? EspFilterDrones { get; set; }
        public bool? EspFilterBarrels { get; set; }
        public bool? EspFilterOther { get; set; }
        public bool? FovOverride { get; set; }
        public float? FovValue { get; set; }
        public float? FlightSpeed { get; set; }
        public bool? SaleStarCheat { get; set; }
        public int? SaleStarDropCount { get; set; }
        public int? GiveCount { get; set; }
        public bool? InfiniteShrineChance { get; set; }
        public bool? TeleporterInstaCharge { get; set; }
        public bool? NoEnemies { get; set; }
        public bool? LockExp { get; set; }
        public bool? PreventProfileWriting { get; set; }
        public bool? RailgunnerInstaCharge { get; set; }
        public bool? RailgunnerNoUltCooldown { get; set; }
        public bool? RailgunnerUltSpam { get; set; }

        public static ConfigProfile CaptureCurrent(string name)
        {
            return new ConfigProfile
            {
                Name = name,
                AimActive = Aim.Enabled,
                AimTargetWeakPoints = Aim.TargetWeakPoints,
                AimNoSpread = Aim.NoSpread,
                AimNoRecoil = Aim.NoRecoil,
                AimSticky = Aim.Sticky,
                AimBossesOnly = Aim.PrioritizeBosses,
                AimCheckLos = Aim.RequireLoS,
                AimMagicBullet = Aim.MagicBullet,
                AimUseFov = Aim.UseFov,
                AimDrawFov = Aim.ShowFovCircle,
                AimFovRadius = Aim.Fov,
                AimMaxRange = Aim.MaxRange,

                EspMobs = RenderModule.EspMobs,
                EspInteractables = RenderModule.EspInteractables,
                EspTeleporter = RenderModule.EspTeleporter,
                EspShowNames = RenderModule.ShowNames,
                EspShowDistance = RenderModule.ShowDistance,
                EspShowHealth = RenderModule.ShowEnemyHealth,
                EspShowOutline = RenderModule.ShowOutline,
                EspFontSize = RenderModule.FontSize,
                EspMaxDistance = RenderModule.MaxDistance,
                EspMarkerSize = RenderModule.MarkerSize,

                MovementFlight = MovementModule.Flight,
                MovementNoclip = MovementModule.NoClip,
                MovementAlwaysSprint = MovementModule.AlwaysSprint,
                MovementJumpPack = MovementModule.JumpPack,
                MovementBhop = MovementModule.Bhop,
                MovementTeleportToPing = MovementModule.TeleportToPing,

                GodMode = PlayerModule.GodMode,
                BuddhaMode = Safety.Buddha,
                InfiniteSkills = PlayerModule.InfiniteSkills,
                NoEquipCooldown = ItemsModule.NoEquipmentCooldown,

                StatDmgOn = StatsModule.DamageOn,
                StatDmgMult = StatsModule.DamageMult,
                StatAtkOn = StatsModule.AttackSpeedOn,
                StatAtkMult = StatsModule.AttackSpeedMult,
                StatMoveOn = StatsModule.MoveSpeedOn,
                StatMoveMult = StatsModule.MoveSpeedMult,
                StatArmorOn = StatsModule.ArmorOn,
                StatArmorBonus = StatsModule.ArmorBonus,
                StatCritOn = StatsModule.CritOn,
                StatCritBonus = StatsModule.CritBonus,
                StatHpOn = StatsModule.MaxHealthOn,
                StatHpMult = StatsModule.MaxHealthMult,
                GuaranteedShrineChance = WorldModule.GuaranteedShrineChance,

                AimSorting = Aim.Sorting,
                AimHighlight = Aim.Highlight,
                EspFilterChests = RenderModule.EspFilterChests,
                EspFilterShrines = RenderModule.EspFilterShrines,
                EspFilterPrinters = RenderModule.EspFilterPrinters,
                EspFilterScrappers = RenderModule.EspFilterScrappers,
                EspFilterDrones = RenderModule.EspFilterDrones,
                EspFilterBarrels = RenderModule.EspFilterBarrels,
                EspFilterOther = RenderModule.EspFilterOther,
                FovOverride = RenderModule.FovOverride,
                FovValue = RenderModule.FovValue,
                FlightSpeed = ModConfig.FlightSpeed.Value,
                SaleStarCheat = ItemsModule.SaleStarCheat,
                SaleStarDropCount = ItemsModule.SaleStarDropCount,
                GiveCount = ItemsModule.GiveCount,
                InfiniteShrineChance = WorldModule.InfiniteShrineChance,
                TeleporterInstaCharge = TeleporterModule.InstaCharge,
                NoEnemies = Safety.NoEnemies,
                LockExp = Safety.LockExp,
                PreventProfileWriting = Safety.PreventProfileWriting,
                RailgunnerInstaCharge = FunModule.RailgunnerInstaCharge,
                RailgunnerNoUltCooldown = FunModule.RailgunnerNoUltCooldown,
                RailgunnerUltSpam = FunModule.RailgunnerUltSpam
            };
        }

        public void Apply()
        {
            Aim.Enabled = AimActive;
            Aim.Active = AimActive;
            Aim.TargetWeakPoints = AimTargetWeakPoints;
            Aim.NoSpread = AimNoSpread;
            Aim.NoRecoil = AimNoRecoil;
            Aim.Sticky = AimSticky;
            Aim.PrioritizeBosses = AimBossesOnly;
            Aim.RequireLoS = AimCheckLos;
            Aim.MagicBullet = AimMagicBullet;
            Aim.UseFov = AimUseFov;
            Aim.ShowFovCircle = AimDrawFov;
            Aim.Fov = AimFovRadius;
            Aim.MaxRange = AimMaxRange;

            RenderModule.EspMobs = EspMobs;
            RenderModule.EspInteractables = EspInteractables;
            RenderModule.EspTeleporter = EspTeleporter;
            RenderModule.ShowNames = EspShowNames;
            RenderModule.ShowDistance = EspShowDistance;
            RenderModule.ShowEnemyHealth = EspShowHealth;
            RenderModule.ShowOutline = EspShowOutline;
            RenderModule.FontSize = EspFontSize;
            RenderModule.MaxDistance = EspMaxDistance;
            RenderModule.MarkerSize = EspMarkerSize;

            MovementModule.Flight = MovementFlight;
            MovementModule.NoClip = MovementNoclip;
            MovementModule.AlwaysSprint = MovementAlwaysSprint;
            MovementModule.JumpPack = MovementJumpPack;
            MovementModule.Bhop = MovementBhop;
            MovementModule.TeleportToPing = MovementTeleportToPing;

            PlayerModule.GodMode = GodMode;
            Safety.Buddha = BuddhaMode;
            PlayerModule.InfiniteSkills = InfiniteSkills;
            ItemsModule.NoEquipmentCooldown = NoEquipCooldown;

            StatsModule.DamageOn = StatDmgOn;
            StatsModule.DamageMult = StatDmgMult;
            StatsModule.AttackSpeedOn = StatAtkOn;
            StatsModule.AttackSpeedMult = StatAtkMult;
            StatsModule.MoveSpeedOn = StatMoveOn;
            StatsModule.MoveSpeedMult = StatMoveMult;
            StatsModule.ArmorOn = StatArmorOn;
            StatsModule.ArmorBonus = StatArmorBonus;
            StatsModule.CritOn = StatCritOn;
            StatsModule.CritBonus = StatCritBonus;
            StatsModule.MaxHealthOn = StatHpOn;
            StatsModule.MaxHealthMult = StatHpMult;
            WorldModule.GuaranteedShrineChance = GuaranteedShrineChance;

            if (AimSorting.HasValue) Aim.Sorting = AimSorting.Value;
            if (AimHighlight.HasValue) Aim.Highlight = AimHighlight.Value;
            if (EspFilterChests.HasValue) RenderModule.EspFilterChests = EspFilterChests.Value;
            if (EspFilterShrines.HasValue) RenderModule.EspFilterShrines = EspFilterShrines.Value;
            if (EspFilterPrinters.HasValue) RenderModule.EspFilterPrinters = EspFilterPrinters.Value;
            if (EspFilterScrappers.HasValue) RenderModule.EspFilterScrappers = EspFilterScrappers.Value;
            if (EspFilterDrones.HasValue) RenderModule.EspFilterDrones = EspFilterDrones.Value;
            if (EspFilterBarrels.HasValue) RenderModule.EspFilterBarrels = EspFilterBarrels.Value;
            if (EspFilterOther.HasValue) RenderModule.EspFilterOther = EspFilterOther.Value;
            if (FovOverride.HasValue) RenderModule.FovOverride = FovOverride.Value;
            if (FovValue.HasValue) RenderModule.FovValue = FovValue.Value;
            if (FlightSpeed.HasValue) ModConfig.FlightSpeed.Value = FlightSpeed.Value;
            if (SaleStarCheat.HasValue) ItemsModule.SaleStarCheat = SaleStarCheat.Value;
            if (SaleStarDropCount.HasValue) ItemsModule.SaleStarDropCount = SaleStarDropCount.Value;
            if (GiveCount.HasValue) ItemsModule.GiveCount = GiveCount.Value;
            if (InfiniteShrineChance.HasValue) WorldModule.InfiniteShrineChance = InfiniteShrineChance.Value;
            if (TeleporterInstaCharge.HasValue) TeleporterModule.InstaCharge = TeleporterInstaCharge.Value;
            if (NoEnemies.HasValue) Safety.NoEnemies = NoEnemies.Value;
            if (LockExp.HasValue) Safety.LockExp = LockExp.Value;
            if (PreventProfileWriting.HasValue) Safety.PreventProfileWriting = PreventProfileWriting.Value;
            if (RailgunnerInstaCharge.HasValue) FunModule.RailgunnerInstaCharge = RailgunnerInstaCharge.Value;
            if (RailgunnerNoUltCooldown.HasValue) FunModule.RailgunnerNoUltCooldown = RailgunnerNoUltCooldown.Value;
            if (RailgunnerUltSpam.HasValue) FunModule.RailgunnerUltSpam = RailgunnerUltSpam.Value;

            ModConfig.Save();
        }
    }
}
