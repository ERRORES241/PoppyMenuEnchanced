using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace PoppyMenu
{
    internal class RunModule : PoppyModule
    {
        internal override string Name => "Run";

        private static int _stages;
        private static int _minutes;
        private static int _teamLevel = 1;

        internal override void DrawMenu()
        {
            if (!PlayerContext.InGame || Run.instance == null) { Widgets.EmptyState("Start a run first."); return; }

            Run run = Run.instance;

            Widgets.SectionBegin("Run");
            Widgets.Value("Stage", (run.stageClearCount + 1).ToString());
            Widgets.Value("Run time", FormatClock(run.GetRunStopwatch()));
            Widgets.Value("Difficulty", run.difficultyCoefficient.ToString("0.0"));

            _stages = Widgets.StepperAction("Stages cleared", _stages, 1, 0, 200, "Set", v => NetUtil.Do(PoppyOp.SetStagesCleared, i1: v));
            _minutes = Widgets.StepperAction("Run time (minutes)", _minutes, 1, 0, 600, "Set", v => NetUtil.Do(PoppyOp.SetRunTime, f1: v * 60f),
                "Difficulty grows with run time. To stop the clock, use Freeze timer on the World page.");
            Widgets.SectionEnd();

            Widgets.SectionBegin("Level");
            _teamLevel = Widgets.StepperAction("Team level", _teamLevel, 1, 1, 99999, "Set",
                v => NetUtil.Do(PoppyOp.SetTeamLevel, i1: (int)TeamIndex.Player, i2: v));
            Widgets.SectionEnd();

            Widgets.SectionBegin("Stage");
            Widgets.PickerButton("Go to scene", "Scenes", SceneRows);
            Widgets.SectionEnd();

            DrawArtifacts();
        }

        private static List<ListPicker.Row> SceneRows()
        {
            var rows = new List<ListPicker.Row>();
            var defs = SceneCatalog.allSceneDefs;
            foreach (SceneDef def in defs)
            {
                if (def == null || !IsPlayable(def)) continue;
                SceneDef d = def;
                string name = Language.GetString(d.nameToken);
                if (string.IsNullOrEmpty(name) || name == d.nameToken) name = d.cachedName;
                rows.Add(new ListPicker.Row(name, Color.white, () => NetUtil.Do(PoppyOp.ChangeScene, i1: (int)d.sceneDefIndex)));
            }
            return rows;
        }

        // Menus, cutscenes and junk scenes (title, lobby, logbook, intro/outro) break the run when advanced to.
        private static bool IsPlayable(SceneDef def)
        {
            if (def.isOfflineScene) return false;
            switch (def.sceneType)
            {
                case SceneType.Stage:
                case SceneType.UntimedStage:
                case SceneType.Intermission:
                case SceneType.TimedIntermission:
                    return true;
                default:
                    return false;
            }
        }

        private static void DrawArtifacts()
        {
            Widgets.SectionBegin("Artifacts");
            ArtifactDef[] defs = ArtifactCatalog.artifactDefs;
            RunArtifactManager mgr = RunArtifactManager.instance;
            if (defs == null || mgr == null) { Widgets.Note("Not available right now."); Widgets.SectionEnd(); return; }

            foreach (ArtifactDef def in defs)
            {
                if (def == null) continue;
                bool on = mgr.IsArtifactEnabled(def);
                bool now = Widgets.Toggle(ArtifactName(def), on);
                if (now != on) NetUtil.Do(PoppyOp.SetArtifact, i1: (int)def.artifactIndex, b1: now);
            }
            Widgets.SectionEnd();
        }

        private static string ArtifactName(ArtifactDef def)
        {
            string name = Language.GetString(def.nameToken);
            if (string.IsNullOrEmpty(name) || name == def.nameToken) name = def.cachedName;
            return name;
        }

        private static string FormatClock(float seconds)
        {
            int s = Mathf.Max(0, (int)seconds);
            return (s / 60) + ":" + (s % 60).ToString("00");
        }
    }
}
