using Itinero.Attributes;

namespace Itinero.Intermodal.Test.Functional.Temp
{
    /// <summary>
    /// Contains extension methods for shortcuts.
    /// </summary>
    public static class ShortcutExtensions
    {
        /// <summary>
        /// Holds the shortcut key constant.
        /// </summary>
        public static string SHORTCUT_KEY = "shortcut";

        /// <summary>
        /// Returns true if the given attribute collection represents a shortcut and returns the name.
        /// </summary>
        public static bool IsShortcut(this IAttributeCollection attributes, out string name)
        {
            return attributes.TryGetValue(ShortcutExtensions.SHORTCUT_KEY, out name);
        }
    }
}