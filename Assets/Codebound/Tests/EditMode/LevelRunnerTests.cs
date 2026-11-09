using System.Linq;
using Codebound.Simulation;
using NUnit.Framework;
using static Codebound.Tests.EditMode.TestLevels;

namespace Codebound.Tests.EditMode
{
    public class LevelRunnerTests
    {
        [Test]
        public void Level1ProgramActivatesTheTerminal()
        {
            var run = LevelRunner.Run("MoveRight();\nMoveRight();\nInteract();", Level1);

            Assert.That(run.Compiled, Is.True);
            Assert.That(run.FinalState.Position, Is.EqualTo(new GridPosition(3, 1)));
            Assert.That(run.FinalState.TerminalActive, Is.True);
        }

        [Test]
        public void Level2ProgramGoesAroundTheObstacle()
        {
            var run = LevelRunner.Run("MoveRight();\nMoveUp();\nMoveRight();\nInteract();", Level2);

            Assert.That(run.FinalState.Position, Is.EqualTo(new GridPosition(3, 1)));
            Assert.That(run.FinalState.TerminalActive, Is.True);
        }

        [Test]
        public void Level3ProgramActivatesEveryModuleAndTheTerminal()
        {
            var run = LevelRunner.Run("for (int i = 0; i < 4; i++)\n{\n    MoveRight();\n}\nInteract();", Level3);

            Assert.That(run.FinalState.ActiveModules, Is.EquivalentTo(Level3.Modules));
            Assert.That(run.FinalState.TerminalActive, Is.True);
        }

        [Test]
        public void Level4ProgramChargesTheCoreAndOpensTheDoor()
        {
            var run = LevelRunner.Run("int pulses = 3;\nChargeCore(pulses);\nInteract();", Level4);

            Assert.That(run.FinalState.CoreCharge, Is.EqualTo(3));
            Assert.That(run.FinalState.PowerDoorOpen, Is.True);
        }

        [Test]
        public void Level5ProgramOpensBothGatesAndReachesTheLastOne()
        {
            var run = LevelRunner.Run(Level5Program, Level5);

            Assert.That(run.FinalState.HasKey, Is.True);
            Assert.That(run.FinalState.OpenGates, Is.EquivalentTo(Level5.Gates));
            Assert.That(run.FinalState.Position, Is.EqualTo(new GridPosition(3, 1)));
        }

        [Test]
        public void MovingUpLowersYAndMovingLeftLowersX()
        {
            var layout = new LevelLayout { Width = 5, Height = 5, Start = new GridPosition(2, 2) };

            var run = LevelRunner.Run("MoveUp();\nMoveLeft();\nMoveDown();\nMoveDown();", layout);

            Assert.That(run.Steps.Select(step => step.To), Is.EqualTo(new[]
            {
                new GridPosition(2, 1),
                new GridPosition(1, 1),
                new GridPosition(1, 2),
                new GridPosition(1, 3),
            }));
        }

        [Test]
        public void WalkingIntoAWallIsReportedAndTheProgramContinues()
        {
            var run = LevelRunner.Run("MoveRight();\nMoveRight();\nMoveUp();", Level2);

            var blocked = run.Steps[1];
            Assert.That(blocked.Succeeded, Is.False);
            Assert.That(blocked.From, Is.EqualTo(new GridPosition(2, 2)));
            Assert.That(blocked.To, Is.EqualTo(new GridPosition(2, 2)));
            Assert.That(blocked.Message, Is.EqualTo("El camino está bloqueado."));
            Assert.That(run.FinalState.Position, Is.EqualTo(new GridPosition(2, 1)));
        }

        [Test]
        public void TheEdgeOfTheGridBlocksMovement()
        {
            var layout = new LevelLayout { Width = 2, Height = 2, Start = new GridPosition(0, 0) };

            var run = LevelRunner.Run("MoveLeft();\nMoveUp();", layout);

            Assert.That(run.Steps.All(step => !step.Succeeded), Is.True);
            Assert.That(run.FinalState.Position, Is.EqualTo(new GridPosition(0, 0)));
        }

        [Test]
        public void ObjectsAndClosedDoorsBlockMovement()
        {
            Assert.That(LevelRunner.Run("MoveRight();\nMoveRight();\nMoveRight();", Level1).FinalState.Position, Is.EqualTo(new GridPosition(3, 1)));
            Assert.That(LevelRunner.Run("MoveRight();", Level4).Steps[0].Succeeded, Is.False);
            Assert.That(LevelRunner.Run("MoveUp();", Level4).Steps[0].Succeeded, Is.False);
            Assert.That(LevelRunner.Run("MoveUp();", Level5).Steps[0].Succeeded, Is.False);
            Assert.That(LevelRunner.Run("MoveRight();", Level5).Steps[0].Succeeded, Is.False);
        }

