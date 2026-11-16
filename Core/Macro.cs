using System.Collections.Generic;

namespace PoppyMenu
{
    internal class Macro
    {
        // Stable key for binds, Home shortcuts and macro steps, so renaming a macro keeps them working.
        public string Id;
        public string Name = "Macro";
        public List<CustomStep> Steps = new List<CustomStep>();
    }
}
