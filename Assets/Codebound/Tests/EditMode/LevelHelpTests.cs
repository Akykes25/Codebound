using System.Linq;
using Codebound.Levels;
using NUnit.Framework;
using static Codebound.Tests.EditMode.LevelDataTests;

namespace Codebound.Tests.EditMode
{
    public class LevelHelpTests
    {
        private static string Normalize(string code)
        {
            return code.Replace("\r\n", "\n").Trim();
        }

        [Test]
        public void EveryLevelHasStepByStepHintsAndAnExplainedSolution()
        {
            foreach (var level in LoadLevels())
            {
                Assert.That(level.Hints.Count, Is.GreaterThanOrEqualTo(2), $"Nivel {level.Number}");
                Assert.That(level.Hints, Has.All.Not.Empty, $"Nivel {level.Number}");
                Assert.That(level.SolutionCode, Is.Not.Empty, $"Nivel {level.Number}");
                Assert.That(level.SolutionExplanation, Is.Not.Empty, $"Nivel {level.Number}");
            }
        }

        [Test]
        public void EverySolutionCompletesItsLevelWithItsOwnActions()
        {
            foreach (var level in LoadLevels())
            {
                var run = level.Run(level.SolutionCode);
                var verdict = level.Evaluate(run);

                Assert.That(run.Diagnostics, Is.Empty, $"Nivel {level.Number}");
                Assert.That(verdict.Completed, Is.True,
                    $"Nivel {level.Number}: {string.Join(" ", verdict.PendingObjectives.Concat(verdict.MissingConcepts))}");
            }
        }

        [Test]
        public void TheSolutionsAreTheGddPrograms()
        {
            foreach (var level in LoadLevels())
            {
                Assert.That(Normalize(level.SolutionCode), Is.EqualTo(Normalize(GddPrograms[level.Number])), $"Nivel {level.Number}");
            }
        }

        [Test]
        public void NoHintWritesTheWholeSolution()
        {
            foreach (var level in LoadLevels())
            {
                var solution = Normalize(level.SolutionCode);
                Assert.That(level.Hints.Any(hint => Normalize(hint).Contains(solution)), Is.False, $"Nivel {level.Number}");
            }
        }

        [Test]
        public void TheFirstHintOfLevel3OffersAForTemplate()
        {
            Assert.That(LoadLevel(3).Hints[0], Does.Contain("for (int i = 0;"));
        }

        [Test]
        public void TheHintsOfLevel4DoNotGiveAwayTheCharge()
        {
            Assert.That(LoadLevel(4).Hints, Has.None.Contains("3"));
        }

        [Test]
        public void EachSolutionExplainsWhatTheGddAsksFor()
        {
            Assert.That(LoadLevel(1).SolutionExplanation, Does.Contain("Interact"));
            Assert.That(LoadLevel(2).SolutionExplanation, Does.Contain("válido"));
            Assert.That(LoadLevel(3).SolutionExplanation, Does.Contain("contador").And.Contain("condición").And.Contain("i++"));
            Assert.That(LoadLevel(4).SolutionExplanation, Does.Contain("pulses").And.Contain("ChargeCore"));
            Assert.That(LoadLevel(5).SolutionExplanation, Does.Contain("HasKey").And.Contain("else"));
        }

        [Test]
        public void AHintSequenceStartsWithNothingRevealed()
        {
            var sequence = new HintSequence(new[] { "a", "b" });

            Assert.That(sequence.Revealed, Is.Empty);
            Assert.That(sequence.HasNext, Is.True);
        }

        [Test]
        public void AHintSequenceRevealsHintsInOrderUntilNoneAreLeft()
        {
            var sequence = new HintSequence(new[] { "a", "b" });

            Assert.That(sequence.RevealNext(), Is.EqualTo("a"));
            Assert.That(sequence.RevealNext(), Is.EqualTo("b"));
            Assert.That(sequence.HasNext, Is.False);
            Assert.That(sequence.RevealNext(), Is.Null);
            Assert.That(sequence.Revealed, Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public void ALevelCreatesASequenceWithItsOwnHints()
        {
            var level = LoadLevel(1);

            Assert.That(level.CreateHintSequence().RevealNext(), Is.EqualTo(level.Hints[0]));
        }
    }
}
