using System;

namespace PoppyMenu
{
    // A sub-page of a tab: draws sections taken from one or more modules.
    // A page without a draw callback is the Featured page, which MenuRoot builds from every other page.
    internal sealed class Page : PoppyModule
    {
        private readonly string _name;
        private readonly Action[] _sections;

        internal Page(string name, params Action[] sections)
        {
            _name = name;
            _sections = sections;
        }

        internal override string Name => _name;

        internal bool IsFeatured => _sections.Length == 0;

        internal override void DrawMenu()
        {
            foreach (Action s in _sections) s();
        }
    }
}
