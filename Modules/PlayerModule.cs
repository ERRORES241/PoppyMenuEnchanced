using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace PoppyMenu
{
    internal class PlayerModule : PoppyModule
    {
        internal override string Name => "General";

        internal static bool GodMode;
        internal static bool InfiniteSkills;

        private static int _heal = 500;
        private static int _hurt = 100;
        private static float _buffDuration;

        internal static void ToggleGod() => GodMode = !GodMode;

        private static bool _godApplied;
        private static CharacterBody _godBody;

        internal override void Tick()
        {
            if (!PlayerContext.InGame || !PlayerContext.HasBody) return;

            // Write only on change (or new body) so we never fight the game's own god command or other mods.
            if (GodMode != _godApplied || PlayerContext.Body != _godBody)
            {
                if (PlayerContext.Health != null) PlayerContext.Health.godMode = GodMode;
                if (PlayerContext.Master != null) PlayerContext.Master.godMode = GodMode;
                _godApplied = GodMode;
                _godBody = PlayerContext.Body;
            }

            if (InfiniteSkills && PlayerContext.Skills != null)
                PlayerContext.Skills.ApplyAmmoPack();
        }

        internal override void DrawMenu()
        {
            Widgets.SectionBegin("Survival");
            GodMode = Widgets.Toggle("God Mode", GodMode);
            Safety.Buddha = Widgets.Toggle("Semi-Godmode", Safety.Buddha, "Survive lethal hits at 1 HP.");
            InfiniteSkills = Widgets.Toggle("Skills No CD", InfiniteSkills);
            Widgets.SectionEnd();

            Widgets.SectionBegin("Health");
            Widgets.PrimaryButton("Heal to Full", () => NetUtil.Do(PoppyOp.HealFull));
            _heal = Widgets.StepperAction("Heal amount", _heal, 100, 1, 1000000000, "Heal", v => NetUtil.Do(PoppyOp.HealAmount, f1: v));
            _hurt = Widgets.StepperAction("Hurt amount", _hurt, 50, 1, 1000000000, "Hurt", v => NetUtil.Do(PoppyOp.HurtBody, f1: v));
            Widgets.Button("Respawn", () => NetUtil.Do(PoppyOp.Respawn));
            Widgets.SectionEnd();

            Widgets.SectionBegin("Economy");
            ModConfig.GiveMoneyAmount.Value = Widgets.StepperAction("Money", ModConfig.GiveMoneyAmount.Value, 1000, 0, 1000000000, "Give",
                v => NetUtil.Do(PoppyOp.GiveMoney, i1: v));
            ModConfig.GiveXpAmount.Value = Widgets.StepperAction("Experience", ModConfig.GiveXpAmount.Value, 100, 0, 1000000000, "Give",
                v => NetUtil.Do(PoppyOp.GiveXp, i1: v));
            ModConfig.GiveCoinsAmount.Value = Widgets.StepperAction("Lunar Coins", ModConfig.GiveCoinsAmount.Value, 5, 0, 100000, "Give",
                v => NetUtil.Do(PoppyOp.GiveLunar, i1: v));
            Widgets.SectionEnd();

            Widgets.SectionBegin("Buffs & DoTs");
            _buffDuration = Widgets.Slider("Buff duration", _buffDuration, 0f, 60f, "0 means permanent.", "0 s");
            Widgets.PickerButton("Give Buff", "Buffs", BuffRows);
            Widgets.PickerButton("Inflict DoT", "Damage over time", DotRows);
            Widgets.DangerButton("Remove All Buffs", () => NetUtil.Do(PoppyOp.RemoveAllBuffs));
            Widgets.SectionEnd();
        }

        private static List<ListPicker.Row> BuffRows()
        {
            var rows = new List<ListPicker.Row>(Catalogs.Buffs.Count);
            foreach (var entry in Catalogs.Buffs)
            {
                var e = entry;
                rows.Add(new ListPicker.Row(e.Name, e.IsDebuff ? Color.red : Color.cyan, () =>
                {
                    if (_buffDuration > 0.05f) NetUtil.Do(PoppyOp.GiveTimedBuff, i1: (int)e.Index, f1: _buffDuration);
                    else NetUtil.Do(PoppyOp.GiveBuff, (int)e.Index, 1);
                }));
            }
            return rows;
        }

        private static List<ListPicker.Row> DotRows()
        {
            var rows = new List<ListPicker.Row>();
            foreach (DotController.DotIndex dot in System.Enum.GetValues(typeof(DotController.DotIndex)))
            {
                if ((int)dot < 0 || dot >= DotController.DotIndex.Count) continue;
                DotController.DotIndex d = dot;
                rows.Add(new ListPicker.Row(d.ToString(), Color.red, () =>
                    NetUtil.Do(PoppyOp.InflictDot, i1: (int)d, f1: _buffDuration > 0.05f ? _buffDuration : 5f)));
            }
            return rows;
        }
    }
}
