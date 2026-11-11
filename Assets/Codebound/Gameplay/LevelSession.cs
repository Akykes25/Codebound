using System;
using Codebound.Levels;
using Codebound.Progress;
using Codebound.Simulation;

namespace Codebound.Gameplay
{
    /// <summary>The time it took to complete a level, as the player sees it at the end.</summary>
    public sealed class LevelResult
    {
        internal LevelResult(double? timeSeconds, bool assisted, bool isNewBest, double? bestTimeSeconds)
        {
            TimeSeconds = timeSeconds;
            Assisted = assisted;
            IsNewBest = isNewBest;
            BestTimeSeconds = bestTimeSeconds;
        }

        /// <summary>The final time, or null when the level has no timer.</summary>
        public double? TimeSeconds { get; }

        /// <summary>True when the solution was opened before completing the level.</summary>
        public bool Assisted { get; }

        public bool IsNewBest { get; }
        public double? BestTimeSeconds { get; }

        public string TimeText => TimeSeconds.HasValue ? LevelTimer.Format(TimeSeconds.Value) : null;
        public string BestTimeText => BestTimeSeconds.HasValue ? LevelTimer.Format(BestTimeSeconds.Value) : null;

        /// <summary>The final time, with the "Asistido" mark next to it when the solution was opened.</summary>
        public string TimeLine
        {
            get
            {
                if (TimeText == null)
                {
                    return "¡Nivel completado!";
                }

                var assisted = Assisted ? $" ({LevelTimes.AssistedMark})" : string.Empty;
                return $"Tiempo: {TimeText}{assisted}";
            }
        }

        public string BestTimeLine
        {
            get
            {
                if (BestTimeText == null)
                {
                    return string.Empty;
                }

                return IsNewBest ? "¡Nuevo mejor tiempo!" : $"Mejor tiempo: {BestTimeText}";
            }
        }
    }

    /// <summary>Everything one run of the player's code produced.</summary>
    public sealed class LevelAttempt
    {
        internal LevelAttempt(LevelRun run, LevelVerdict verdict, string advice, LevelDebrief debrief, LevelResult result)
        {
            Run = run;
            Verdict = verdict;
            Advice = advice;
            Debrief = debrief;
            Result = result;
        }

        public LevelRun Run { get; }
        public LevelVerdict Verdict { get; }

        /// <summary>The level's advice when the code ran but did not reach the goal; otherwise null.</summary>
        public string Advice { get; }

        /// <summary>Why the solution works, when the level was completed; otherwise null.</summary>
        public LevelDebrief Debrief { get; }

        /// <summary>The final time, only for the run that completed the level; otherwise null.</summary>
        public LevelResult Result { get; }
    }

    /// <summary>
    /// One play of a level, from the moment it opens until it is completed. The timer starts when the session
    /// is created and stops when the code completes the level; the time is saved in the player's progress.
    /// </summary>
    public sealed class LevelSession
    {
        private readonly SaveData progress;

        public LevelSession(IPlayableLevel level, SaveData progress, Func<double> clock)
        {
            Level = level ?? throw new ArgumentNullException(nameof(level));
            this.progress = progress ?? throw new ArgumentNullException(nameof(progress));
            Help = level.CreateHelp();

            if (level.IsTimed)
            {
                Timer = new LevelTimer(clock);
                Timer.Start();
            }
        }

        public IPlayableLevel Level { get; }
        public LevelHelp Help { get; }

        /// <summary>The level's timer, or null when the level has no timer.</summary>
        public LevelTimer Timer { get; }

        public bool IsCompleted { get; private set; }

        /// <summary>The time so far as mm:ss, or null when the level has no timer.</summary>
        public string ElapsedText => Timer == null ? null : LevelTimer.Format(Timer.Elapsed);

        public LevelAttempt Submit(string source)
        {
            var run = Level.Run(source);
            var verdict = Level.Evaluate(run);
            progress.RecordAttempt(Level.Number, source);

            LevelResult result = null;
            if (verdict.Completed && !IsCompleted)
            {
                IsCompleted = true;
                result = RecordCompletion(source);
            }

            return new LevelAttempt(run, verdict, Level.Advise(source, run, verdict), Level.Debrief(source, verdict), result);
        }

        private LevelResult RecordCompletion(string source)
        {
            if (Timer == null)
            {
                progress.RecordCompletion(Level.Number, source);
                return new LevelResult(null, Help.IsAssisted, false, null);
            }

            Timer.Stop();
            var time = (float)Timer.Elapsed;
            var before = progress.Find(Level.Number);
            var isNewBest = before == null || !before.HasBestTime || time < before.BestTimeSeconds;

            progress.RecordCompletion(Level.Number, time, Help.IsAssisted, source);
            return new LevelResult(time, Help.IsAssisted, isNewBest, progress.Find(Level.Number).BestTimeSeconds);
        }
    }
}