        [Test]
        public void InteractingAwayFromTheTerminalFailsWithAnExplanation()
        {
            var run = LevelRunner.Run("Interact();\nMoveRight();\nMoveRight();", Level1);

            Assert.That(run.Steps[0].Succeeded, Is.False);
            Assert.That(run.Steps[0].Message, Is.EqualTo("No hay nada para activar al lado del robot."));
            Assert.That(run.FinalState.TerminalActive, Is.False);
        }

        [Test]
        public void TheDoorStaysClosedWithTheWrongCharge()
        {
            var run = LevelRunner.Run("ChargeCore(2);\nInteract();", Level4);

            Assert.That(run.Steps[1].Succeeded, Is.False);
            Assert.That(run.Steps[1].Message, Is.EqualTo("La puerta necesita que el núcleo tenga carga 3."));
            Assert.That(run.FinalState.PowerDoorOpen, Is.False);
        }

        [Test]
        public void ChargingTheCoreSetsTheValueInsteadOfAddingIt()
        {
            var run = LevelRunner.Run("ChargeCore(2);\nChargeCore(1);", Level4);

            Assert.That(run.FinalState.CoreCharge, Is.EqualTo(1));
        }

        [Test]
        public void ChargingWithoutACoreNearbyFails()
        {
            var run = LevelRunner.Run("ChargeCore(3);", Level1);

            Assert.That(run.Steps[0].Succeeded, Is.False);
            Assert.That(run.Steps[0].Message, Is.EqualTo("No hay ningún núcleo al lado del robot."));
        }

        [Test]
        public void AGateCannotBeOpenedWithoutTheKey()
        {
            var run = LevelRunner.Run("OpenGate();", Level5);

            Assert.That(run.Steps[0].Succeeded, Is.False);
            Assert.That(run.Steps[0].Message, Is.EqualTo("Para abrir esta puerta hace falta la llave."));
            Assert.That(run.FinalState.OpenGates, Is.Empty);
        }

        [Test]
        public void OpenGateWithoutAClosedGateNearbyFails()
        {
            var run = LevelRunner.Run("CollectKey();\nOpenGate();\nOpenGate();", Level5);

            Assert.That(run.Steps[2].Succeeded, Is.False);
            Assert.That(run.Steps[2].Message, Is.EqualTo("No hay ninguna puerta cerrada al lado del robot."));
        }

        [Test]
        public void CollectingAKeyThatIsNotNearbyFails()
        {
            var run = LevelRunner.Run("CollectKey();", Level1);

            Assert.That(run.Steps[0].Succeeded, Is.False);
            Assert.That(run.Steps[0].Message, Is.EqualTo("No hay ninguna llave al lado del robot."));
            Assert.That(run.FinalState.HasKey, Is.False);
        }

        [Test]
        public void HasKeyAnswersWithTheCurrentState()
        {
            var run = LevelRunner.Run(Level5Program, Level5);

            Assert.That(run.Steps.Where(step => step.Method == "HasKey").Select(step => step.Result), Is.EqualTo(new object[] { false, true }));
        }

        [Test]
        public void EveryStepKnowsItsLineInTheProgram()
        {
            var run = LevelRunner.Run("MoveRight();\n\nMoveRight();", Level1);

            Assert.That(run.Steps.Select(step => step.Line), Is.EqualTo(new[] { 1, 3 }));
            Assert.That(run.Steps[1].Column, Is.EqualTo(1));
        }

        [Test]
        public void AProgramWithErrorsDoesNotMoveTheRobot()
        {
            var run = LevelRunner.Run("MoveRight()\nInteract();", Level1);

            Assert.That(run.Compiled, Is.False);
            Assert.That(run.Diagnostics, Is.Not.Empty);
            Assert.That(run.Steps, Is.Empty);
            Assert.That(run.FinalState.Position, Is.EqualTo(Level1.Start));
        }

        [Test]
        public void MethodsOutsideTheGameCatalogAreRejected()
        {
            var run = LevelRunner.Run("Explode();", Level1);

            Assert.That(run.Compiled, Is.False);
        }

        [Test]
        public void ARunThatPassesTheStepLimitStopsAndKeepsItsState()
        {
            var run = LevelRunner.Run("for (int i = 0; i < 1000; i++)\n{\n    MoveLeft();\n}", Level1);

            Assert.That(run.StepLimitExceeded, Is.True);
            Assert.That(run.FinalState.Position, Is.EqualTo(new GridPosition(0, 1)));
        }

        [Test]
        public void RunningTwiceGivesTheSameStepsAndDoesNotShareState()
        {
            var first = LevelRunner.Run(Level5Program, Level5);
            var second = LevelRunner.Run(Level5Program, Level5);

            Assert.That(second.Steps.Select(step => step.ToString()), Is.EqualTo(first.Steps.Select(step => step.ToString())));
            Assert.That(second.Steps[0].Result, Is.EqualTo(false));
        }
    }
}
