using UnityEngine;

namespace PoppyMenu
{
    internal class MacrosModule : PoppyModule
    {
        internal override string Name => "Macros";

        private static string _newName = "";
        private static int _expanded = -1;
        private static Macro _pendingDelete;

        internal override void DrawMenu()
        {
            Widgets.SectionBegin("Macros");
            Widgets.Note("A macro runs several custom actions at once. Add any items, buffs, currency, spawns, or features, then bind it to a key or pin it on Home.");
            if (Widgets.BeginRow())
            {
            _newName = Widgets.TextField(_newName, "New macro name");
            Widgets.RowSpace();
            if (Widgets.SmallButton("New macro", Widgets.Kind.Primary, 100))
            {
                string name = string.IsNullOrWhiteSpace(_newName) ? "Macro " + (MacroStore.Macros.Count + 1) : _newName.Trim();
                MacroStore.Macros.Add(new Macro { Id = MacroStore.NewId(), Name = name });
                MacroStore.Save();
                _newName = "";
                _expanded = MacroStore.Macros.Count - 1;
            }
            Widgets.EndRow();
            }
            Widgets.SectionEnd();

            if (MacroStore.Macros.Count == 0)
            {
                Widgets.EmptyState("No macros yet. Name one above and click New macro.");
                return;
            }

            for (int i = 0; i < MacroStore.Macros.Count; i++)
            {
                Macro m = MacroStore.Macros[i];
                Widgets.SectionBegin(null);

                if (Widgets.BeginRow())
                {
                    string nn = Widgets.TextField(m.Name);
                    if (nn != m.Name) { m.Name = nn; MacroStore.Save(); }
                    Widgets.RowSpace();
                    if (Widgets.SmallButton(_expanded == i ? "Close" : "Edit", Widgets.Kind.Normal, 70))
                        _expanded = _expanded == i ? -1 : i;
                    Widgets.EndRow();
                }

                KeyCode key = BindStore.KeyFor(MacroStore.BindKey(m));
                string info = m.Steps.Count + (m.Steps.Count == 1 ? " step" : " steps") +
                              (key != KeyCode.None ? ", bound to [" + key + "]" : "");
                Macro target = m;
                Widgets.Buttons(m.Name, Rebind.IsActive ? "Press a key or button to bind this macro. Esc cancels." : info,
                    ("Run now", () => MacroStore.Run(target.Id)),
                    ("Bind key", () => Rebind.Capture(kc => BindStore.Add(kc, MacroStore.BindKey(target)))));

                if (_expanded == i) DrawEditor(m);
                Widgets.SectionEnd();
            }

            if (_pendingDelete != null) { MacroStore.Macros.Remove(_pendingDelete); MacroStore.Save(); _pendingDelete = null; _expanded = -1; }
        }

        private static void DrawEditor(Macro m)
        {
            Widgets.Header("Steps (run top to bottom)");
            StepBuilder.Draw(s => { m.Steps.Add(s); MacroStore.Save(); });

            int removeAt = -1;
            for (int k = 0; k < m.Steps.Count; k++)
            {
                int index = k;
                Widgets.Buttons((k + 1) + ". " + m.Steps[k].Label, null, ("Remove", () => removeAt = index));
            }
            if (removeAt >= 0) { m.Steps.RemoveAt(removeAt); MacroStore.Save(); }
            if (m.Steps.Count == 0) Widgets.Note("No steps yet. Use the buttons above to add any item, buff, spawn, feature, and more.");

            Widgets.ConfirmButton("macro.delete." + m.Id, "Delete this macro", () => _pendingDelete = m);
        }
    }
}
