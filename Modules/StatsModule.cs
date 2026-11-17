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

        internal static float ArmorMult { get => ArmorBonus; set => ArmorBonus = value; }
        internal static float CritMult { get => CritBonus; set => CritBonus = value; }

        private static Harmony _harmony;

        private static bool AnyOn =>
            DamageOn || AttackSpeedOn || MoveSpeedOn || ArmorOn || CritOn || MaxHealthOn;

        internal static bool Active => AnyOn;

        internal static void DisableAll()
        {
            DamageOn = AttackSpeedOn = MoveSpeedOn = ArmorOn = CritOn = MaxHealthOn = false;
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
            if (CritOn) __instance.crit += CritBonus;
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

        private static float _prevDmg, _prevAtk, _prevMove, _prevArmor, _prevCrit, _prevHp;
        private static bool _prevDmgOn, _prevAtkOn, _prevMoveOn, _prevArmorOn, _prevCritOn, _prevHpOn;
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
                _prevHpOn != MaxHealthOn || _prevHp != MaxHealthMult;

            if (changed)
            {
                _prevBody = body;
                _prevDmgOn = DamageOn; _prevDmg = DamageMult;
                _prevAtkOn = AttackSpeedOn; _prevAtk = AttackSpeedMult;
                _prevMoveOn = MoveSpeedOn; _prevMove = MoveSpeedMult;
                _prevArmorOn = ArmorOn; _prevArmor = ArmorBonus;
                _prevCritOn = CritOn; _prevCrit = CritBonus;
                _prevHpOn = MaxHealthOn; _prevHp = MaxHealthMult;

                body.RecalculateStats();
            }
        }

        internal override void DrawMenu()
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
            if (CritOn) CritBonus = Widgets.Slider("Crit chance", CritBonus, 0f, 1000f, format: "+0'%'");

            MaxHealthOn = Widgets.Toggle("Max health multiplier", MaxHealthOn);
            if (MaxHealthOn) MaxHealthMult = Widgets.Slider("Max health", MaxHealthMult, 1f, 100f, format: "0.#x");
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
