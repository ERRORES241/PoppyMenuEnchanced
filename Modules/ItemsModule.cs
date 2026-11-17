using System.Collections.Generic;
using RoR2;

namespace PoppyMenu
{
    internal class ItemsModule : PoppyModule
    {
        internal override string Name => "Items";

        internal static bool NoEquipmentCooldown;
        internal static int GiveCount = 1;
        internal static bool SaleStarCheat;
        internal static int SaleStarDropCount = 5;

        internal override void DrawMenu()
        {
            Widgets.SectionBegin("Give");
            GiveCount = Widgets.IntStepper("Count", GiveCount, 1, 1, 1000, "How many of each item to give.");

            Widgets.Button("Give item", () =>
                ItemPicker.Open("Items", idx => NetUtil.Do(PoppyOp.GiveItem, (int)idx, GiveCount)));

            Widgets.PickerButton("Give equipment", "Equipment", EquipRows);
            Widgets.PickerButton("Set key item", "Key items", KeyItemRows);
            Widgets.Button($"Give all items (x{GiveCount})", () => NetUtil.Do(PoppyOp.GiveAllItems, i2: GiveCount));
            Widgets.SectionEnd();

            Widgets.SectionBegin("Inventory");
            Widgets.Button("Stack", "Turns each tier into one item, like a Shrine of Order.", () => NetUtil.Do(PoppyOp.StackInventory));
            Widgets.Button("Reroll", "Swaps every item for a random one of the same tier.", () => NetUtil.Do(PoppyOp.RollItems));
            Widgets.Button("Undo", "Reverts the last give all, clear, stack, reroll or key item change.", () => NetUtil.Do(PoppyOp.UndoInventory));
            NoEquipmentCooldown = Widgets.Toggle("No equipment cooldown", NoEquipmentCooldown);
            Widgets.ConfirmButton("items.clearinv", "Clear inventory", () => NetUtil.Do(PoppyOp.ClearInventory));
            Widgets.SectionEnd();

            Widgets.SectionBegin("Sale Star");
            SaleStarCheat = Widgets.Toggle("Fixed drop count", SaleStarCheat, "Chests opened with Sale Star drop the number of items set below.");
            if (SaleStarCheat)
            {
                SaleStarDropCount = Widgets.IntStepper("Items per chest", SaleStarDropCount, 1, 2, 5);
            }
            Widgets.SectionEnd();
        }

        private static List<ListPicker.Row> EquipRows()
        {
            var rows = new List<ListPicker.Row>(Catalogs.Equipment.Count);
            foreach (var entry in Catalogs.Equipment)
            {
                var e = entry;
                rows.Add(new ListPicker.Row(e.Name, e.Color, () => NetUtil.Do(PoppyOp.GiveEquipment, (int)e.Index)));
            }
            return rows;
        }

        private static readonly UnityEngine.Color KeyItemColor = new UnityEngine.Color(0.85f, 0.65f, 1f);

        // DLC4 key item slot (e.g. Cursed Book). One slot per player; "None" clears it.
        private static List<ListPicker.Row> KeyItemRows()
        {
            var rows = new List<ListPicker.Row>
            {
                new ListPicker.Row("None", UnityEngine.Color.gray, () => NetUtil.Do(PoppyOp.SetKeyItem, i1: (int)KeyItemIndex.None))
            };
            foreach (KeyItemDef def in KeyItemCatalog.allKeyItemDefs)
            {
                if (def == null) continue;
                KeyItemIndex idx = def.keyItemIndex;
                string name = Language.GetString(def.nameToken);
                if (string.IsNullOrEmpty(name) || name == def.nameToken) name = def.name;
                rows.Add(new ListPicker.Row(name, KeyItemColor, () => NetUtil.Do(PoppyOp.SetKeyItem, i1: (int)idx)));
            }
            return rows;
        }

        internal static void Init()
        {
            On.RoR2.PurchaseInteraction.OnInteractionBegin += PurchaseInteraction_OnInteractionBegin;
        }

        private static void PurchaseInteraction_OnInteractionBegin(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            var body = activator ? activator.GetComponent<CharacterBody>() : null;
            int prevConsumed = 0;
            if (body && body.inventory)
            {
                prevConsumed = body.inventory.GetItemCount(DLC2Content.Items.LowerPricedChestsConsumed);
            }

            orig(self, activator);

            if (SaleStarCheat && body && body.inventory)
            {
                int postConsumed = body.inventory.GetItemCount(DLC2Content.Items.LowerPricedChestsConsumed);
                if (postConsumed > prevConsumed)
                {
                    var chest = self.GetComponent<ChestBehavior>();
                    if (chest) chest.dropCount = SaleStarDropCount;

                    var roulette = self.GetComponent<RouletteChestController>();
                    if (roulette) roulette.dropCount = SaleStarDropCount;
                }
            }
        }

        internal override void Tick()
        {
            if (!NoEquipmentCooldown || !NetUtil.IsServer || !PlayerContext.HasBody)
                return;

            EquipmentSlot slot = PlayerContext.Body.equipmentSlot;
            if (slot == null || slot.equipmentIndex == EquipmentIndex.None)
                return;

            int max = slot.maxStock > 0 ? slot.maxStock : 1;
            if (slot.stock < max)
                slot.stock = max;
        }
    }
}
