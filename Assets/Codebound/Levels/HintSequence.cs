using System.Collections.Generic;
using System.Linq;

namespace Codebound.Levels
{
    /// <summary>
    /// Reveals a level's hints one step at a time, in order.
    /// Revealing hints never counts as assistance; only opening the solution does.
    /// </summary>
    public sealed class HintSequence
    {
        private readonly List<string> hints;
        private int revealedCount;

        public HintSequence(IEnumerable<string> hints)
        {
            this.hints = hints.ToList();
        }

        public bool HasNext => revealedCount < hints.Count;

        public IReadOnlyList<string> Revealed => hints.GetRange(0, revealedCount);

        /// <summary>Returns the next hint, or null when every hint was already shown.</summary>
        public string RevealNext()
        {
            if (!HasNext)
            {
                return null;
            }

            return hints[revealedCount++];
        }
    }
}
