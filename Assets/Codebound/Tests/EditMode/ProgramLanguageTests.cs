using System.Collections.Generic;
using System.Linq;
using Codebound.Language;
using NUnit.Framework;

namespace Codebound.Tests.EditMode
{
    public class ProgramLanguageTests
    {
        private static readonly MethodSignature[] Catalog =
        {
            new MethodSignature("MoveRight", ValueKind.Void),
            new MethodSignature("MoveLeft", ValueKind.Void),
            new MethodSignature("MoveUp", ValueKind.Void),
            new MethodSignature("MoveDown", ValueKind.Void),
            new MethodSignature("Interact", ValueKind.Void),
            new MethodSignature("ChargeCore", ValueKind.Void, ValueKind.Int),
            new MethodSignature("HasKey", ValueKind.Bool),
            new MethodSignature("CollectKey", ValueKind.Void),
            new MethodSignature("OpenGate", ValueKind.Void),
        };

        private sealed class FakeHost : IProgramHost
        {
            private bool hasKey;

            public object Invoke(string method, IReadOnlyList<object> arguments)
            {
                if (method == "CollectKey")
                {
                    hasKey = true;
                }

                return method == "HasKey" ? (object)hasKey : null;
            }
        }

        private static CompilationResult Compile(string source)
        {
            return ProgramCompiler.Compile(source, Catalog);
        }

        private static ExecutionResult Run(string source)
        {
            var compilation = Compile(source);
            var errors = string.Join("; ", compilation.Diagnostics.Select(d => d.ToString()));
            Assert.That(compilation.Succeeded, Is.True, errors);
            return ProgramInterpreter.Run(compilation.Program, new FakeHost());
        }

        private static string[] Calls(ExecutionResult result)
        {
            return result.Trace.Select(entry => entry.ToString()).ToArray();
        }

        private static CodeDiagnostic SingleError(string source)
        {
            var compilation = Compile(source);
            Assert.That(compilation.Succeeded, Is.False);
            Assert.That(compilation.Diagnostics, Is.Not.Empty);
            return compilation.Diagnostics[0];
        }

        private const string Level1 = "MoveRight();\nMoveRight();\nInteract();";
        private const string Level2 = "MoveRight();\nMoveUp();\nMoveRight();\nInteract();";
        private const string Level3 = "for (int i = 0; i < 4; i++)\n{\n    MoveRight();\n}\nInteract();";
        private const string Level4 = "int pulses = 3;\nChargeCore(pulses);\nInteract();";
        private const string Level5 =
            "for (int i = 0; i < 2; i++)\n{\n    if (HasKey())\n    {\n        OpenGate();\n    }\n    else\n    {\n" +
            "        CollectKey();\n        OpenGate();\n    }\n    MoveRight();\n}";

        [Test]
        public void Level1_RunsCallsInOrder()
        {
            Assert.That(Calls(Run(Level1)), Is.EqualTo(new[] { "1:MoveRight()", "2:MoveRight()", "3:Interact()" }));
        }

        [Test]
        public void Level2_RunsTheCorrectedSequence()
        {
            Assert.That(Calls(Run(Level2)), Is.EqualTo(new[]
            {
                "1:MoveRight()", "2:MoveUp()", "3:MoveRight()", "4:Interact()",
            }));
        }

        [Test]
        public void Level3_ForRepeatsTheBodyFourTimes()
        {
            Assert.That(Calls(Run(Level3)), Is.EqualTo(new[]
            {
                "3:MoveRight()", "3:MoveRight()", "3:MoveRight()", "3:MoveRight()", "5:Interact()",
            }));
        }

        [Test]
        public void Level4_PassesTheVariableValueToTheCall()
        {
            Assert.That(Calls(Run(Level4)), Is.EqualTo(new[] { "2:ChargeCore(3)", "3:Interact()" }));
        }

        [Test]
        public void Level5_RunsBothBranchesAcrossIterations()
        {
            Assert.That(Calls(Run(Level5)), Is.EqualTo(new[]
            {
                "3:HasKey()=False", "9:CollectKey()", "10:OpenGate()", "12:MoveRight()",
                "3:HasKey()=True", "5:OpenGate()", "12:MoveRight()",
            }));
        }

