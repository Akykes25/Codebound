using System.Collections.Generic;
using System.Linq;

namespace Codebound.Levels
{
    /// <summary>
    /// The on-screen catalog of the methods a level allows: how each one is written and what it does.
    /// It only helps to write the code; it never writes it for the player (GDD, "Pantalla de juego").
    /// </summary>
    public static class MethodCatalogText
    {
        public const string Title = "Métodos disponibles";

        public static string Format(IEnumerable<ActionEntry> entries)
        {
            return string.Join("\n\n", entries.Select(entry => $"{entry.Usage}\n{entry.Description}"));
        }
    }
}
