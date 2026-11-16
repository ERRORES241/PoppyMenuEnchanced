using HarmonyLib;
using RoR2;
using UnityEngine;

namespace PoppyMenu
{
    internal class WorldModule : PoppyModule
    {
        internal override string Name => "Time";

        internal static bool FreezeMatch;
        internal static bool FreezeTimer;
        internal static float TimeScale = 1f;
        internal static bool GuaranteedShrineChance;

        internal static bool InfiniteShrineChance;

        private static bool _controllingTime;
        private static bool _timerFrozen;
        private static Harmony _harmony;

        private static readonly AccessTools.FieldRef<ShrineChanceBehavior, int> _successfulPurchaseCountRef =
            AccessTools.FieldRefAccess<ShrineChanceBehavior, int>("successfulPurchaseCount");

        internal static void Init()
        {
            try
            {
                _harmony = new Harmony("poppy.world");
                var addStack = AccessTools.Method(typeof(ShrineChanceBehavior), nameof(ShrineChanceBehavior.AddShrineStack));
                if (addStack != null)
                {
                    _harmony.Patch(addStack,
                        prefix: new HarmonyMethod(typeof(WorldModule), nameof(AddShrineStack_Prefix)),
                        postfix: new HarmonyMethod(typeof(WorldModule), nameof(AddShrineStack_Postfix)));
                }
            }
            catch (System.Exception e)
            {
                Log.Error("WorldModule Harmony patch failed: " + e);
            }
        }

        private struct ShrineOdds
        {
            internal bool Overridden;
            internal float FailureChance;
            internal float FailureWeight;
        }

        // The game rolls the drop itself (Chance Doll jackpot, effects, chat, events); we only remove the failure odds.
        private static void AddShrineStack_Prefix(ShrineChanceBehavior __instance, out ShrineOdds __state)
        {
            __state = default;
            if (__instance == null || !UnityEngine.Networking.NetworkServer.active) return;

            // Infinite uses: never let the success count reach maxPurchaseCount.
            if (InfiniteShrineChance) _successfulPurchaseCountRef(__instance) = 0;

            if (!GuaranteedShrineChance) return;
            __state = new ShrineOdds { Overridden = true, FailureChance = __instance.failureChance, FailureWeight = __instance.failureWeight };
            __instance.failureChance = -1f; // drop table path: roll > failureChance always succeeds
            __instance.failureWeight = 0f;  // legacy weighted path
        }

        private static void AddShrineStack_Postfix(ShrineChanceBehavior __instance, ShrineOdds __state)
        {
            if (!__state.Overridden || __instance == null) return;
            __instance.failureChance = __state.FailureChance;
            __instance.failureWeight = __state.FailureWeight;
        }

        internal override void Tick()
        {
            if (NetUtil.IsServer && Run.instance != null)
            {
                if (FreezeTimer) Run.instance.SetRunStopwatchPaused(true);
                else if (_timerFrozen) Run.instance.SetRunStopwatchPaused(false);
            }
            _timerFrozen = FreezeTimer;

            ApplyTimeScale();
        }

        private static void ApplyTimeScale()
        {
            bool want = FreezeMatch || Mathf.Abs(TimeScale - 1f) > 0.001f;
            if (want)
            {
                if (PauseManager.isPaused) return; // do not override the pause menu
                Time.timeScale = FreezeMatch ? 0f : TimeScale;
                _controllingTime = true;
            }
            else if (_controllingTime)
            {
                if (!PauseManager.isPaused) Time.timeScale = 1f;
                _controllingTime = false;
            }
        }

        internal static void RestoreTime()
        {
            FreezeMatch = false;
            if (_controllingTime)
            {
                if (!PauseManager.isPaused) Time.timeScale = 1f;
                _controllingTime = false;
            }
            if (_timerFrozen)
            {
                if (NetUtil.IsServer && Run.instance != null) Run.instance.SetRunStopwatchPaused(false);
                _timerFrozen = false;
            }
            FreezeTimer = false;
        }

        internal override void OnUnload() => RestoreTime();

        internal override void DrawMenu()
        {
            Widgets.SectionBegin("Time");
            FreezeMatch = Widgets.Toggle("Freeze Match (everything stops)", FreezeMatch);
            Widgets.Hint("Stops everything, including you. Toggle off to move. Host or solo.");

            FreezeTimer = Widgets.Toggle("Freeze Timer (difficulty)", FreezeTimer);
            Widgets.Hint("Stops the run clock so difficulty quits climbing. Enemies still move.");

            GUILayout.Space(4);
            TimeScale = Widgets.Slider("Time Scale", TimeScale, 0.1f, 3f);
            GUILayout.BeginHorizontal();
            Widgets.Button("Slow-mo (0.25x)", () => TimeScale = 0.25f);
            Widgets.Button("Normal (1x)", () => TimeScale = 1f);
            Widgets.Button("Fast (2x)", () => TimeScale = 2f);
            GUILayout.EndHorizontal();
            Widgets.SectionEnd();

            Widgets.SectionBegin("Global Actions");
            Widgets.DangerButton("Kill All Enemies", () => NetUtil.Do(PoppyOp.KillAllEnemies));
            Widgets.SectionEnd();

            Widgets.SectionBegin("Shrines");
            GuaranteedShrineChance = Widgets.Toggle("100% Shrine of Chance Win Rate", GuaranteedShrineChance);
            Widgets.Hint("Guarantees an item drop on every offer at Shrine of Chance (no fails). Host or solo.");
            InfiniteShrineChance = Widgets.Toggle("Infinite Shrine of Chance Uses", InfiniteShrineChance);
            Widgets.Hint("Allows using Shrine of Chance infinitely without it shutting down.");
            Widgets.SectionEnd();

            Widgets.SectionBegin("Safety");
            Safety.NoEnemies = Widgets.Toggle("No Enemies (kill on spawn)", Safety.NoEnemies);
            Safety.LockExp = Widgets.Toggle("Lock Experience", Safety.LockExp);
            Safety.PreventProfileWriting = Widgets.Toggle("Prevent Profile Saving", Safety.PreventProfileWriting);
            Widgets.Hint("Host or solo. Prevent Profile Saving keeps test runs out of your save file.");
            Widgets.SectionEnd();
        }
    }
}
