using System.Collections.Generic;
using UnityEngine;

namespace PoppyMenu
{
    internal class KeybindsModule : PoppyModule
    {
        internal override string Name => "Keybinds";

        internal override void DrawMenu()
        {
            Widgets.SectionBegin("Add a keybind");
            Widgets.PickerButton("Bind a feature", "Pick a feature", ActionRows, Rebind.IsActive
                ? "Now press a key or button. Esc cancels."
                : "Pick any feature, then press the key or mouse button you want. Bindings fire while the menu is closed.");
            Widgets.SectionEnd();

            Widgets.SectionBegin("Your keybinds");
            if (BindStore.Binds.Count == 0)
                Widgets.Note("Nothing bound yet.");

            for (int i = BindStore.Binds.Count - 1; i >= 0; i--)
            {
                Bind b = BindStore.Binds[i];
                PoppyAction act = ActionRegistry.Get(b.ActionId);
                string label = act != null ? act.Category + ": " + act.Name : b.ActionId;
                if (act == null && b.ActionId != null && b.ActionId.StartsWith(MacroStore.BindPrefix))
                {
                    Macro bound = MacroStore.Get(b.ActionId.Substring(MacroStore.BindPrefix.Length));
                    label = bound != null ? "Macro: " + bound.Name : "Macro (deleted)";
                }

                Bind captured = b;
                Widgets.Buttons(label, null,
                    (b.Key == KeyCode.None ? "Set key" : b.Key.ToString(), () => Rebind.Capture(kc => { captured.Key = kc; BindStore.Save(); })),
                    ("Remove", () => BindStore.Remove(captured)));
            }

            if (BindStore.Binds.Count > 0)
                Widgets.ConfirmButton("keybinds.clear", "Clear all keybinds", BindStore.Clear);
            Widgets.SectionEnd();
        }

        private static List<ListPicker.Row> ActionRows()
        {
            var rows = new List<ListPicker.Row>(ActionRegistry.All.Count + MacroStore.Macros.Count);
            foreach (PoppyAction a in ActionRegistry.All)
            {
                PoppyAction act = a;
                rows.Add(new ListPicker.Row(act.Category + ": " + act.Name, Color.white,
                    () => Rebind.Capture(kc => BindStore.Add(kc, act.Id))));
            }
            foreach (Macro m in MacroStore.Macros)
            {
                Macro macro = m;
                rows.Add(new ListPicker.Row("Macro: " + macro.Name, Color.yellow,
                    () => Rebind.Capture(kc => BindStore.Add(kc, MacroStore.BindKey(macro)))));
            }
            return rows;
        }
    }
}
