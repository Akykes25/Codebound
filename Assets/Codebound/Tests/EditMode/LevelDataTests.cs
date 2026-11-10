using System.Collections.Generic;
using System.Linq;
using Codebound.Language;
using Codebound.Levels;
using Codebound.Simulation;
using NUnit.Framework;
using UnityEditor;
using static Codebound.Tests.EditMode.TestLevels;

namespace Codebound.Tests.EditMode
{
    public class LevelDataTests
    {
        private const string DataFolder = "Assets/Codebound/Levels/Data";

        internal static readonly Dictionary<int, string> GddPrograms = new Dictionary<int, string>
        {
            { 1, Level1Program },
            { 2, Level2Program },
            { 3, Level3Program },
            { 4, Level4Program },
            { 5, Level5Program },
        };

        internal static List<LevelDefinition> LoadLevels()
        {
            return AssetDatabase.FindAssets("t:" + nameof(LevelDefinition), new[] { DataFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .OrderBy(level => level.Number)
                .ToList();
        }

        internal static LevelDefinition LoadLevel(int number)
        {
            return LoadLevels().Single(level => level.Number == number);
        }

        private static ActionCatalog LoadCatalog()
        {
            return AssetDatabase.LoadAssetAtPath<ActionCatalog>(DataFolder + "/ActionCatalog.asset");
        }

        [Test]
        public void TheSliceHasFiveLevelsNumberedInOrder()
        {
            Assert.That(LoadLevels().Select(level => level.Number), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
        }

        [Test]
        public void EveryLevelHasItsTexts()
        {
            foreach (var level in LoadLevels())
            {
                Assert.That(level.Title, Is.Not.Empty, $"Nivel {level.Number}");
                Assert.That(level.Objective, Is.Not.Empty, $"Nivel {level.Number}");
                Assert.That(level.Concept, Is.Not.Empty, $"Nivel {level.Number}");
            }

            Assert.That(LoadLevel(1).Title, Is.EqualTo("La señal de arranque"));
            Assert.That(LoadLevel(5).Title, Is.EqualTo("El guardián del núcleo"));
        }

        [Test]
        public void OnlyTheFifthLevelIsTheBoss()
        {
            Assert.That(LoadLevels().Where(level => level.IsBoss).Select(level => level.Number), Is.EqualTo(new[] { 5 }));
        }

        [Test]
        public void EveryGddProgramCompletesItsLevelAsset()
        {
            foreach (var level in LoadLevels())
            {
                var run = level.Run(GddPrograms[level.Number]);
                var verdict = level.Evaluate(run);

                Assert.That(run.Diagnostics, Is.Empty, $"Nivel {level.Number}");
                Assert.That(verdict.Completed, Is.True,
                    $"Nivel {level.Number}: {string.Join(" ", verdict.PendingObjectives.Concat(verdict.MissingConcepts))}");
            }
        }

        [Test]
        public void TheLevelAssetsMatchTheGoalsOfTheTests()
        {
            Assert.That(LoadLevel(3).CreateGoal().RequiredConcepts, Is.EqualTo(Level3Goal.RequiredConcepts));
            Assert.That(LoadLevel(4).CreateGoal().CoreCharge, Is.EqualTo(Level4Goal.CoreCharge));
            Assert.That(LoadLevel(5).CreateGoal().FinalPosition, Is.EqualTo(Level5Goal.FinalPosition));
            Assert.That(LoadLevel(5).CreateLayout().Gates, Is.EqualTo(Level5.Gates));
            Assert.That(LoadLevel(2).CreateLayout().Walls, Is.EqualTo(Level2.Walls));
        }

        [Test]
        public void TheStarterCodeOfLevel2RunsButDoesNotSolveIt()
        {
            var level = LoadLevel(2);
            var run = level.Run(level.StarterCode);

            Assert.That(run.Compiled, Is.True);
            Assert.That(level.Evaluate(run).Completed, Is.False);
        }

        [Test]
        public void AMethodFromALaterLevelIsNotAvailableYet()
        {
            var run = LoadLevel(1).Run("ChargeCore(3);");

            Assert.That(run.Compiled, Is.False);
            Assert.That(run.Diagnostics.Single().Message, Is.EqualTo("El método ChargeCore todavía no está disponible en este nivel."));
        }

        [Test]
        public void LevelsOnlyListActionsThatExistInTheGame()
        {
            var gameMethods = GameApi.Catalog.Select(method => method.Name).ToList();
            foreach (var level in LoadLevels())
            {
                Assert.That(gameMethods, Is.SupersetOf(level.AvailableActions), $"Nivel {level.Number}");
            }
        }

        [Test]
        public void ActionsAccumulateFromOneLevelToTheNext()
        {
            var levels = LoadLevels();
            for (var i = 1; i < levels.Count; i++)
            {
                Assert.That(levels[i].AvailableActions, Is.SupersetOf(levels[i - 1].AvailableActions), $"Nivel {levels[i].Number}");
            }
        }

        [Test]
        public void TheCatalogExplainsEveryGameMethodOnce()
        {
            var catalog = LoadCatalog();

            Assert.That(catalog.Actions.Select(action => action.Method),
                Is.EquivalentTo(GameApi.Catalog.Select(method => method.Name)));
            Assert.That(catalog.Actions.All(action => action.Usage.StartsWith(action.Method) && action.Description.Length > 0), Is.True);
        }

        [Test]
        public void TheCatalogShowsOnlyTheActionsOfALevel()
        {
            var catalog = LoadCatalog();

            Assert.That(catalog.For(LoadLevel(1)).Select(action => action.Method),
                Is.EqualTo(new[] { "MoveRight", "MoveLeft", "MoveUp", "MoveDown", "Interact" }));
            Assert.That(catalog.For(LoadLevel(5)).Count, Is.EqualTo(GameApi.Catalog.Count));
        }

        [Test]
        public void TheCompilerReportsUnavailableMethodsWithoutRunningAnything()
        {
            var result = ProgramCompiler.Compile("MoveRight();\nOpenGate();", GameApi.Catalog, new[] { "MoveRight" });

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Diagnostics.Single().Line, Is.EqualTo(2));
        }
    }
}
