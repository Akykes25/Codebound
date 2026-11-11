using Codebound.Language;
using Codebound.Levels;
using Codebound.Simulation;
using NUnit.Framework;
using static Codebound.Tests.EditMode.LevelDataTests;

namespace Codebound.Tests.EditMode
{
    public class LevelDebriefTests
    {
        private const string Solution = "MoveRight();\nMoveRight();\nInteract();";
        private const string Explanation = "Two moves leave the robot next to the terminal.";

        private static readonly LevelGoal TerminalGoal = new LevelGoal { TerminalActive = true };

        private static LevelDebrief DebriefLevel1(string source, LevelGoal goal = null)
        {
            var run = LevelRunner.Run(source, TestLevels.Level1);
            var verdict = LevelValidator.Evaluate(run, TestLevels.Level1, goal ?? TerminalGoal);
            return LevelDebrief.For(source, verdict, Solution, Explanation);
        }

        [Test]
        public void CompletingTheLevelWithTheExpectedProgramExplainsWhyItWorks()
        {
            var debrief = DebriefLevel1("MoveRight();  MoveRight(); // done\nInteract();");

            Assert.That(debrief, Is.Not.Null);
            Assert.That(debrief.MatchesSolution, Is.True);
            Assert.That(debrief.Title, Is.EqualTo("Así funciona tu solución"));
            Assert.That(debrief.Explanation, Is.EqualTo(Explanation));
            Assert.That(debrief.ReferenceSolution, Is.Null);
        }

        [Test]
        public void CompletingTheLevelAnotherWayAlsoShowsTheReferenceSolution()
        {
            var debrief = DebriefLevel1("MoveRight();\nMoveUp();\nMoveDown();\nMoveRight();\nInteract();");

            Assert.That(debrief, Is.Not.Null);
            Assert.That(debrief.MatchesSolution, Is.False);
            Assert.That(debrief.Title, Is.EqualTo("Tu programa también resuelve el nivel"));
            Assert.That(debrief.Explanation, Is.EqualTo(Explanation));
            Assert.That(debrief.ReferenceSolution, Is.EqualTo(Solution));
        }

        [Test]
        public void ALevelThatIsNotCompletedHasNoDebrief()
        {
            Assert.That(DebriefLevel1("MoveLeft();\nInteract();"), Is.Null);
            Assert.That(DebriefLevel1("MoveRight()\nInteract();"), Is.Null);
        }

        [Test]
        public void ReachingTheGoalWithoutTheRequiredConceptHasNoDebrief()
        {
            var goal = new LevelGoal { TerminalActive = true, RequiredConcepts = ProgramConcept.ForLoop };

            Assert.That(DebriefLevel1(Solution, goal), Is.Null);
        }

        [Test]
        public void EachLevelExplainsItsSolutionWhenItIsCompleted()
        {
            foreach (var level in LoadLevels())
            {
                var run = level.Run(level.SolutionCode);
                var debrief = level.Debrief(level.SolutionCode, level.Evaluate(run));

                Assert.That(debrief, Is.Not.Null, $"Nivel {level.Number}");
                Assert.That(debrief.MatchesSolution, Is.True, $"Nivel {level.Number}");
                Assert.That(debrief.Explanation, Is.EqualTo(level.SolutionExplanation), $"Nivel {level.Number}");
            }
        }

        [Test]
        public void NoLevelHasADebriefWhenItIsNotCompleted()
        {
            foreach (var level in LoadLevels())
            {
                const string wrong = "Interact();";
                var run = level.Run(wrong);

                Assert.That(level.Debrief(wrong, level.Evaluate(run)), Is.Null, $"Nivel {level.Number}");
            }
        }
    }
}