        [TestCase(Level1)]
        [TestCase(Level2)]
        [TestCase(Level3)]
        [TestCase(Level4)]
        [TestCase(Level5)]
        public void RepeatedRuns_ProduceIdenticalTraces(string source)
        {
            Assert.That(Calls(Run(source)), Is.EqualTo(Calls(Run(source))));
        }

        [Test]
        public void CompletedRun_ReportsCompletedStatus()
        {
            Assert.That(Run(Level1).Status, Is.EqualTo(ExecutionStatus.Completed));
        }

        [Test]
        public void Comments_AreIgnored()
        {
            Assert.That(Calls(Run("// start\nMoveRight(); /* inline */\n/* multi\nline */ Interact();")),
                Is.EqualTo(new[] { "2:MoveRight()", "4:Interact()" }));
        }

        [Test]
        public void WindowsLineEndings_KeepLineNumbers()
        {
            Assert.That(Calls(Run("MoveRight();\r\nInteract();")), Is.EqualTo(new[] { "1:MoveRight()", "2:Interact()" }));
        }

        [Test]
        public void SingleStatementBodies_AreValidCSharp()
        {
            Assert.That(Calls(Run("for (int i = 0; i < 2; i++) MoveRight();")),
                Is.EqualTo(new[] { "1:MoveRight()", "1:MoveRight()" }));
        }

        [Test]
        public void LessThanCondition_ChoosesTheBranch()
        {
            Assert.That(Calls(Run("int a = 1;\nif (a < 2) { MoveUp(); } else { MoveDown(); }")),
                Is.EqualTo(new[] { "2:MoveUp()" }));
        }

        [Test]
        public void MissingSemicolon_PointsAtTheLineThatLacksIt()
        {
            var error = SingleError("MoveRight()\nInteract();");
            Assert.That(error.Line, Is.EqualTo(1));
            Assert.That(error.Message, Does.Contain(";"));
        }

        [Test]
        public void MissingClosingBrace_IsReported()
        {
            Assert.That(SingleError("for (int i = 0; i < 2; i++)\n{\n    MoveRight();\n").Message, Does.Contain("}"));
        }

        [Test]
        public void UnknownMethod_IsRejectedWithItsLine()
        {
            var error = SingleError("MoveRight();\nJump();");
            Assert.That(error.Line, Is.EqualTo(2));
            Assert.That(error.Message, Does.Contain("Jump"));
        }

        [TestCase("while (HasKey()) { MoveRight(); }", "while")]
        [TestCase("foreach (int a in b) { }", "foreach")]
        [TestCase("do { MoveRight(); } while (HasKey());", "do")]
        [TestCase("else { }", "else")]
        public void UnsupportedStatements_AreRejected(string source, string keyword)
        {
            Assert.That(SingleError(source).Message, Does.Contain(keyword));
        }

        [TestCase("System.IO.File.Delete();")]
        [TestCase("player.MoveRight();")]
        [TestCase("new System.Random();")]
        [TestCase("string a = \"x\";")]
        public void CodeOutsideTheCatalog_IsRejected(string source)
        {
            Assert.That(Compile(source).Succeeded, Is.False);
        }

        [TestCase("ChargeCore(missing);", "missing")]
        [TestCase("ChargeCore(HasKey());", "ChargeCore")]
        [TestCase("ChargeCore();", "ChargeCore")]
        [TestCase("ChargeCore(1, 2);", "ChargeCore")]
        [TestCase("if (3) { }", "if")]
        [TestCase("int a = HasKey();", "a")]
        [TestCase("ChargeCore(Interact());", "Interact")]
        [TestCase("int a = 1;\nint a = 2;", "a")]
        [TestCase("int a = 1;\nif (HasKey()) { int a = 2; }", "a")]
        [TestCase("a = 3;", "=")]
        [TestCase("int i = 0;\ni++;", "++")]
        [TestCase("int a = 99999999999;", "99999999999")]
        public void InvalidPrograms_AreRejectedWithTheCulprit(string source, string culprit)
        {
            Assert.That(SingleError(source).Message, Does.Contain(culprit));
        }

