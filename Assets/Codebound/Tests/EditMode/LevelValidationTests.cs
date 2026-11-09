using Codebound.Language;
using Codebound.Simulation;
using NUnit.Framework;
using static Codebound.Tests.EditMode.TestLevels;

namespace Codebound.Tests.EditMode
{
    public class LevelValidationTests
    {
        private static LevelVerdict Evaluate(string program, LevelLayout layout, LevelGoal goal)
        {
            return LevelValidator.Evaluate(LevelRunner.Run(program, layout), layout, goal);
        }

        [Test]
        public void EveryGddProgramCompletesItsLevel()
        {
            Assert.That(Evaluate(Level1Program, Level1, Level1Goal).Completed, Is.True, "Nivel 1");
            Assert.That(Evaluate(Level2Program, Level2, Level2Goal).Completed, Is.True, "Nivel 2");
            Assert.That(Evaluate(Level3Program, Level3, Level3Goal).Completed, Is.True, "Nivel 3");
            Assert.That(Evaluate(Level4Program, Level4, Level4Goal).Completed, Is.True, "Nivel 4");
            Assert.That(Evaluate(Level5Program, Level5, Level5Goal).Completed, Is.True, "Nivel 5");
        }

        [Test]
        public void InteractingBeforeMovingDoesNotCompleteLevel1()
        {
            var verdict = Evaluate("Interact();\nMoveRight();\nMoveRight();", Level1, Level1Goal);

            Assert.That(verdict.Completed, Is.False);
            Assert.That(verdict.PendingObjectives, Is.EqualTo(new[] { "La terminal sigue apagada." }));
        }

        [Test]
        public void Level2FailsWhenTheRobotWalksIntoTheObstacle()
        {
            var verdict = Evaluate("MoveRight();\nMoveRight();\nInteract();", Level2, Level2Goal);

            Assert.That(verdict.Completed, Is.False);
        }

        [Test]
        public void Level3SolvedWithoutAForAsksForTheLoop()
        {
            var verdict = Evaluate("MoveRight();\nMoveRight();\nMoveRight();\nMoveRight();\nInteract();", Level3, Level3Goal);

            Assert.That(verdict.Completed, Is.False);
            Assert.That(verdict.PendingObjectives, Is.Empty);
            Assert.That(verdict.MissingConcepts, Is.EqualTo(new[] { "Llegaste, pero este nivel te pide repetir acciones con un for." }));
        }

        [Test]
        public void Level3CountsTheModulesStillOff()
        {
            var verdict = Evaluate("for (int i = 0; i < 2; i++)\n{\n    MoveRight();\n}", Level3, Level3Goal);

            Assert.That(verdict.PendingObjectives, Is.EqualTo(new[] { "La terminal sigue apagada.", "Faltan activar 2 módulos." }));
            Assert.That(verdict.MissingConcepts, Is.Empty);
        }

        [Test]
        public void TheWrongChargeLeavesLevel4Unfinished()
        {
            var verdict = Evaluate("int pulses = 2;\nChargeCore(pulses);\nInteract();", Level4, Level4Goal);

            Assert.That(verdict.Completed, Is.False);
            Assert.That(verdict.PendingObjectives, Is.EqualTo(new[]
            {
                "El núcleo tiene carga 2 y necesita 3.",
                "La puerta de energía sigue cerrada.",
            }));
        }

        [Test]
        public void Level4WithALiteralChargeAsksForTheVariable()
        {
            var verdict = Evaluate("ChargeCore(3);\nInteract();", Level4, Level4Goal);

            Assert.That(verdict.PendingObjectives, Is.Empty);
            Assert.That(verdict.MissingConcepts, Is.EqualTo(new[]
            {
                "Llegaste, pero este nivel te pide guardar el valor en una variable int y pasarla al método.",
            }));
        }

        [Test]
        public void Level5WithoutIfElseAsksForTheDecision()
        {
            const string program = "CollectKey();\nfor (int i = 0; i < 2; i++)\n{\n    OpenGate();\n    MoveRight();\n}";

            var verdict = Evaluate(program, Level5, Level5Goal);

            Assert.That(verdict.PendingObjectives, Is.Empty);
            Assert.That(verdict.MissingConcepts, Is.EqualTo(new[] { "Llegaste, pero este nivel te pide decidir con un if y su else." }));
        }

        [Test]
        public void Level5RequiresTheRobotToReachTheLastGate()
        {
            const string program = "for (int i = 0; i < 2; i++)\n{\n    if (HasKey())\n    {\n        OpenGate();\n    }\n    else\n    {\n        CollectKey();\n        OpenGate();\n    }\n    if (i < 1)\n    {\n        MoveRight();\n    }\n}";

            var verdict = Evaluate(program, Level5, Level5Goal);

            Assert.That(verdict.Completed, Is.False);
            Assert.That(verdict.PendingObjectives, Is.EqualTo(new[] { "El robot tiene que terminar en (3,1)." }));
        }

        [Test]
        public void MissingConceptsAreReportedEvenWhenTheWorldIsNotSolved()
        {
            var verdict = Evaluate("MoveRight();", Level5, Level5Goal);

            Assert.That(verdict.MissingConcepts, Is.EqualTo(new[]
            {
                "Este nivel te pide repetir acciones con un for.",
                "Este nivel te pide decidir con un if y su else.",
            }));
        }

        [Test]
        public void CodeWithErrorsNeverCompletesALevel()
        {
            var verdict = Evaluate("MoveRight()\nInteract();", Level1, Level1Goal);

            Assert.That(verdict.Completed, Is.False);
            Assert.That(verdict.PendingObjectives, Is.EqualTo(new[] { "El código tiene errores. Corregilos y ejecutá de nuevo." }));
        }

        [Test]
        public void AProgramStoppedAtTheStepLimitNeverCompletesALevel()
        {
            var verdict = Evaluate("for (int i = 0; i < 2000000000; i++)\n{\n    Interact();\n}", Level1, new LevelGoal());

            Assert.That(verdict.Completed, Is.False);
            Assert.That(verdict.PendingObjectives, Is.EqualTo(new[] { "El programa hizo demasiados pasos y se detuvo antes de terminar." }));
        }

        [Test]
        public void ConceptsAreReadFromTheCode()
        {
            Assert.That(LevelRunner.Run(Level1Program, Level1).Concepts, Is.EqualTo(ProgramConcept.None));
            Assert.That(LevelRunner.Run(Level3Program, Level3).Concepts, Is.EqualTo(ProgramConcept.ForLoop));
            Assert.That(LevelRunner.Run(Level4Program, Level4).Concepts, Is.EqualTo(ProgramConcept.IntVariableArgument));
            Assert.That(LevelRunner.Run(Level5Program, Level5).Concepts, Is.EqualTo(ProgramConcept.ForLoop | ProgramConcept.IfElse));
        }

        [Test]
        public void AnIfWithoutElseIsNotTheIfElseConcept()
        {
            var run = LevelRunner.Run("if (HasKey())\n{\n    OpenGate();\n}", Level5);

            Assert.That(run.Concepts, Is.EqualTo(ProgramConcept.None));
        }

        [Test]
        public void ALoopVariablePassedToACallCountsAsAnIntArgument()
        {
            var run = LevelRunner.Run("for (int i = 0; i < 4; i++)\n{\n    ChargeCore(i);\n}", Level4);

            Assert.That(run.Concepts, Is.EqualTo(ProgramConcept.ForLoop | ProgramConcept.IntVariableArgument));
        }
    }
}
