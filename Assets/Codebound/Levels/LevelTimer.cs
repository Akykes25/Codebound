using System;

namespace Codebound.Levels
{
    /// <summary>
    /// Measures how long the player takes to complete a level: it starts when the level starts and stops,
    /// keeping its final time, when the level is completed. The game has no pause, so it never stops in between.
    /// The time never grades the player: it is shown next to the result, not instead of it.
    /// </summary>
    public sealed class LevelTimer
    {
        private readonly Func<double> now;
        private double startedAt;
        private double finalTime;

        /// <param name="now">The current time in seconds, for example () => Time.realtimeSinceStartupAsDouble.</param>
        public LevelTimer(Func<double> now)
        {
            this.now = now ?? throw new ArgumentNullException(nameof(now));
        }

        public bool IsRunning { get; private set; }

        /// <summary>True after <see cref="Stop"/>: the time is final until <see cref="Restart"/>.</summary>
        public bool IsStopped { get; private set; }

        /// <summary>Seconds since the timer started, or the final time once it stopped.</summary>
        public double Elapsed => IsRunning ? now() - startedAt : finalTime;

        /// <summary>Starts counting from zero. It does nothing if the timer is already running or stopped.</summary>
        public void Start()
        {
            if (IsRunning || IsStopped)
            {
                return;
            }

            startedAt = now();
            IsRunning = true;
        }

        /// <summary>Freezes the final time, when the level is completed.</summary>
        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            finalTime = now() - startedAt;
            IsRunning = false;
            IsStopped = true;
        }

        /// <summary>Goes back to zero and starts counting again, for a new attempt at the level.</summary>
        public void Restart()
        {
            IsRunning = false;
            IsStopped = false;
            finalTime = 0;
            Start();
        }

        /// <summary>Shows seconds as mm:ss, for example 65 as 01:05.</summary>
        public static string Format(double seconds)
        {
            var whole = (int)Math.Floor(Math.Max(0, seconds));
            return $"{whole / 60:00}:{whole % 60:00}";
        }
    }
}
