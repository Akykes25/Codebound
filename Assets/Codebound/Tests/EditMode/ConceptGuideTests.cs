using System.Linq;
using Codebound.Language;
using Codebound.Levels;
using NUnit.Framework;
using UnityEditor;
using static Codebound.Tests.EditMode.LevelDataTests;

namespace Codebound.Tests.EditMode
{
    public class ConceptGuideTests
    {
        private static readonly ConceptEntry Calls =
            new ConceptEntry("Llamar a un método", 1, "Le da una orden al robot.", "MoveRight();");

        private static readonly ConceptEntry Loop =
            new ConceptEntry("Ciclo for", 3, "Repite un bloque.", "for (int i = 0; i < 3; i++)\n{\n    MoveDown();\n}");

        private static ConceptGuide LoadGuide()
        {
            return AssetDatabase.LoadAssetAtPath<ConceptGuide>("Assets/Codebound/Levels/Data/ConceptGuide.asset");
        }

        [Test]
        public void EachConceptShowsItsTitleExplanationAndExample()
        {
            Assert.That(ConceptGuideText.Format(new[] { Calls }, 2), Is.EqualTo(
                "Llamar a un método\nLe da una orden al robot.\nEjemplo:\nMoveRight();"));
        }

        [Test]
        public void TheConceptsOfTheCurrentLevelAreMarkedAsNew()
        {
            var text = ConceptGuideText.Format(new[] { Calls, Loop }, 3);

            Assert.That(text, Is.EqualTo(
                "Llamar a un método\nLe da una orden al robot.\nEjemplo:\nMoveRight();\n\n" +
                "Ciclo for (nuevo en este nivel)\nRepite un bloque.\nEjemplo:\nfor (int i = 0; i < 3; i++)\n{\n    MoveDown();\n}"));
        }

        [Test]
        public void NoConceptsShowNothing()
        {
            Assert.That(ConceptGuideText.Format(new ConceptEntry[0], 1), Is.Empty);
        }

        [Test]
        public void ALevelOnlyShowsTheConceptsLearnedUpToIt()
        {
            var entries = new[] { Calls, Loop };

            Assert.That(ConceptGuide.LearnedBy(entries, 1), Is.EqualTo(new[] { Calls }));
            Assert.That(ConceptGuide.LearnedBy(entries, 2), Is.EqualTo(new[] { Calls }));
            Assert.That(ConceptGuide.LearnedBy(entries, 3), Is.EqualTo(new[] { Calls, Loop }));
        }

        [Test]
        public void TheLearnedConceptsKeepTheGuideOrder()
        {
            Assert.That(ConceptGuide.LearnedBy(new[] { Loop, Calls }, 5), Is.EqualTo(new[] { Loop, Calls }));
        }

        [Test]
        public void EveryLevelIntroducesAtLeastOneConcept()
        {
            var guide = LoadGuide();

            foreach (var level in LoadLevels())
            {
                Assert.That(guide.Concepts.Any(concept => concept.IntroducedIn == level.Number), Is.True, $"Nivel {level.Number}");
            }

            Assert.That(guide.Concepts.Select(concept => concept.IntroducedIn), Is.Ordered);
        }

        [Test]
        public void EveryConceptHasItsTexts()
        {
            foreach (var concept in LoadGuide().Concepts)
            {
                Assert.That(concept.Title, Is.Not.Empty);
                Assert.That(concept.Explanation, Is.Not.Empty, concept.Title);
                Assert.That(concept.Example, Is.Not.Empty, concept.Title);
            }
        }

        [Test]
        public void TheFirstLevelOnlyShowsHowToCallAMethodAndTheLastShowsEverything()
        {
            var guide = LoadGuide();

            Assert.That(guide.For(LoadLevel(1)).Select(concept => concept.Title), Is.EqualTo(new[] { "Llamar a un método" }));
            Assert.That(guide.For(LoadLevel(5)), Is.EqualTo(guide.Concepts));
        }

        [Test]
        public void EachExampleCompilesWithTheActionsOfItsLevel()
        {
            foreach (var concept in LoadGuide().Concepts)
            {
                var run = LoadLevel(concept.IntroducedIn).Run(concept.Example);

                Assert.That(run.Diagnostics.Select(diagnostic => diagnostic.Message), Is.Empty, concept.Title);
            }
        }

        [Test]
        public void NoExampleSolvesItsLevel()
        {
            foreach (var concept in LoadGuide().Concepts)
            {
                var level = LoadLevel(concept.IntroducedIn);

                Assert.That(level.Evaluate(level.Run(concept.Example)).Completed, Is.False, concept.Title);
            }
        }

        [Test]
        public void TheExamplesOfALevelUseTheConceptItRequiresForTheFirstTime()
        {
            var guide = LoadGuide();
            var alreadyRequired = ProgramConcept.None;

            foreach (var level in LoadLevels())
            {
                var newlyRequired = level.CreateGoal().RequiredConcepts & ~alreadyRequired;
                var used = guide.Concepts
                    .Where(concept => concept.IntroducedIn == level.Number)
                    .Aggregate(ProgramConcept.None, (all, concept) => all | level.Run(concept.Example).Concepts);

                Assert.That(used & newlyRequired, Is.EqualTo(newlyRequired), $"Nivel {level.Number}");
                alreadyRequired |= level.CreateGoal().RequiredConcepts;
            }
        }
    }
}
