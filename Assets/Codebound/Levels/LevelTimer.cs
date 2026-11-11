using System;

namespace Codebound.Levels
{
    /// <summary>
    /// Measures how long the player takes in a level. It can be paused and resumed; once stopped it keeps
    /// its final time until it is restarted. What pauses or restarts it is decided by the game, not here.
    /// The time never grades the player: it is shown next to the result, not instead of it.
    /// </summary>
    public sealed class LevelTimer
    {
        private readonly Func<double> now;
        private double accumulated;
        private double runningSince;

        /// <param name="now">The current time in seconds, for example () => Time.unscaledTimeAsDouble.</param>
        public LevelTimer(Func<double> now)
        {
            this.now = now ?? throw new ArgumentNullException(nameof(now));
        }

        public bool IsRunning { get; private set; }

        /// <summary>True after <see cref="Stop"/>: the time is final until <see cref="Restart"/>.</summary>
        public bool IsStopped { get; private set; }

        /// <summary>Seconds counted so far, without the time spent paused.</summary>
        public double Elapsed => accumulated + (IsRunning ? now() - runningSince : 0);

        /// <summary>Starts counting. It does nothing if the timer is already running or stopped.</summary>
        public void Start()
        {
            Resume();
        }

        public void Pause()
        {
            if (!IsRunning)
            {
                return;
            }

            accumulated = Elapsed;
            IsRunning = false;
        }

        public void Resume()
        {
            if (IsRunning || IsStopped)
            {
                return;
            }

            runningSince = now();
            IsRunning = true;
        }

        /// <summary>Freezes the final time, for example when the level is completed.</summary>
        public void Stop()
        {
            Pause();
            IsStopped = true;
        }

        /// <summary>Goes back to zero and starts counting again.</summary>
        public void Restart()
        {
            accumulated = 0;
            IsRunning = false;
            IsStopped = false;
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
