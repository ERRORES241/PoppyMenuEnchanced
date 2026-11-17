using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace PoppyMenu
{
    internal class SpawnModule : PoppyModule
    {
        internal override string Name => "Spawn";

        private static int _count = 1;
        private static bool _ally;

        private static Vector3 ComputePos()
        {
            if (Physics.Raycast(PlayerContext.AimRay(), out RaycastHit hit, 1000f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                return hit.point;
            if (PlayerContext.HasBody)
                return PlayerContext.Body.footPosition + PlayerContext.Body.transform.forward * 5f;
            return Vector3.zero;
        }

        private static List<ListPicker.Row> Rows(bool interactables)
        {
            var rows = new List<ListPicker.Row>();
            foreach (var entry in Catalogs.SpawnCards)
            {
                if (entry.IsInteractable != interactables) continue;
                // Look up by card name: DLC variants share display names (snowy/sandy shrines, void chests).
                string card = entry.Card != null ? entry.Card.name : entry.Name;
                rows.Add(new ListPicker.Row(entry.Name, interactables ? Color.cyan : Color.red, () =>
                {
                    Vector3 pos = ComputePos();
                    NetUtil.Do(PoppyOp.Spawn, i1: _count, i2: _ally ? (int)TeamIndex.Player : (int)TeamIndex.Monster, s1: card, f1: pos.x, f2: pos.y, f3: pos.z);
                }));
            }
            return rows;
        }

        internal override void DrawMenu()
        {
            if (Catalogs.SpawnCards.Count == 0)
            {
                Widgets.EmptyState("Nothing to spawn yet. Load into a stage.");
                return;
            }

            Widgets.SectionBegin("Spawn");
            _count = Widgets.IntStepper("Count", _count, 1, 1, 50);
            _ally = Widgets.Toggle("On your team", _ally);
            Widgets.PickerButton("Monster", "Monsters", () => Rows(false), "Appears where you aim, or in front of you.");
            Widgets.PickerButton("Interactable", "Interactables", () => Rows(true), "Appears where you aim, or in front of you.");
            Widgets.SectionEnd();
        }
    }
}