        [TestCase("for (int i = 0; j < 2; i++) { }")]
        [TestCase("for (int i = 0; i < 2; j++) { }")]
        [TestCase("for (int i = 0; i > 2; i++) { }")]
        [TestCase("for (int i = 0; i < 2; i--) { }")]
        [TestCase("for (int i = 0; i < HasKey(); i++) { }")]
        public void ForHeaders_OutsideTheCountedForm_AreRejected(string source)
        {
            Assert.That(Compile(source).Succeeded, Is.False);
        }

        [TestCase("for (int i = 0; i < 4; i++) int a = 1;")]
        [TestCase("if (HasKey()) int a = 1;")]
        [TestCase("if (HasKey()) { } else int a = 1;")]
        public void DeclarationAsLoneBody_IsRejectedLikeCSharp(string source)
        {
            Assert.That(SingleError(source).Message, Does.Contain("llaves"));
        }

        [TestCase("using System;")]
        [TestCase("class A { }")]
        [TestCase("void M() { }")]
        [TestCase("int M() { return 1; }")]
        [TestCase("return;")]
        [TestCase("goto x;")]
        [TestCase("unsafe { }")]
        [TestCase("typeof(int);")]
        [TestCase("int a = 1;\nChargeCore(ref a);")]
        [TestCase("ChargeCore(value: 1);")]
        public void SyntaxBeyondTheSubset_IsRejected(string source)
        {
            Assert.That(Compile(source).Succeeded, Is.False);
        }

        [TestCase("}", "Sobra }")]
        [TestCase("for (int i = 0; i < 4; i++)\n  MoveRight();\n}", "Sobra }")]
        [TestCase("else { }", "if")]
        [TestCase("if (HasKey()) { } else", "final del código")]
        [TestCase("MoveRight(); @", "@")]
        [TestCase("void M() { }", "métodos propios")]
        [TestCase("int M() { return 1; }", "métodos propios")]
        public void BeginnerMistakes_GetAClearMessage(string source, string expected)
        {
            Assert.That(SingleError(source).Message, Does.Contain(expected));
        }

        [TestCase("int a 3;")]
        [TestCase("MoveRight);")]
        [TestCase("a")]
        public void IncompleteInstructions_DoNotBlameAMisleadingSymbol(string source)
        {
            Assert.That(SingleError(source).Message, Does.Contain("incompleta"));
        }

        [Test]
        public void LoopVariable_IsOutOfScopeAfterTheLoop()
        {
            Assert.That(SingleError("for (int i = 0; i < 1; i++) { }\nChargeCore(i);").Message, Does.Contain("i"));
        }

        [Test]
        public void SiblingBlocks_MayReuseAName()
        {
            Assert.That(Calls(Run("if (HasKey()) { int a = 1; } else { int a = 2; ChargeCore(a); }")),
                Is.EqualTo(new[] { "1:HasKey()=False", "1:ChargeCore(2)" }));
        }

        [Test]
        public void HugeEmptyLoop_StopsAtTheStepLimit()
        {
            var result = Run("for (int i = 0; i < 2000000000; i++) { }");
            Assert.That(result.Status, Is.EqualTo(ExecutionStatus.StepLimitExceeded));
        }

        [Test]
        public void HugeLoopWithCalls_StopsAtTheStepLimitAndKeepsItsTrace()
        {
            var result = Run("for (int i = 0; i < 2000000000; i++) { MoveRight(); }");
            Assert.That(result.Status, Is.EqualTo(ExecutionStatus.StepLimitExceeded));
            Assert.That(result.Trace.Count, Is.GreaterThan(0).And.LessThanOrEqualTo(ProgramInterpreter.DefaultMaxSteps));
        }

        [Test]
        public void EmptyProgram_CompilesAndDoesNothing()
        {
            Assert.That(Calls(Run("  \n// nothing\n")), Is.Empty);
        }
    }
}
