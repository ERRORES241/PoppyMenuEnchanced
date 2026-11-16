using RoR2;
using UnityEngine;

namespace PoppyMenu
{
    internal static class Cheats
    {
        internal static void DisableAll()
        {
            PlayerModule.GodMode = PlayerModule.InfiniteSkills = false;
            Safety.Buddha = Safety.NoEnemies = Safety.LockExp = false;
            Aim.Enabled = false; Aim.MagicBullet = false; Aim.NoSpread = false; Aim.NoRecoil = false; Aim.Target = null;
            MovementModule.Flight = MovementModule.NoClip = MovementModule.AlwaysSprint = MovementModule.JumpPack = false;
            MovementModule.Bhop = MovementModule.TeleportToPing = false;
            ItemsModule.NoEquipmentCooldown = false;
            ItemsModule.SaleStarCheat = false;
            RenderModule.EspMobs = RenderModule.EspInteractables = RenderModule.EspTeleporter = false;
            RenderModule.FovOverride = false;
            StatsModule.DisableAll();
            FunModule.RailgunnerInstaCharge = FunModule.RailgunnerNoUltCooldown = FunModule.RailgunnerUltSpam = false;
            TeleporterModule.InstaCharge = false;
            WorldModule.FreezeMatch = false; WorldModule.FreezeTimer = false; WorldModule.TimeScale = 1f;
            WorldModule.GuaranteedShrineChance = false; WorldModule.InfiniteShrineChance = false;
            Notify.Push("All features disabled");
        }
    }
}
