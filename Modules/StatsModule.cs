using System;
using HarmonyLib;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace PoppyMenu
{
    internal class StatsModule : PoppyModule
    {
        internal override string Name => "Stats";

        internal static bool DamageOn, AttackSpeedOn, MoveSpeedOn, ArmorOn, CritOn, MaxHealthOn;
        internal static float DamageMult = 1f, AttackSpeedMult = 1f, MoveSpeedMult = 1f, MaxHealthMult = 1f;
        internal static float ArmorBonus = 0f;
        internal static float CritBonus = 100f;
        // Host side: money and experience are team-wide in the game, regen is applied by the server.
        internal static bool MoneyOn, XpOn, RegenOn;
        internal static float MoneyMult = 1f, XpMult = 1f, RegenMult = 1f;

        // Crit chance is a percentage: anything above 100 changes nothing.
        internal const float CritMax = 100f;

        internal static float ArmorMult { get => ArmorBonus; set => ArmorBonus = value; }
        internal static float CritMult { get => CritBonus; set => CritBonus = value; }

        private static Harmony _harmony;

        private static bool AnyOn =>
            DamageOn || AttackSpeedOn || MoveSpeedOn || ArmorOn || CritOn || MaxHealthOn || MoneyOn || XpOn || RegenOn;

        internal static bool Active => AnyOn;

        internal static void DisableAll()
        {
            DamageOn = AttackSpeedOn = MoveSpeedOn = ArmorOn = CritOn = MaxHealthOn = false;
            MoneyOn = XpOn = RegenOn = false;
        }

        private static void EnsurePatched()
        {
            if (_harmony != null)
                return;
            _harmony = new Harmony("poppy.stats");
            var orig = AccessTools.Method(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats));
            var pre = AccessTools.Method(typeof(StatsModule), nameof(RecalcPrefix));
            var post = AccessTools.Method(typeof(StatsModule), nameof(RecalcPostfix));
            _harmony.Patch(orig, prefix: new HarmonyMethod(pre), postfix: new HarmonyMethod(post));

            // Kills, barrels and gold orbs pay through the team; the menu's Give buttons do not, so they stay exact.
            PatchPrefix(AccessTools.Method(typeof(TeamManager), nameof(TeamManager.GiveTeamMoney), new[] { typeof(TeamIndex), typeof(uint), typeof(bool) }), nameof(MoneyPrefix));
            PatchPrefix(AccessTools.Method(typeof(ExperienceManager), nameof(ExperienceManager.AwardExperience)), nameof(XpPrefix));
        }

        private static void PatchPrefix(System.Reflection.MethodInfo m, string prefix)
        {
            if (m == null) { Log.Warning("Stats: method for " + prefix + " not found, that multiplier will be inert."); return; }
            try { _harmony.Patch(m, prefix: new HarmonyMethod(typeof(StatsModule), prefix)); }
            catch (Exception e) { Log.Error("Stats patch " + prefix + ": " + e); }
        }

        private static void MoneyPrefix(TeamIndex teamIndex, ref uint money)
        {
            if (!MoneyOn || teamIndex != TeamIndex.Player) return;
            money = (uint)Math.Min(money * (double)MoneyMult, uint.MaxValue);
        }

        // Before the orbs fly, so they show the multiplied amount.
        private static void XpPrefix(CharacterBody body, ref ulong amount)
        {
            if (!XpOn || body == null || body.teamComponent == null || body.teamComponent.teamIndex != TeamIndex.Player) return;
            double v = amount * (double)XpMult;
            amount = v >= ulong.MaxValue ? ulong.MaxValue : (ulong)v;
        }

        private struct HealthState
        {
            internal bool Valid;
            internal float OldMax;
            internal float Health;
        }

        // RecalculateStats adjusts current health to the change between the old and the new max health.
        // With the multiplier, the old max is already multiplied and the new one is not yet, so the game
        // clamps health down on every recalculation. Remember the state here and redo that step in the postfix.
        private static void RecalcPrefix(CharacterBody __instance, out HealthState __state)
        {
            __state = default;
            if (!MaxHealthOn || __instance == null || __instance != PlayerContext.Body || __instance.healthComponent == null) return;
            __state = new HealthState { Valid = true, OldMax = __instance.maxHealth, Health = __instance.healthComponent.health };
        }

        private static void RecalcPostfix(CharacterBody __instance, HealthState __state)
        {
            if (__instance == null || __instance != PlayerContext.Body)
                return;
            if (DamageOn) __instance.damage *= DamageMult;
            if (AttackSpeedOn) __instance.attackSpeed *= AttackSpeedMult;
            if (MoveSpeedOn) __instance.moveSpeed *= MoveSpeedMult;
            if (ArmorOn) __instance.armor += ArmorBonus;
            if (CritOn) __instance.crit += Mathf.Min(CritBonus, CritMax);
            if (RegenOn && __instance.regen > 0f) __instance.regen *= RegenMult;
            if (MaxHealthOn) ApplyMaxHealth(__instance, __state);
        }

        private static void ApplyMaxHealth(CharacterBody body, HealthState state)
        {
            float baseMax = body.maxHealth;
            float newMax = baseMax * MaxHealthMult;
            float delta = newMax - baseMax;
            body.maxHealth = newMax;
            body.maxBarrier += delta;
            body.maxHull += delta;

            HealthComponent hc = body.healthComponent;
            if (!state.Valid || hc == null || !NetworkServer.active) return;

            // Undo the game's adjustment and apply it again against the multiplied max.
            hc.Networkhealth = state.Health;
            float change = newMax - state.OldMax;
            if (change > 0f) hc.Heal(change, default(ProcChainMask), false);
            else if (hc.health > newMax) hc.Networkhealth = Mathf.Max(hc.health + change, newMax);
        }

        private static float _prevDmg, _prevAtk, _prevMove, _prevArmor, _prevCrit, _prevHp, _prevRegen;
        private static bool _prevDmgOn, _prevAtkOn, _prevMoveOn, _prevArmorOn, _prevCritOn, _prevHpOn, _prevRegenOn;
        private static CharacterBody _prevBody;

        internal override void Tick()
        {
            EnsurePatched();
            if (!PlayerContext.HasBody) return;

            CharacterBody body = PlayerContext.Body;
            bool changed = body != _prevBody ||
                _prevDmgOn != DamageOn || _prevDmg != DamageMult ||
                _prevAtkOn != AttackSpeedOn || _prevAtk != AttackSpeedMult ||
                _prevMoveOn != MoveSpeedOn || _prevMove != MoveSpeedMult ||
                _prevArmorOn != ArmorOn || _prevArmor != ArmorBonus ||
                _prevCritOn != CritOn || _prevCrit != CritBonus ||
                _prevHpOn != MaxHealthOn || _prevHp != MaxHealthMult ||
                _prevRegenOn != RegenOn || _prevRegen != RegenMult;

            if (changed)
            {
                _prevBody = body;
                _prevDmgOn = DamageOn; _prevDmg = DamageMult;
                _prevAtkOn = AttackSpeedOn; _prevAtk = AttackSpeedMult;
                _prevMoveOn = MoveSpeedOn; _prevMove = MoveSpeedMult;
                _prevArmorOn = ArmorOn; _prevArmor = ArmorBonus;
                _prevCritOn = CritOn; _prevCrit = CritBonus;
                _prevHpOn = MaxHealthOn; _prevHp = MaxHealthMult;
                _prevRegenOn = RegenOn; _prevRegen = RegenMult;

                body.RecalculateStats();
            }
        }

        // Player / Stats
        internal static void DrawStats()
        {
            Widgets.SectionBegin("Modifiers");

            DamageOn = Widgets.Toggle("Damage multiplier", DamageOn);
            if (DamageOn) DamageMult = Widgets.Slider("Damage", DamageMult, 1f, 100f, format: "0.#x");

            AttackSpeedOn = Widgets.Toggle("Attack speed multiplier", AttackSpeedOn);
            if (AttackSpeedOn) AttackSpeedMult = Widgets.Slider("Attack speed", AttackSpeedMult, 1f, 100f, format: "0.#x");

            MoveSpeedOn = Widgets.Toggle("Move speed multiplier", MoveSpeedOn);
            if (MoveSpeedOn) MoveSpeedMult = Widgets.Slider("Move speed", MoveSpeedMult, 1f, 100f, format: "0.#x");

            ArmorOn = Widgets.Toggle("Bonus armor", ArmorOn);
            if (ArmorOn) ArmorBonus = Widgets.Slider("Armor", ArmorBonus, 0f, 1000f, format: "+0");

            CritOn = Widgets.Toggle("Bonus crit chance", CritOn);
            if (CritOn) CritBonus = Mathf.Min(Widgets.Slider("Crit chance", CritBonus, 0f, CritMax, format: "+0'%'"), CritMax);

            MaxHealthOn = Widgets.Toggle("Max health multiplier", MaxHealthOn);
            if (MaxHealthOn) MaxHealthMult = Widgets.Slider("Max health", MaxHealthMult, 1f, 100f, format: "0.#x");

            Widgets.HostOnly();
            RegenOn = Widgets.Toggle("Regen multiplier", RegenOn, "Health regeneration.");
            if (RegenOn) RegenMult = Widgets.Slider("Regen", RegenMult, 1f, 100f, format: "0.#x");
            Widgets.SectionEnd();

            Widgets.SectionBegin("Earnings");
            Widgets.HostOnly();
            MoneyOn = Widgets.Toggle("Money multiplier", MoneyOn, "Gold from kills, barrels and orbs, for the whole team.");
            if (MoneyOn) MoneyMult = Widgets.Slider("Money", MoneyMult, 1f, 100f, format: "0.#x");
            Widgets.HostOnly();
            XpOn = Widgets.Toggle("Experience multiplier", XpOn, "Experience from kills, for the whole team.");
            if (XpOn) XpMult = Widgets.Slider("Experience", XpMult, 1f, 100f, format: "0.#x");
            Widgets.SectionEnd();

            Widgets.SectionBegin("Current");
            if (PlayerContext.HasBody)
            {
                CharacterBody b = PlayerContext.Body;
                Widgets.Value("Max health", b.maxHealth.ToString("0"));
                Widgets.Value("Damage", b.damage.ToString("0.##"));
                Widgets.Value("Attack speed", b.attackSpeed.ToString("0.##"));
                Widgets.Value("Armor", b.armor.ToString("0.##"));
                Widgets.Value("Crit chance", b.crit.ToString("0.##") + "%");
                Widgets.Value("Regen", b.regen.ToString("0.##") + "/s");
                Widgets.Value("Move speed", b.moveSpeed.ToString("0.##"));
            }
            else
            {
                Widgets.Note("No character right now.");
            }
            Widgets.SectionEnd();
        }

        internal override void OnUnload()
        {
            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
                _harmony = null;
            }
        }
    }
}
