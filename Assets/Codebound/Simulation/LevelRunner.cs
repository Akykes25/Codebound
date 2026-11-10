using System;
using System.Collections.Generic;
using Codebound.Language;

namespace Codebound.Simulation
{
    /// <summary>One call of the program and what it did to the world.</summary>
    public sealed class RunStep
    {
        internal RunStep(TraceEntry call, CallOutcome outcome)
        {
            Line = call.Line;
            Column = call.Column;
            Method = call.Method;
            Result = call.Result;
            From = outcome.From;
            To = outcome.To;
            Message = outcome.Failure;
        }

        public int Line { get; }
        public int Column { get; }
        public string Method { get; }
        public object Result { get; }
        public GridPosition From { get; }
        public GridPosition To { get; }
        public string Message { get; }
        public bool Succeeded => Message == null;

        public override string ToString()
        {
            return $"{Line}:{Column} {Method} {From}->{To} result={Result} message={Message}";
        }
    }

    /// <summary>The full result of a run, ready to be shown step by step.</summary>
    public sealed class LevelRun
    {
        internal LevelRun(
            bool compiled,
            IReadOnlyList<CodeDiagnostic> diagnostics,
            IReadOnlyList<RunStep> steps,
            WorldState finalState,
            bool stepLimitExceeded,
            ProgramConcept concepts)
        {
            Compiled = compiled;
            Diagnostics = diagnostics;
            Steps = steps;
            FinalState = finalState;
            StepLimitExceeded = stepLimitExceeded;
            Concepts = concepts;
        }

        public bool Compiled { get; }
        public IReadOnlyList<CodeDiagnostic> Diagnostics { get; }
        public IReadOnlyList<RunStep> Steps { get; }
        public WorldState FinalState { get; }
        public bool StepLimitExceeded { get; }

        /// <summary>The concepts the program uses. None when it did not compile.</summary>
        public ProgramConcept Concepts { get; }
    }

    /// <summary>Runs a player's program from the start of a level, without animating anything.</summary>
    public static class LevelRunner
    {
        /// <param name="availableMethods">The game methods this level allows, or null for all of them.</param>
        public static LevelRun Run(string source, LevelLayout layout, IEnumerable<string> availableMethods = null)
        {
            var world = new GridWorld(layout);
            var compilation = ProgramCompiler.Compile(source, GameApi.Catalog, availableMethods);
            if (!compilation.Succeeded)
            {
                return new LevelRun(false, compilation.Diagnostics, Array.Empty<RunStep>(), world.State, false, ProgramConcept.None);
            }

            var execution = ProgramInterpreter.Run(compilation.Program, world);
            var steps = new RunStep[execution.Trace.Count];
            for (var i = 0; i < steps.Length; i++)
            {
                steps[i] = new RunStep(execution.Trace[i], world.Outcomes[i]);
            }

            return new LevelRun(
                true,
                compilation.Diagnostics,
                steps,
                world.State,
                execution.Status == ExecutionStatus.StepLimitExceeded,
                compilation.Program.Concepts);
        }
    }
}
