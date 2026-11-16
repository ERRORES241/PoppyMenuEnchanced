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

        internal override void DrawMenu()
        {
            if (!PlayerContext.InGame) { Widgets.Label("Start a run first."); return; }

            Widgets.SectionBegin("Teleporter");
            TeleporterInteraction tp = TeleporterInteraction.instance;
            Widgets.Label(tp != null ? "Charge: " + Mathf.FloorToInt(tp.chargeFraction * 100f) + "%" : "No teleporter on this stage.");
            InstaCharge = Widgets.Toggle("Instant Charge (hold)", InstaCharge);
            Widgets.Hint("Also fills Void Fields cells, Void Locus signals and moon pillars.");
            Widgets.Button("Skip Stage", () => NetUtil.Do(PoppyOp.SkipStage));
            Widgets.Button("Add Mountain Shrine Stack", () => NetUtil.Do(PoppyOp.AddMountainShrine));
            Widgets.SectionEnd();
        }
    }
}
