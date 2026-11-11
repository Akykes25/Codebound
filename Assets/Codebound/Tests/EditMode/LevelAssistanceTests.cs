using Codebound.Levels;
using NUnit.Framework;
using static Codebound.Tests.EditMode.LevelDataTests;

namespace Codebound.Tests.EditMode
{
    public class LevelAssistanceTests
    {
        private static LevelHelp CreateHelp()
        {
            return new LevelHelp(new[] { "first", "second" }, "Move(1);", "Moves once.");
        }

        [Test]
        public void ANewAttemptIsNotAssisted()
        {
            var help = CreateHelp();

            Assert.That(help.IsAssisted, Is.False);
            Assert.That(help.Hints.Revealed, Is.Empty);
        }

        [Test]
        public void RevealingEveryHintDoesNotMarkTheAttemptAsAssisted()
        {
            var help = CreateHelp();

            while (help.Hints.HasNext)
            {
                help.Hints.RevealNext();
            }

            Assert.That(help.Hints.Revealed, Is.EqualTo(new[] { "first", "second" }));
            Assert.That(help.IsAssisted, Is.False);
        }

        [Test]
        public void OpeningTheSolutionShowsItAndMarksTheAttemptAsAssisted()
        {
            var help = CreateHelp();

            var solution = help.OpenSolution();

            Assert.That(solution.Code, Is.EqualTo("Move(1);"));
            Assert.That(solution.Explanation, Is.EqualTo("Moves once."));
            Assert.That(help.IsAssisted, Is.True);
        }

        [Test]
        public void TheMarkStaysAfterTheSolutionIsOpenedAgain()
        {
            var help = CreateHelp();

            help.OpenSolution();
            help.OpenSolution();

            Assert.That(help.IsAssisted, Is.True);
        }

        [Test]
        public void ALevelCreatesAnUnassistedHelpWithItsOwnHintsAndSolution()
        {
            var level = LoadLevel(2);

            var help = level.CreateHelp();

            Assert.That(help.IsAssisted, Is.False);
            Assert.That(help.Hints.RevealNext(), Is.EqualTo(level.Hints[0]));
            var solution = help.OpenSolution();
            Assert.That(solution.Code, Is.EqualTo(level.SolutionCode));
            Assert.That(solution.Explanation, Is.EqualTo(level.SolutionExplanation));
        }

        [Test]
        public void EachAttemptStartsUnassistedEvenIfAnEarlierOneOpenedTheSolution()
        {
            var level = LoadLevel(1);

            level.CreateHelp().OpenSolution();

            Assert.That(level.CreateHelp().IsAssisted, Is.False);
        }
    }
}
