using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace PoppyMenu
{
    internal static class StepBuilder
    {
        private static int _amount = 1;
        private static float _duration;
        private static bool _ally;

        internal static void Draw(Action<CustomStep> onAdd)
        {
            _amount = Widgets.IntStepper("Count / amount", _amount, 1, 1, 1000000000);
            _duration = Widgets.Slider("Buff duration", _duration, 0f, 120f, "0 means permanent.", "0 s");
            _ally = Widgets.Toggle("Spawn as ally (your team)", _ally);

            Widgets.ButtonGrid(
                ("Item", () => ItemPicker.Open("Pick an item", idx => Done(onAdd, Steps.Item((int)idx, _amount)))),
                ("Buff", () => ListPicker.Open("Pick a buff", BuffRows(onAdd))),
                ("Equipment", () => ListPicker.Open("Pick equipment", EquipRows(onAdd))));
            Widgets.ButtonGrid(
                ("Spawn", () => ListPicker.Open("Pick something to spawn", SpawnRows(onAdd))),
                ("Feature", () => ListPicker.Open("Pick a feature", FeatureRows(onAdd))),
                ("Macro", () => ListPicker.Open("Pick a macro", MacroRows(onAdd))));
            Widgets.ButtonGrid(
                ("Money +" + _amount, () => Done(onAdd, Steps.Money(_amount))),
                ("XP +" + _amount, () => Done(onAdd, Steps.Xp(_amount))),
                ("Lunar +" + _amount, () => Done(onAdd, Steps.Lunar(_amount))),
                ("Heal full", () => Done(onAdd, Steps.Heal(0))));
        }

        private static void Done(Action<CustomStep> onAdd, CustomStep s) { onAdd(s); Notify.Push("Added " + s.Label); }

        private static List<ListPicker.Row> BuffRows(Action<CustomStep> onAdd)
        {
            var rows = new List<ListPicker.Row>(Catalogs.Buffs.Count);
            foreach (var entry in Catalogs.Buffs)
            {
                var e = entry;
                rows.Add(new ListPicker.Row(e.Name, e.IsDebuff ? Color.red : Color.cyan, () => Done(onAdd, Steps.Buff((int)e.Index, _duration))));
            }
            return rows;
        }

        private static List<ListPicker.Row> EquipRows(Action<CustomStep> onAdd)
        {
            var rows = new List<ListPicker.Row>(Catalogs.Equipment.Count);
            foreach (var entry in Catalogs.Equipment)
            {
                var e = entry;
                rows.Add(new ListPicker.Row(e.Name, e.Color, () => Done(onAdd, Steps.Equipment((int)e.Index))));
            }
            return rows;
        }

        private static List<ListPicker.Row> SpawnRows(Action<CustomStep> onAdd)
        {
            var rows = new List<ListPicker.Row>(Catalogs.SpawnCards.Count);
            foreach (var entry in Catalogs.SpawnCards)
            {
                var e = entry;
                Color c = e.IsInteractable ? Color.cyan : Color.red;
                // Card name, not display name: DLC variants share display names.
                string card = e.Card != null ? e.Card.name : e.Name;
                rows.Add(new ListPicker.Row(e.Name, c, () => Done(onAdd, Steps.Spawn(card, _amount, _ally ? (int)TeamIndex.Player : (int)TeamIndex.Monster, e.Name))));
            }
            return rows;
        }

        private static List<ListPicker.Row> FeatureRows(Action<CustomStep> onAdd)
        {
            var rows = new List<ListPicker.Row>(ActionRegistry.All.Count);
            foreach (PoppyAction a in ActionRegistry.All)
            {
                PoppyAction act = a;
                rows.Add(new ListPicker.Row(act.Category + ": " + act.Name, Color.white, () => Done(onAdd, Steps.Feature(act.Id))));
            }
            return rows;
        }

        private static List<ListPicker.Row> MacroRows(Action<CustomStep> onAdd)
        {
            var rows = new List<ListPicker.Row>(MacroStore.Macros.Count);
            foreach (Macro m in MacroStore.Macros)
            {
                Macro macro = m;
                rows.Add(new ListPicker.Row(macro.Name, Color.yellow, () => Done(onAdd, Steps.Macro(macro))));
            }
            return rows;
        }
    }
}
