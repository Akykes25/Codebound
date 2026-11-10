using System.Collections.Generic;
using System.Linq;

namespace Codebound.Progress
{
    /// <summary>The player's progress across every level.</summary>
    public sealed class SaveData
    {
        /// <summary>Bump it when the file format changes, and teach <see cref="JsonProgressStore"/> to upgrade older files.</summary>
        public const int CurrentVersion = 1;

        private readonly List<LevelProgress> levels;

        public SaveData()
            : this(new List<LevelProgress>())
        {
        }

        internal SaveData(IEnumerable<LevelProgress> levels)
        {
            this.levels = levels.ToList();
        }

        public int Version => CurrentVersion;
        public IReadOnlyList<LevelProgress> Levels => levels;

        /// <summary>The progress of a level, or null if the player never ran code in it.</summary>
        public LevelProgress Find(int levelNumber)
        {
            return levels.FirstOrDefault(level => level.LevelNumber == levelNumber);
        }

        /// <summary>Remembers the code of a run, whether it won or not.</summary>
        public void RecordAttempt(int levelNumber, string code)
        {
            GetOrAdd(levelNumber).RecordAttempt(code);
        }

        /// <summary>Marks the level as completed and keeps the time if it beats the best one.</summary>
        public void RecordCompletion(int levelNumber, float timeSeconds, bool assisted, string code)
        {
            GetOrAdd(levelNumber).RecordCompletion(timeSeconds, assisted, code);
        }

        private LevelProgress GetOrAdd(int levelNumber)
        {
            var level = Find(levelNumber);
            if (level == null)
            {
                level = new LevelProgress(levelNumber);
                levels.Add(level);
            }

            return level;
        }
    }
}
