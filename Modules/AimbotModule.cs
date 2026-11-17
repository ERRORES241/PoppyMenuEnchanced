using UnityEngine;

namespace PoppyMenu
{
    internal class AimbotModule : PoppyModule
    {
        private static readonly string[] Priorities =
            { "Closest to Crosshair", "Closest Distance", "Lowest HP", "Highest HP" };

        internal override void Tick() => Aim.Tick();
        internal override void DrawOverlay() => Aim.DrawOverlay();

        internal override string Name => "Aimbot";

        internal override void DrawMenu()
        {
            Widgets.SectionBegin("Aimbot");
            Aim.Enabled = Widgets.Toggle("Enable Aimbot", Aim.Enabled, ModConfig.SilentAimKey.Value,
                "Silent aim. Your shots go to the target, your camera never moves.");
            Widgets.KeybindRow("Aim hold key", ModConfig.SilentAimKey, ModConfig.SilentAimKey.Value == KeyCode.None
                ? "No hold key, so aim is on whenever it's enabled. Click and press a key or mouse button."
                : $"Only active while you hold [{ModConfig.SilentAimKey.Value}]. X clears it.");
            Widgets.SectionEnd();

            Widgets.SectionBegin("Targeting");
            Aim.Sorting = Widgets.Dropdown("Priority", Aim.Sorting, Priorities);

            Aim.PrioritizeBosses = Widgets.Toggle("Prioritize Bosses", Aim.PrioritizeBosses);
            Aim.TargetWeakPoints = Widgets.Toggle("Railgunner: Target Weak Points", Aim.TargetWeakPoints,
                "When scoped as Railgunner, aims at red weak points for guaranteed crits.");
            Aim.Sticky = Widgets.Toggle("Sticky Target", Aim.Sticky, "Keeps one target until it dies or leaves range.");
            Aim.RequireLoS = Widgets.Toggle("Require Line of Sight", Aim.RequireLoS);

            Aim.MaxRange = Widgets.Slider("Max Range", Aim.MaxRange, 20f, 600f, format: "0 m");
            Aim.UseFov = Widgets.Toggle("Limit to FOV cone", Aim.UseFov);
            if (Aim.UseFov)
                Aim.Fov = Widgets.Slider("FOV", Aim.Fov, 1f, 180f, format: "0°");
            Widgets.SectionEnd();

            Widgets.SectionBegin("Projectiles");
            Aim.MagicBullet = Widgets.Toggle("Magic Bullet (shoot through walls)", Aim.MagicBullet,
                "Your projectiles curve onto the locked target and your shots pass through walls. Enemies' don't. Host or solo.");
            Widgets.SectionEnd();

            Widgets.SectionBegin("Weapon Handling");
            Aim.NoSpread = Widgets.Toggle("No Spread (Perfect Accuracy)", Aim.NoSpread, "Removes bullet and projectile spread on all weapons.");
            Aim.NoRecoil = Widgets.Toggle("No Recoil (Fixed Camera)", Aim.NoRecoil, "Prevents camera kickback when firing skills.");
            Widgets.SectionEnd();

            Widgets.SectionBegin("Visuals");
            Aim.Highlight = Widgets.Toggle("Highlight Target", Aim.Highlight);
            Aim.ShowFovCircle = Widgets.Toggle("Show FOV Circle", Aim.ShowFovCircle);
            Widgets.SectionEnd();
        }
    }
}
