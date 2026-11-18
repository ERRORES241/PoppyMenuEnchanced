using System;

namespace PoppyMenu
{
    internal class PoppyAction
    {
        internal readonly string Id;
        internal readonly string Category;
        internal readonly string Name;
        internal readonly Action Invoke;
        // Label of the menu row this action belongs to; its right-click menu offers the keybind.
        internal readonly string Row;

        internal PoppyAction(string id, string category, string name, Action invoke, string row = null)
        {
            Id = id; Category = category; Name = name; Invoke = invoke; Row = row ?? name;
        }
    }
}
