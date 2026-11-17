using UnityEngine;

namespace PoppyMenu
{
    internal class AimbotModule : PoppyModule
    {
        private static readonly string[] Priorities =
            { "Closest to crosshair", "Closest", "Lowest HP", "Highest HP" };

        internal override void Tick() => Aim.Tick();
        internal override void DrawOverlay() => Aim.DrawOverlay();

        internal override string Name => "Aimbot";

        internal override void DrawMenu()
        {
            Widgets.SectionBegin("Aimbot");
            Aim.Enabled = Widgets.Toggle("Aimbot", Aim.Enabled, ModConfig.SilentAimKey.Value,
                "Shots go to the target. The camera stays where you point it.");
            Widgets.KeybindRow("Aim hold key", ModConfig.SilentAimKey, ModConfig.SilentAimKey.Value == KeyCode.None
                ? "Not set, so the aimbot is always on. Click, then press a key or mouse button."
                : $"Works only while [{ModConfig.SilentAimKey.Value}] is held. X clears the key.");
            Widgets.SectionEnd();

            Widgets.SectionBegin("Targeting");
            Aim.Sorting = Widgets.Dropdown("Priority", Aim.Sorting, Priorities);

            Aim.PrioritizeBosses = Widgets.Toggle("Bosses first", Aim.PrioritizeBosses);
            Aim.TargetWeakPoints = Widgets.Toggle("Railgunner weak points", Aim.TargetWeakPoints,
                "While scoped, aims at the red weak points. Hits there always crit.");
            Aim.Sticky = Widgets.Toggle("Sticky target", Aim.Sticky, "Stays on one target until it dies or leaves range.");
            Aim.RequireLoS = Widgets.Toggle("Line of sight only", Aim.RequireLoS, "Ignores targets behind walls.");

            Aim.MaxRange = Widgets.Slider("Range", Aim.MaxRange, 20f, 600f, format: "0 m");
            Aim.UseFov = Widgets.Toggle("FOV limit", Aim.UseFov, "Only targets inside the circle around the crosshair.");
            if (Aim.UseFov)
                Aim.Fov = Widgets.Slider("FOV size", Aim.Fov, 1f, 180f, format: "0°");
            Widgets.SectionEnd();

            Widgets.SectionBegin("Projectiles");
            Aim.MagicBullet = Widgets.Toggle("Magic bullet", Aim.MagicBullet,
                "Your projectiles turn toward the target and your shots go through walls. Host or solo.");
            Widgets.SectionEnd();

            Widgets.SectionBegin("Weapon");
            Aim.NoSpread = Widgets.Toggle("No spread", Aim.NoSpread, "Bullets and projectiles go exactly where you aim.");
            Aim.NoRecoil = Widgets.Toggle("No recoil", Aim.NoRecoil, "The camera doesn't kick when you shoot.");
            Widgets.SectionEnd();

            Widgets.SectionBegin("On screen");
            Aim.Highlight = Widgets.Toggle("Mark target", Aim.Highlight, "Shows which enemy the aimbot is locked on.");
            Aim.ShowFovCircle = Widgets.Toggle("Show FOV circle", Aim.ShowFovCircle);
            Widgets.SectionEnd();
        }
    }
}
