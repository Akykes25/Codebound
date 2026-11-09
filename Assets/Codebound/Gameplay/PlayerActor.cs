using System;
using System.Collections;
using System.Collections.Generic;
using Codebound.Simulation;
using UnityEngine;

namespace Codebound.Gameplay
{
    /// <summary>Shows a finished run on the character, one step at a time.</summary>
    public sealed class PlayerActor : MonoBehaviour
    {
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float secondsPerStep = 0.35f;

        /// <summary>Raised when a step starts, so the interface can follow the active line.</summary>
        public event Action<RunStep> StepStarted;

        public float SecondsPerStep
        {
            get => secondsPerStep;
            set => secondsPerStep = value;
        }

        public void PlaceAt(GridPosition cell)
        {
            transform.localPosition = ToLocalPosition(cell);
        }

        public IEnumerator Play(IReadOnlyList<RunStep> steps)
        {
            foreach (var step in steps)
            {
                StepStarted?.Invoke(step);
                yield return Animate(step);
            }
        }

        private IEnumerator Animate(RunStep step)
        {
            var start = ToLocalPosition(step.From);
            var end = ToLocalPosition(step.To);
            for (var elapsed = 0f; elapsed < secondsPerStep; elapsed += Time.deltaTime)
            {
                transform.localPosition = Vector3.Lerp(start, end, elapsed / secondsPerStep);
                yield return null;
            }

            transform.localPosition = end;
        }

        // Grid Y grows downwards and Unity's Y grows upwards.
        private Vector3 ToLocalPosition(GridPosition cell)
        {
            return new Vector3(cell.X * cellSize, -cell.Y * cellSize, 0f);
        }
    }
}
