using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace PoppyMenu
{
    internal static class ActionRegistry
    {
        private static readonly List<PoppyAction> _all = new List<PoppyAction>();
        private static readonly Dictionary<string, PoppyAction> _byId = new Dictionary<string, PoppyAction>();

        internal static IReadOnlyList<PoppyAction> All => _all;

        static ActionRegistry() => Build();

        internal static PoppyAction Get(string id) => id != null && _byId.TryGetValue(id, out var a) ? a : null;

        internal static void Run(string id)
        {
            PoppyAction a = Get(id);
            if (a == null) return;
            try { a.Invoke(); }
            catch (Exception e) { Log.Error($"action {id} failed: {e}"); }
        }

        internal static void RunId(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (id.StartsWith(MacroStore.BindPrefix)) { MacroStore.Run(id.Substring(MacroStore.BindPrefix.Length)); return; }
            Run(id);
        }

        private static void Add(string id, string category, string name, Action fn)
        {
            var a = new PoppyAction(id, category, name, fn);
            _all.Add(a);
            _byId[id] = a;
        }

        private static void Build()
        {
            Add("menu.disableAll", "Menu", "Disable everything", Cheats.DisableAll);

            Add("player.god", "Player", "God mode", () => PlayerModule.GodMode = !PlayerModule.GodMode);
            Add("player.buddha", "Player", "Semi-god mode", () => Safety.Buddha = !Safety.Buddha);
            Add("player.skills", "Player", "No skill cooldowns", () => PlayerModule.InfiniteSkills = !PlayerModule.InfiniteSkills);
            Add("player.healFull", "Player", "Heal to full", () => NetUtil.Do(PoppyOp.HealFull));
            Add("player.respawn", "Player", "Respawn", () => NetUtil.Do(PoppyOp.Respawn));

            Add("move.flight", "Movement", "Flight", MovementModule.ToggleFlight);
            Add("move.noclip", "Movement", "No-clip", MovementModule.ToggleNoClip);
            Add("move.sprint", "Movement", "Always sprint", MovementModule.ToggleSprint);
            Add("move.jump", "Movement", "Jump pack", () => MovementModule.JumpPack = !MovementModule.JumpPack);
            Add("move.bhop", "Movement", "Bunny hop", () => MovementModule.Bhop = !MovementModule.Bhop);
            Add("move.tp_ping_auto", "Movement", "Teleport on ping", MovementModule.ToggleTeleportToPing);
            Add("move.tp_ping", "Movement", "Teleport to ping", MovementModule.TeleportToCurrentPing);
            Add("move.tp_cursor", "Movement", "Teleport to cursor", MovementModule.TeleportToCursor);

            Add("aim.toggle", "Aimbot", "Aimbot", () => Aim.Enabled = !Aim.Enabled);
            Add("aim.magic", "Aimbot", "Magic bullet", () => Aim.MagicBullet = !Aim.MagicBullet);

            Add("items.giveAll", "Items", "Give all items", () => NetUtil.Do(PoppyOp.GiveAllItems, i2: 1));
            Add("items.clear", "Items", "Clear inventory", () => NetUtil.Do(PoppyOp.ClearInventory));
            Add("items.stack", "Items", "Stack inventory", () => NetUtil.Do(PoppyOp.StackInventory));
            Add("items.reroll", "Items", "Reroll items", () => NetUtil.Do(PoppyOp.RollItems));
            Add("items.noEquipCd", "Items", "No equipment cooldown", () => ItemsModule.NoEquipmentCooldown = !ItemsModule.NoEquipmentCooldown);

            Add("combat.killAll", "World", "Kill all enemies", () => NetUtil.Do(PoppyOp.KillAllEnemies));
            Add("combat.noEnemies", "Safety", "No enemies", () => Safety.NoEnemies = !Safety.NoEnemies);

            Add("tp.charge", "Teleporter", "Instant charge", () => TeleporterModule.InstaCharge = !TeleporterModule.InstaCharge);
            Add("tp.skip", "Teleporter", "Skip stage", () => NetUtil.Do(PoppyOp.SkipStage));
            Add("tp.portals", "Teleporter", "Spawn all portals", () =>
            {
                // Around you, every destination portal right away (not on teleporter completion).
                Vector3 pos = PlayerContext.HasBody ? PlayerContext.Body.footPosition
                    : (TeleporterInteraction.instance != null ? TeleporterInteraction.instance.transform.position : Vector3.zero);
                NetUtil.Do(PoppyOp.SpawnPortals, f1: pos.x, f2: pos.y, f3: pos.z);
            });

            Add("world.freeze", "World", "Freeze game", () => WorldModule.FreezeMatch = !WorldModule.FreezeMatch);
            Add("world.freezeTimer", "World", "Freeze timer", () => WorldModule.FreezeTimer = !WorldModule.FreezeTimer);

            Add("esp.enemies", "ESP", "Enemies", () => RenderModule.EspMobs = !RenderModule.EspMobs);
            Add("esp.interactables", "ESP", "Interactables", () => RenderModule.EspInteractables = !RenderModule.EspInteractables);
            Add("esp.teleporter", "ESP", "Teleporter and portals", () => RenderModule.EspTeleporter = !RenderModule.EspTeleporter);

            Add("fun.instaCharge", "Fun", "Instant ult charge", () => FunModule.RailgunnerInstaCharge = !FunModule.RailgunnerInstaCharge);
            Add("fun.noUltCooldown", "Fun", "No ult cooldown", () => FunModule.RailgunnerNoUltCooldown = !FunModule.RailgunnerNoUltCooldown);
            Add("fun.ultSpam", "Fun", "Every shot is an ult", () => FunModule.RailgunnerUltSpam = !FunModule.RailgunnerUltSpam);

            Add("macro.midgame", "Macros", "Mid-game loadout", () =>
            {
                GiveRandom(20, ItemTier.Tier1);
                GiveRandom(8, ItemTier.Tier2);
                GiveRandom(2, ItemTier.Tier3);
                NetUtil.Do(PoppyOp.GiveMoney, i1: 1000);
                Notify.Push("Mid-game loadout given");
            });
            Add("macro.lategame", "Macros", "End-game loadout", () =>
            {
                GiveRandom(40, ItemTier.Tier1);
                GiveRandom(20, ItemTier.Tier2);
                GiveRandom(8, ItemTier.Tier3);
                GiveRandom(1, ItemTier.Boss);
                NetUtil.Do(PoppyOp.GiveMoney, i1: 10000);
                Notify.Push("End-game loadout given");
            });
            Add("macro.dtzoom", "Macros", "Zoom items", () =>
            {
                // Item defs, not localized names: the lookup must work in any game language.
                NetUtil.Do(PoppyOp.GiveItem, i1: (int)RoR2Content.Items.Hoof.itemIndex, i2: 20);
                NetUtil.Do(PoppyOp.GiveItem, i1: (int)RoR2Content.Items.Feather.itemIndex, i2: 200);
                Notify.Push("Zoom items given");
            });

            Add("tab.player", "Open tab", "Player tab", () => MenuRoot.SelectTabByName("Player"));
            Add("tab.items", "Open tab", "Items tab", () => MenuRoot.SelectTabByName("Items"));
            Add("tab.players", "Open tab", "Players tab", () => MenuRoot.SelectTabByName("Players"));
            Add("tab.fun", "Open tab", "Fun tab", () => MenuRoot.SelectTabByName("Fun"));
            Add("tab.keybinds", "Open tab", "Keybinds tab", () => MenuRoot.SelectTabByName("Keybinds"));
        }

        private static void GiveRandom(int count, ItemTier? tier)
        {
            var pool = new List<int>();
            foreach (var e in Catalogs.Items)
                if (tier == null || e.Tier == tier.Value) pool.Add((int)e.Index);
            if (pool.Count == 0) return;
            count = Mathf.Clamp(count, 1, 1000);
            for (int i = 0; i < count; i++)
                NetUtil.Do(PoppyOp.GiveItem, i1: pool[UnityEngine.Random.Range(0, pool.Count)], i2: 1);
        }

    }
}
