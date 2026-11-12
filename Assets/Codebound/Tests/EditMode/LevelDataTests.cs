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

        internal static ActionCatalog LoadCatalog()
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
            Assert.That(LoadLevel(5).CreateLayout().Gates, Is.EqualTo(Level5.Gates));
            Assert.That(LoadLevel(2).CreateLayout().Walls, Is.EqualTo(Level2.Walls));
        }

        [Test]
        public void TheFinalLevelAssetIsTheFinalRoomOfTheTests()
        {
            var layout = LoadLevel(5).CreateLayout();
            var goal = LoadLevel(5).CreateGoal();

            Assert.That(new[] { layout.Width, layout.Height }, Is.EqualTo(new[] { Level5.Width, Level5.Height }));
            Assert.That(layout.Start, Is.EqualTo(Level5.Start));
            Assert.That(layout.Key, Is.EqualTo(Level5.Key));
            Assert.That(layout.Modules, Is.EqualTo(Level5.Modules));
            Assert.That(layout.Core, Is.EqualTo(Level5.Core));
            Assert.That(layout.PowerDoor, Is.EqualTo(Level5.PowerDoor));
            Assert.That(layout.RequiredCharge, Is.EqualTo(Level5.RequiredCharge));
            Assert.That(layout.Terminal, Is.EqualTo(Level5.Terminal));
            Assert.That(layout.Walls, Is.Empty);

            Assert.That(goal.TerminalActive, Is.EqualTo(Level5Goal.TerminalActive));
            Assert.That(goal.AllModulesActive, Is.EqualTo(Level5Goal.AllModulesActive));
            Assert.That(goal.CoreCharge, Is.EqualTo(Level5Goal.CoreCharge));
            Assert.That(goal.PowerDoorOpen, Is.EqualTo(Level5Goal.PowerDoorOpen));
            Assert.That(goal.AllGatesOpen, Is.EqualTo(Level5Goal.AllGatesOpen));
            Assert.That(goal.FinalPosition, Is.EqualTo(Level5Goal.FinalPosition));
            Assert.That(goal.RequiredConcepts, Is.EqualTo(Level5Goal.RequiredConcepts));
        }

        [Test]
        public void TheFinalLevelIsTheLargestRoomAndAsksForEveryConcept()
        {
            var levels = LoadLevels();
            var final = levels.Last();

            Assert.That(final.IsBoss, Is.True);
            Assert.That(final.IsTimed, Is.True);
            var finalLayout = final.CreateLayout();
            foreach (var level in levels.Take(levels.Count - 1))
            {
                var layout = level.CreateLayout();
                Assert.That(finalLayout.Width * finalLayout.Height, Is.GreaterThan(layout.Width * layout.Height), $"Nivel {level.Number}");
                Assert.That(final.CreateGoal().RequiredConcepts.HasFlag(level.CreateGoal().RequiredConcepts), Is.True, $"Nivel {level.Number}");
            }

            Assert.That(final.CreateGoal().RequiredConcepts,
                Is.EqualTo(ProgramConcept.ForLoop | ProgramConcept.IfElse | ProgramConcept.IntVariableArgument));
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
