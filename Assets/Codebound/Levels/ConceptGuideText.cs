using System.Collections.Generic;
using System.Linq;

namespace Codebound.Levels
{
    /// <summary>
    /// The text of the "Conceptos" tab: each concept with its explanation and a short example.
    /// The concepts introduced by the current level are marked so the player knows what is new.
    /// </summary>
    public static class ConceptGuideText
    {
        public const string Title = "Conceptos";
        public const string NewMark = " (nuevo en este nivel)";

        public static string Format(IEnumerable<ConceptEntry> entries, int levelNumber)
        {
            return string.Join("\n\n", entries.Select(entry =>
            {
                var title = entry.IntroducedIn == levelNumber ? entry.Title + NewMark : entry.Title;
                return $"{title}\n{entry.Explanation}\nEjemplo:\n{entry.Example}";
            }));
        }
    }
}
