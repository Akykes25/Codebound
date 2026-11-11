using System.Linq;
using Codebound.Language;
using Codebound.Levels;
using Codebound.Simulation;
using NUnit.Framework;
using static Codebound.Tests.EditMode.LevelDataTests;

namespace Codebound.Tests.EditMode
{
    public class LevelAdviceTests
    {
        private const string Advice = "Contá las casillas hasta la terminal.";
        private const string Solution = "MoveRight();\nMoveRight();\nInteract();";

        private static readonly LevelGoal TerminalGoal = new LevelGoal { TerminalActive = true };

        private static string AdviseLevel1(string source, string advice = Advice, string solution = Solution)
        {
            var run = LevelRunner.Run(source, TestLevels.Level1);
            var verdict = LevelValidator.Evaluate(run, TestLevels.Level1, TerminalGoal);
            return LevelAdvisor.Advise(source, run, verdict, solution, advice);
        }

        [Test]
        public void TheSameProgramWithOtherSpacingAndCommentsMatchesTheSolution()
        {
            const string player = "MoveRight();   // first step\n\n  MoveRight();\n/* now */ Interact();";

            Assert.That(ProgramComparer.AreEquivalent(player, Solution), Is.True);
        }

        [Test]
        public void ADifferentNameForTheSameVariableMatchesTheSolution()
        {
            Assert.That(
                ProgramComparer.AreEquivalent("int carga = 3;\nChargeCore(carga);", "int pulses = 3;\nChargeCore(pulses);"),
                Is.True);
            Assert.That(
                ProgramComparer.AreEquivalent(
                    "for (int j = 0; j < 4; j++) { MoveRight(); }",
                    "for (int i = 0; i < 4; i++)\n{\n    MoveRight();\n}"),
                Is.True);
        }

        [Test]
        public void ADifferentMovementOrValueDoesNotMatchTheSolution()
        {
            Assert.That(ProgramComparer.AreEquivalent("MoveLeft();\nMoveLeft();\nInteract();", Solution), Is.False);
            Assert.That(ProgramComparer.AreEquivalent("int pulses = 2;\nChargeCore(pulses);", "int pulses = 3;\nChargeCore(pulses);"), Is.False);
            Assert.That(ProgramComparer.AreEquivalent("MoveRight();\nInteract();", Solution), Is.False);
        }

        [Test]
        public void ARunThatMovesTheWrongWayGetsTheLevelAdvice()
        {
            Assert.That(AdviseLevel1("MoveLeft();\nMoveLeft();\nInteract();"), Is.EqualTo(Advice));
        }

        [Test]
        public void EveryWrongProgramOfALevelGetsTheSameAdvice()
        {
            var wrongPrograms = new[]
            {
                "MoveLeft();\nInteract();",
                "Interact();\nMoveRight();\nMoveRight();",
                "MoveRight();\nMoveRight();",
                "MoveUp();",
            };

            Assert.That(wrongPrograms.Select(program => AdviseLevel1(program)), Has.All.EqualTo(Advice));
        }

        [Test]
        public void ARunThatCompletesTheLevelGetsNoAdvice()
        {
            Assert.That(AdviseLevel1(Solution), Is.Null);
        }

        [Test]
        public void CodeWithErrorsGetsItsErrorMessageInsteadOfTheAdvice()
        {
            Assert.That(AdviseLevel1("MoveLeft()\nInteract();"), Is.Null);
            Assert.That(AdviseLevel1("Jump();"), Is.Null);
        }

        [Test]
        public void CodeThatMatchesTheSolutionGetsNoAdviceEvenIfTheWorldIsNotSolved()
        {
            const string wrongSolution = "MoveRight();\nInteract();";

            Assert.That(AdviseLevel1("MoveRight();   Interact();", solution: wrongSolution), Is.Null);
        }

        [Test]
        public void ALevelWithoutAdviceGivesNone()
        {
            Assert.That(AdviseLevel1("MoveLeft();", advice: ""), Is.Null);
        }

        [Test]
        public void ReachingTheGoalWithoutTheRequiredConceptGetsTheConceptMessageNotTheAdvice()
        {
            var goal = new LevelGoal { TerminalActive = true, RequiredConcepts = ProgramConcept.ForLoop };
            var run = LevelRunner.Run(Solution, TestLevels.Level1);
            var verdict = LevelValidator.Evaluate(run, TestLevels.Level1, goal);

            Assert.That(verdict.MissingConcepts, Is.Not.Empty);
            Assert.That(LevelAdvisor.Advise(Solution, run, verdict, "for (int i = 0; i < 3; i++) { MoveRight(); }\nInteract();", Advice), Is.Null);
        }

        [Test]
        public void EveryLevelHasItsOwnAdvice()
        {
            var levels = LoadLevels();

            Assert.That(levels.Select(level => level.Advice), Has.All.Not.Empty);
            Assert.That(levels.Select(level => level.Advice).Distinct().Count(), Is.EqualTo(levels.Count));
        }

        [Test]
        public void NoAdviceWritesTheSolution()
        {
            foreach (var level in LoadLevels())
            {
                var firstLine = level.SolutionCode.Replace("\r\n", "\n").Split('\n')[0].Trim();
                Assert.That(level.Advice, Does.Not.Contain(level.SolutionCode.Trim()), $"Nivel {level.Number}");
                Assert.That(level.Advice, Does.Not.Contain(firstLine), $"Nivel {level.Number}");
            }
        }

        [Test]
        public void EachLevelGivesItsAdviceWhenTheProgramDoesNotReachTheGoal()
        {
            foreach (var level in LoadLevels())
            {
                const string wrong = "Interact();";
                var run = level.Run(wrong);

                Assert.That(level.Advise(wrong, run, level.Evaluate(run)), Is.EqualTo(level.Advice), $"Nivel {level.Number}");
            }
        }

        [Test]
        public void TheSolutionOfEachLevelGetsNoAdvice()
        {
            foreach (var level in LoadLevels())
            {
                var run = level.Run(level.SolutionCode);

                Assert.That(level.Advise(level.SolutionCode, run, level.Evaluate(run)), Is.Null, $"Nivel {level.Number}");
            }
        }
    }
}
