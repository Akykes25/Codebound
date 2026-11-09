using Codebound.Language;
using Codebound.Simulation;

namespace Codebound.Tests.EditMode
{
    /// <summary>The five levels of the slice, with the GDD programs that solve them.</summary>
    internal static class TestLevels
    {
        public static readonly LevelLayout Level1 = new LevelLayout
        {
            Width = 6,
            Height = 3,
            Start = new GridPosition(1, 1),
            Terminal = new GridPosition(4, 1),
        };

        public static readonly LevelLayout Level2 = new LevelLayout
        {
            Width = 6,
            Height = 4,
            Start = new GridPosition(1, 2),
            Walls = new[] { new GridPosition(3, 2) },
            Terminal = new GridPosition(4, 1),
        };

        public static readonly LevelLayout Level3 = new LevelLayout
        {
            Width = 8,
            Height = 3,
            Start = new GridPosition(1, 1),
            Modules = new[]
            {
                new GridPosition(2, 1),
                new GridPosition(3, 1),
                new GridPosition(4, 1),
                new GridPosition(5, 1),
            },
            Terminal = new GridPosition(6, 1),
        };

        public static readonly LevelLayout Level4 = new LevelLayout
        {
            Width = 5,
            Height = 3,
            Start = new GridPosition(2, 1),
            Core = new GridPosition(2, 0),
            PowerDoor = new GridPosition(3, 1),
            RequiredCharge = 3,
        };

        public static readonly LevelLayout Level5 = new LevelLayout
        {
            Width = 5,
            Height = 3,
            Start = new GridPosition(1, 1),
            Key = new GridPosition(1, 0),
            Gates = new[] { new GridPosition(2, 1), new GridPosition(3, 1) },
        };

        public const string Level5Program = @"for (int i = 0; i < 2; i++)
{
    if (HasKey())
    {
        OpenGate();
    }
    else
    {
        CollectKey();
        OpenGate();
    }
    MoveRight();
}";

        public const string Level1Program = "MoveRight();\nMoveRight();\nInteract();";
        public const string Level2Program = "MoveRight();\nMoveUp();\nMoveRight();\nInteract();";
        public const string Level3Program = "for (int i = 0; i < 4; i++)\n{\n    MoveRight();\n}\nInteract();";
        public const string Level4Program = "int pulses = 3;\nChargeCore(pulses);\nInteract();";

        public static readonly LevelGoal Level1Goal = new LevelGoal { TerminalActive = true };

        public static readonly LevelGoal Level2Goal = new LevelGoal { TerminalActive = true };

        public static readonly LevelGoal Level3Goal = new LevelGoal
        {
            AllModulesActive = true,
            TerminalActive = true,
            RequiredConcepts = ProgramConcept.ForLoop,
        };

        public static readonly LevelGoal Level4Goal = new LevelGoal
        {
            CoreCharge = 3,
            PowerDoorOpen = true,
            RequiredConcepts = ProgramConcept.IntVariableArgument,
        };

        public static readonly LevelGoal Level5Goal = new LevelGoal
        {
            AllGatesOpen = true,
            FinalPosition = new GridPosition(3, 1),
            RequiredConcepts = ProgramConcept.ForLoop | ProgramConcept.IfElse,
        };
    }
}
