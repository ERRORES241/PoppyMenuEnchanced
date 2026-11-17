using RoR2;
using UnityEngine;

namespace PoppyMenu
{
    internal class FunModule : PoppyModule
    {
        internal override string Name => "Fun";

        internal static bool RailgunnerInstaCharge;
        internal static bool RailgunnerNoUltCooldown;
        internal static bool RailgunnerUltSpam;

        internal override void Tick()
        {
            if (!PlayerContext.InGame || !PlayerContext.HasBody) return;

            CharacterBody body = PlayerContext.Body;
            if (body == null) return;

            TickRailgunner(body);
        }

        private static BodyIndex _railgunnerIndex = BodyIndex.None;

        private static bool IsBody(CharacterBody body, ref BodyIndex cached, string name)
        {
            if (cached == BodyIndex.None) cached = BodyCatalog.FindBodyIndex(name);
            return cached != BodyIndex.None && body.bodyIndex == cached;
        }

        private static void TickRailgunner(CharacterBody body)
        {
            if (!(RailgunnerInstaCharge || RailgunnerNoUltCooldown || RailgunnerUltSpam)) return;
            if (!IsBody(body, ref _railgunnerIndex, "RailgunnerBody")) return;

            // Ult Spam implies the other two without overwriting the user's toggles.
            bool instaCharge = RailgunnerInstaCharge || RailgunnerUltSpam;
            bool noUltCooldown = RailgunnerNoUltCooldown || RailgunnerUltSpam;

            {
                // 1. Reset Special skill cooldown ONLY (Do NOT touch primary, secondary, utility).
                // Only for No Ult Cooldown / Ult Spam: Instant Ult Charge alone must not refill the ult.
                if (noUltCooldown && body.skillLocator != null && body.skillLocator.special != null)
                {
                    body.skillLocator.special.stock = body.skillLocator.special.maxStock;
                    body.skillLocator.special.rechargeStopwatch = 999f;
                }

                EntityStateMachine[] machines = body.GetComponents<EntityStateMachine>();
                if (machines != null)
                {
                    foreach (var machine in machines)
                    {
                        if (machine == null || machine.state == null) continue;

                        // Instant Charge logic: set fixedAge to 999f on both weapon and backpack so the UI bar renders 100% full instantly and transitions naturally
                        if (instaCharge)
                        {
                            if (machine.state is EntityStates.Railgunner.Weapon.BaseChargeSnipe chargeWeapon)
                            {
                                chargeWeapon.fixedAge = 999f;
                            }
                            if (machine.state is EntityStates.Railgunner.Backpack.BaseCharging chargeBackpack)
                            {
                                chargeBackpack.fixedAge = 999f;
                            }
                        }

                        // Remove Overheat/Expired lockout after Special fires
                        if (noUltCooldown)
                        {
                            string stateName = machine.state.GetType().Name;
                            if (stateName.Contains("Expired") || stateName.Contains("Offline") || stateName.Contains("Reboot") || stateName.Contains("Overheat"))
                            {
                                machine.SetNextStateToMain();
                            }
                        }

                        // Ult Spam on M1 (Primary / LMB): every M1 press fires a Supercharge Ult shot
                        if (RailgunnerUltSpam && body.inputBank != null && body.inputBank.skill1.down)
                        {
                            if (machine.customName == "Weapon" || machine.state is EntityStates.Railgunner.Weapon.FirePistol || machine.state is EntityStates.Railgunner.Weapon.FireSnipeLight || machine.state is EntityStates.Railgunner.Weapon.FireSnipeHeavy)
                            {
                                if (!(machine.state is EntityStates.Railgunner.Weapon.FireSnipeSuper))
                                {
                                    machine.SetNextState(new EntityStates.Railgunner.Weapon.FireSnipeSuper());
                                }
                            }
                        }
                    }
                }
            }
        }

        internal override void DrawMenu()
        {
            Widgets.SectionBegin("Railgunner");
            RailgunnerInstaCharge = Widgets.Toggle("Instant ult charge", RailgunnerInstaCharge,
                "The special is ready to fire with no charge-up.");
            RailgunnerNoUltCooldown = Widgets.Toggle("No ult cooldown", RailgunnerNoUltCooldown,
                "Refills the special and skips the 5 s overheat. Other skills keep their cooldowns.");
            RailgunnerUltSpam = Widgets.Toggle("Every shot is an ult", RailgunnerUltSpam,
                "Primary fire shoots Supercharge rounds, scoped or not.");
            Widgets.SectionEnd();
        }
    }
}
