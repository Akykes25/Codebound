using System.Collections.Generic;
using System.Linq;
using Codebound.Levels;
using Codebound.Progress;

namespace Codebound.Gameplay
{
    /// <summary>One line of the list of times: a level and what the player achieved in it.</summary>
    public sealed class LevelTimeRow
    {
        internal LevelTimeRow(int number, string title, string status)
        {
            Number = number;
            Title = title;
            Status = status;
        }

        public int Number { get; }
        public string Title { get; }

        /// <summary>The best time (with "Asistido" when it applies), "Completado" or "Sin completar".</summary>
        public string Status { get; }

        public override string ToString()
        {
            return $"Nivel {Number} · {Title}: {Status}";
        }
    }

    /// <summary>Builds the list with the time of each level from the saved progress.</summary>
    public static class LevelTimes
    {
        public const string NotCompleted = "Sin completar";
        public const string CompletedWithoutTimer = "Completado";
        public const string AssistedMark = "Asistido";

        public static IReadOnlyList<LevelTimeRow> Rows(IEnumerable<IPlayableLevel> levels, SaveData progress)
        {
            return levels
                .OrderBy(level => level.Number)
                .Select(level => new LevelTimeRow(level.Number, level.Title, Status(progress.Find(level.Number))))
                .ToList();
        }

        private static string Status(LevelProgress level)
        {
            if (level == null || !level.Completed)
            {
                return NotCompleted;
            }

            if (!level.HasBestTime)
            {
                return CompletedWithoutTimer;
            }

            var time = LevelTimer.Format(level.BestTimeSeconds);
            return level.BestTimeAssisted ? $"{time} ({AssistedMark})" : time;
        }
    }
}
