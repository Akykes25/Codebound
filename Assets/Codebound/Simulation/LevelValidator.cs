using System.Collections.Generic;
using System.Linq;
using Codebound.Language;

namespace Codebound.Simulation
{
    /// <summary>Whether a run completed its level, and what is still missing if it did not.</summary>
    public sealed class LevelVerdict
    {
        internal LevelVerdict(IReadOnlyList<string> pendingObjectives, IReadOnlyList<string> missingConcepts)
        {
            PendingObjectives = pendingObjectives;
            MissingConcepts = missingConcepts;
        }

        public bool Completed => PendingObjectives.Count == 0 && MissingConcepts.Count == 0;

        /// <summary>Explanations of the parts of the goal the world does not meet.</summary>
        public IReadOnlyList<string> PendingObjectives { get; }

        /// <summary>Explanations of the concepts the level asks for and the code does not use.</summary>
        public IReadOnlyList<string> MissingConcepts { get; }
    }

    /// <summary>Checks a finished run against the goal of its level.</summary>
    public static class LevelValidator
    {
        public static LevelVerdict Evaluate(LevelRun run, LevelLayout layout, LevelGoal goal)
        {
            if (!run.Compiled)
            {
                return Failed("El código tiene errores. Corregilos y ejecutá de nuevo.");
            }

            if (run.StepLimitExceeded)
            {
                return Failed("El programa hizo demasiados pasos y se detuvo antes de terminar.");
            }

            var pending = PendingObjectives(run.FinalState, layout, goal);
            var missing = MissingConcepts(run.Concepts, goal.RequiredConcepts, worldDone: pending.Count == 0);
            return new LevelVerdict(pending, missing);
        }

        private static LevelVerdict Failed(string reason)
        {
            return new LevelVerdict(new[] { reason }, new string[0]);
        }

        private static List<string> PendingObjectives(WorldState state, LevelLayout layout, LevelGoal goal)
        {
            var pending = new List<string>();

            if (goal.TerminalActive && !state.TerminalActive)
            {
                pending.Add("La terminal sigue apagada.");
            }

            if (goal.AllModulesActive)
            {
                var inactive = layout.Modules.Count(module => !state.ActiveModules.Contains(module));
                if (inactive > 0)
                {
                    pending.Add(inactive == 1 ? "Falta activar 1 módulo." : $"Faltan activar {inactive} módulos.");
                }
            }

            if (goal.CoreCharge.HasValue && state.CoreCharge != goal.CoreCharge.Value)
            {
                pending.Add($"El núcleo tiene carga {state.CoreCharge} y necesita {goal.CoreCharge.Value}.");
            }

            if (goal.PowerDoorOpen && !state.PowerDoorOpen)
            {
                pending.Add("La puerta de energía sigue cerrada.");
            }

            if (goal.AllGatesOpen)
            {
                var closed = layout.Gates.Count(gate => !state.OpenGates.Contains(gate));
                if (closed > 0)
                {
                    pending.Add(closed == 1 ? "Queda 1 puerta cerrada." : $"Quedan {closed} puertas cerradas.");
                }
            }

            if (goal.FinalPosition.HasValue && !state.Position.Equals(goal.FinalPosition.Value))
            {
                pending.Add($"El robot tiene que terminar en {goal.FinalPosition.Value}.");
            }

            return pending;
        }

        private static List<string> MissingConcepts(ProgramConcept used, ProgramConcept required, bool worldDone)
        {
            // When the world is already solved, say so: the player got there and only the concept is missing.
            var opening = worldDone ? "Llegaste, pero este nivel te pide " : "Este nivel te pide ";
            var missing = new List<string>();

            if (Lacks(used, required, ProgramConcept.ForLoop))
            {
                missing.Add(opening + "repetir acciones con un for.");
            }

            if (Lacks(used, required, ProgramConcept.IfElse))
            {
                missing.Add(opening + "decidir con un if y su else.");
            }

            if (Lacks(used, required, ProgramConcept.IntVariableArgument))
            {
                missing.Add(opening + "guardar el valor en una variable int y pasarla al método.");
            }

            return missing;
        }

        private static bool Lacks(ProgramConcept used, ProgramConcept required, ProgramConcept concept)
        {
            return (required & concept) != 0 && (used & concept) == 0;
        }
    }
}
