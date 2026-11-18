using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace PoppyMenu
{
    // Item and equipment lists. Quality variants (Quality mod) of one item share a row that opens a sublist.
    internal static class ItemPicker
    {
        internal static readonly string[] TierFilters = { "All", "White", "Green", "Red", "Boss", "Lunar", "Void", "Other" };

        private struct Entry
        {
            internal int Index;
            internal string Name, Quality, Tier; // Tier: "[Tier1]" after the name, or null
            internal Color Color;
            internal int Tag, Count;             // Count: owned stacks, 0 when not shown
        }

        /* ─────── Public lists ──────────────────────────────────────── */

        // Every item; pick gets the chosen variant.
        internal static void Open(string title, Action<ItemIndex> pick)
        {
            Action reopen = null;
            reopen = () => ListPicker.Open(title, Groups(AllItems(), i => pick((ItemIndex)i), reopen, null), TierFilters);
            reopen();
        }

        // Items in your inventory with their counts. The list follows the inventory while it is open.
        internal static void OpenOwned(string title, Action<ItemIndex> pick)
        {
            Action reopen = null;
            reopen = () => ListPicker.OpenLive(title, () => Groups(OwnedItems(), i => pick((ItemIndex)i), reopen, OwnedItems), TierFilters);
            reopen();
        }

        internal static void OpenEquipment(string title, Action<EquipmentIndex> pick)
        {
            Action reopen = null;
            reopen = () => ListPicker.Open(title, Groups(AllEquipment(), i => pick((EquipmentIndex)i), reopen, null));
            reopen();
        }

        /* ─────── Sources ───────────────────────────────────────────── */

        private static List<Entry> AllItems()
        {
            var list = new List<Entry>(Catalogs.Items.Count);
            foreach (Catalogs.ItemEntry e in Catalogs.Items) list.Add(FromItem(e, 0));
            return list;
        }

        // Permanent stacks only: that is what RemoveItem takes away. Hidden items are bookkeeping, not loot.
        private static List<Entry> OwnedItems()
        {
            var list = new List<Entry>();
            Inventory inv = PlayerContext.Master != null ? PlayerContext.Master.inventory : null;
            if (inv == null) return list;
            foreach (Catalogs.ItemEntry e in Catalogs.Items)
            {
                int n = inv.GetItemCountPermanent(e.Index);
                if (n <= 0) continue;
                ItemDef def = ItemCatalog.GetItemDef(e.Index);
                if (def == null || def.hidden) continue;
                list.Add(FromItem(e, n));
            }
            return list;
        }

        private static Entry FromItem(Catalogs.ItemEntry e, int count) => new Entry
        {
            Index = (int)e.Index, Name = e.Name, Quality = e.Quality, Tier = e.Tier.ToString(),
            Color = e.Color, Tag = TierTag(e.Tier), Count = count
        };

        private static List<Entry> AllEquipment()
        {
            var list = new List<Entry>(Catalogs.Equipment.Count);
            foreach (Catalogs.EquipEntry e in Catalogs.Equipment)
                list.Add(new Entry { Index = (int)e.Index, Name = e.Name, Quality = e.Quality, Color = e.Color });
            return list;
        }

        private static int TierTag(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Tier1: return 1;
                case ItemTier.Tier2: return 2;
                case ItemTier.Tier3: return 3;
                case ItemTier.Boss: return 4;
                case ItemTier.Lunar: return 5;
                case ItemTier.VoidTier1:
                case ItemTier.VoidTier2:
                case ItemTier.VoidTier3:
                case ItemTier.VoidBoss: return 6;
                default: return 7;
            }
        }

        // Base first, then the Quality mod's tiers in order.
        private static int QualityRank(string q)
        {
            if (string.IsNullOrEmpty(q)) return 0;
            switch (q.ToLowerInvariant())
            {
                case "uncommon": return 1;
                case "rare": return 2;
                case "epic": return 3;
                case "legendary": return 4;
                default: return 5;
            }
        }

        /* ─────── Rows ──────────────────────────────────────────────── */

        // live: source to rebuild the sublist from (owned items), or null for a fixed list.
        private static List<ListPicker.Row> Groups(List<Entry> entries, Action<int> pick, Action reopen, Func<List<Entry>> live)
        {
            var byName = new Dictionary<string, List<Entry>>();
            var order = new List<string>();
            foreach (Entry e in entries)
            {
                if (!byName.TryGetValue(e.Name, out var list)) { list = new List<Entry>(); byName[e.Name] = list; order.Add(e.Name); }
                list.Add(e);
            }

            var rows = new List<ListPicker.Row>(order.Count);
            foreach (string name in order)
            {
                List<Entry> variants = byName[name];
                Entry first = variants[0];
                if (variants.Count == 1)
                {
                    string label = name + (first.Tier != null ? "   [" + first.Tier + "]" : "") + (first.Count > 0 ? "   x" + first.Count : "");
                    int idx = first.Index;
                    rows.Add(new ListPicker.Row(label, first.Color, () => pick(idx), first.Tag));
                    continue;
                }

                int total = 0;
                foreach (Entry v in variants) total += v.Count;
                string n = name;
                string text = name + (total > 0 ? "   x" + total : "") + "   (" + variants.Count + " qualities)";
                Action open = live != null
                    ? (Action)(() => ListPicker.OpenLive(n, () => Variants(n, Named(live(), n), pick, reopen)))
                    : () => ListPicker.Open(n, Variants(n, variants, pick, reopen));
                rows.Add(new ListPicker.Row(text, first.Color, open, first.Tag));
            }
            return rows;
        }

        private static List<Entry> Named(List<Entry> entries, string name)
        {
            var list = new List<Entry>();
            foreach (Entry e in entries) if (e.Name == name) list.Add(e);
            return list;
        }

        private static List<ListPicker.Row> Variants(string name, List<Entry> variants, Action<int> pick, Action reopen)
        {
            var sorted = new List<Entry>(variants);
            sorted.Sort((a, b) => QualityRank(a.Quality).CompareTo(QualityRank(b.Quality)));
            var rows = new List<ListPicker.Row>(sorted.Count + 1);
            rows.Add(new ListPicker.Row("< Back", Color.gray, reopen));
            foreach (Entry e in sorted)
            {
                string q = !string.IsNullOrEmpty(e.Quality) ? e.Quality : e.Tier ?? "Normal";
                int idx = e.Index;
                rows.Add(new ListPicker.Row(name + "  -  " + q + (e.Count > 0 ? "   x" + e.Count : ""), e.Color, () => pick(idx)));
            }
            return rows;
        }
    }
}
