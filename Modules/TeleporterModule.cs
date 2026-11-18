using RoR2;
using UnityEngine;

namespace PoppyMenu
{
    internal class TeleporterModule : PoppyModule
    {
        internal override string Name => "Teleport";

        internal static bool InstaCharge;

        private static float _nextCharge;

        internal override void Tick()
        {
            // Only while a player zone is actually charging, and throttled: an idle teleporter used to send a request every frame.
            if (!InstaCharge || Time.unscaledTime < _nextCharge || !AnyZoneCharging()) return;
            _nextCharge = Time.unscaledTime + 0.5f;
            NetUtil.Do(PoppyOp.ChargeTeleporter);
        }

        private static bool AnyZoneCharging()
        {
            var zones = InstanceTracker.GetInstancesList<HoldoutZoneController>();
            for (int i = 0; i < zones.Count; i++)
            {
                HoldoutZoneController z = zones[i];
                if (z != null && z.chargingTeam == TeamIndex.Player && !z.isFullyCharged) return true;
            }
            return false;
        }

        // World / Stage
        internal static void DrawStage()
        {
            Widgets.SectionBegin("Teleporter", run: true);
            TeleporterInteraction tp = PlayerContext.InGame ? TeleporterInteraction.instance : null;
            if (PlayerContext.InGame)
                Widgets.Value("Charge", tp != null ? Mathf.FloorToInt(tp.chargeFraction * 100f) + "%" : "No teleporter");
            InstaCharge = Widgets.Toggle("Instant charge", InstaCharge,
                "Also charges Void Fields cells, Void Locus signals and moon pillars.");
            Widgets.Button("Add Mountain Shrine", "Same as using a Shrine of the Mountain: one more boss, more loot.", () => NetUtil.Do(PoppyOp.AddMountainShrine));
            Widgets.Button("Spawn all portals", "Every destination portal appears around you right away.", () => ActionRegistry.Run("tp.portals"));
            Widgets.SectionEnd();

            Widgets.SectionBegin("Stage", run: true);
            Widgets.Button("Skip stage", () => NetUtil.Do(PoppyOp.SkipStage));
            Widgets.PickerButton("Go to scene", "Scenes", RunModule.SceneRows);
            Widgets.SectionEnd();
        }
    }
}
