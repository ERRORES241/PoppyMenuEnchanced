using UnityEngine;

namespace PoppyMenu
{
    internal static class StepRunner
    {
        internal static void Run(CustomStep s)
        {
            if (s == null) return;
            switch (s.Kind)
            {
                case StepKind.Feature: ActionRegistry.RunId(s.Id); break;
                case StepKind.Item: NetUtil.Do(PoppyOp.GiveItem, Steps.ResolveItem(s), Mathf.Max(1, s.Amount)); break;
                case StepKind.Equipment: NetUtil.Do(PoppyOp.GiveEquipment, Steps.ResolveEquipment(s)); break;
                case StepKind.Buff:
                    if (s.Duration > 0f) NetUtil.Do(PoppyOp.GiveTimedBuff, i1: Steps.ResolveBuff(s), f1: s.Duration);
                    else NetUtil.Do(PoppyOp.GiveBuff, Steps.ResolveBuff(s), Mathf.Max(1, s.Amount));
                    break;
                case StepKind.Money: NetUtil.Do(PoppyOp.GiveMoney, i1: s.Amount); break;
                case StepKind.Xp: NetUtil.Do(PoppyOp.GiveXp, i1: s.Amount); break;
                case StepKind.Lunar: NetUtil.Do(PoppyOp.GiveLunar, i1: s.Amount); break;
                case StepKind.Spawn:
                {
                    Vector3 p = AtPlayer();
                    NetUtil.Do(PoppyOp.Spawn, i1: Mathf.Max(1, s.Amount), i2: s.Team, f1: p.x, f2: p.y, f3: p.z, s1: s.Id);
                    break;
                }
                case StepKind.Heal:
                    if (s.Amount > 0) NetUtil.Do(PoppyOp.HealAmount, f1: s.Amount);
                    else NetUtil.Do(PoppyOp.HealFull);
                    break;
                case StepKind.Command:
                    if (RoR2.Console.instance != null && !string.IsNullOrEmpty(s.Id))
                    {
                        var sender = RoR2.NetworkUser.readOnlyLocalPlayersList.Count > 0 ? RoR2.NetworkUser.readOnlyLocalPlayersList[0] : null;
                        RoR2.Console.instance.SubmitCmd(sender, s.Id);
                    }
                    break;
                case StepKind.Macro: MacroStore.Run(s.Id); break;
            }
        }

        private static Vector3 AtPlayer() => PlayerContext.HasBody ? PlayerContext.Body.footPosition : Vector3.zero;
    }
}
