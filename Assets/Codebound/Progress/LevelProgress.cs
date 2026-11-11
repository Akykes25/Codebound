using System;
using UnityEngine;

namespace Codebound.Progress
{
    /// <summary>What the game remembers about one level.</summary>
    [Serializable]
    public sealed class LevelProgress
    {
        [SerializeField] private int levelNumber;
        [SerializeField] private bool completed;
        [SerializeField] private bool hasBestTime;
        [SerializeField] private float bestTimeSeconds;
        [Tooltip("True when the best time was set after opening the solution.")]
        [SerializeField] private bool bestTimeAssisted;
        [SerializeField] private string lastCode = string.Empty;

        // Used by JsonUtility when it reads the file.
        private LevelProgress()
        {
        }

        public LevelProgress(int levelNumber)
        {
            this.levelNumber = levelNumber;
        }

        public int LevelNumber => levelNumber;
        public bool Completed => completed;
        public bool HasBestTime => hasBestTime;
        public float BestTimeSeconds => bestTimeSeconds;
        public bool BestTimeAssisted => bestTimeAssisted;
        public string LastCode => lastCode ?? string.Empty;

        internal void RecordAttempt(string code)
        {
            lastCode = code ?? string.Empty;
        }

        internal void RecordCompletion(string code)
        {
            completed = true;
            lastCode = code ?? string.Empty;
        }

        internal void RecordCompletion(float timeSeconds, bool assisted, string code)
        {
            completed = true;
            lastCode = code ?? string.Empty;

            if (!hasBestTime || timeSeconds < bestTimeSeconds)
            {
                hasBestTime = true;
                bestTimeSeconds = timeSeconds;
                bestTimeAssisted = assisted;
            }
        }
    }
}
