using RoR2;

namespace PoppyMenu
{
    // Saved as numbers in the macro and Home files: never remove or reorder. Become is retired (body change was removed).
    internal enum StepKind { Feature, Item, Equipment, Buff, Money, Xp, Lunar, Spawn, Become, Heal, Command, Macro }

    internal class CustomStep
    {
        public StepKind Kind;
        public string Id = "";
        // Internal def name (ItemDef/EquipmentDef/BuffDef.name). Indexes shift when the game or mods add content,
        // names do not. Index is kept only as a fallback for steps saved before names were stored.
        public string Name = "";
        public int Index = -1;
        public int Amount = 1;
        public float Duration;
        public int Team;
        public string Label = "";
    }

    internal static class Steps
    {
        internal static CustomStep Feature(string id)
        {
            PoppyAction a = ActionRegistry.Get(id);
            return new CustomStep { Kind = StepKind.Feature, Id = id, Label = a != null ? a.Name : id };
        }

        internal static CustomStep Item(int index, int count)
            => new CustomStep { Kind = StepKind.Item, Index = index, Name = ItemCatalog.GetItemDef((ItemIndex)index)?.name ?? "", Amount = count, Label = $"Give {ItemName(index)} x{count}" };

        internal static CustomStep Equipment(int index)
            => new CustomStep { Kind = StepKind.Equipment, Index = index, Name = EquipmentCatalog.GetEquipmentDef((EquipmentIndex)index)?.name ?? "", Label = "Equip " + EquipName(index) };

        internal static CustomStep Buff(int index, float duration)
            => new CustomStep { Kind = StepKind.Buff, Index = index, Name = BuffCatalog.GetBuffDef((BuffIndex)index)?.name ?? "", Duration = duration, Label = duration > 0f ? $"Buff {BuffName(index)} ({duration:0}s)" : "Buff " + BuffName(index) };

        internal static int ResolveItem(CustomStep s)
        {
            if (!string.IsNullOrEmpty(s.Name)) { ItemIndex i = ItemCatalog.FindItemIndex(s.Name); if (i != ItemIndex.None) return (int)i; }
            return s.Index;
        }

        internal static int ResolveEquipment(CustomStep s)
        {
            if (!string.IsNullOrEmpty(s.Name)) { EquipmentIndex i = EquipmentCatalog.FindEquipmentIndex(s.Name); if (i != EquipmentIndex.None) return (int)i; }
            return s.Index;
        }

        internal static int ResolveBuff(CustomStep s)
        {
            if (!string.IsNullOrEmpty(s.Name)) { BuffIndex i = BuffCatalog.FindBuffIndex(s.Name); if (i != BuffIndex.None) return (int)i; }
            return s.Index;
        }

        internal static CustomStep Money(int amount) => new CustomStep { Kind = StepKind.Money, Amount = amount, Label = "Money +" + amount };
        internal static CustomStep Xp(int amount) => new CustomStep { Kind = StepKind.Xp, Amount = amount, Label = "XP +" + amount };
        internal static CustomStep Lunar(int amount) => new CustomStep { Kind = StepKind.Lunar, Amount = amount, Label = "Lunar +" + amount };

        internal static CustomStep Spawn(string card, int count, int team, string display)
            => new CustomStep { Kind = StepKind.Spawn, Id = card, Amount = count, Team = team, Label = $"Spawn {display} x{count}" };

        internal static CustomStep Heal(int amount) => new CustomStep { Kind = StepKind.Heal, Amount = amount, Label = amount > 0 ? "Heal " + amount : "Heal to full" };
        internal static CustomStep Command(string cmd) => new CustomStep { Kind = StepKind.Command, Id = cmd, Label = "> " + cmd };
        internal static CustomStep Macro(Macro m) => new CustomStep { Kind = StepKind.Macro, Id = m.Id, Label = "Macro: " + m.Name };

        private static string ItemName(int idx) { foreach (var e in Catalogs.Items) if ((int)e.Index == idx) return e.Name; return "item"; }
        private static string BuffName(int idx) { foreach (var e in Catalogs.Buffs) if ((int)e.Index == idx) return e.Name; return "buff"; }
        private static string EquipName(int idx) { foreach (var e in Catalogs.Equipment) if ((int)e.Index == idx) return e.Name; return "equipment"; }
    }
}
