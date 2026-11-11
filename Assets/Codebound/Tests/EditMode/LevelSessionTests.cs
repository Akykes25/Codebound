using System.Linq;
using Codebound.Gameplay;
using Codebound.Levels;
using Codebound.Progress;
using Codebound.Simulation;
using NUnit.Framework;

namespace Codebound.Tests.EditMode
{
    public class LevelSessionTests
    {
        private const string Solution = "MoveRight();\nMoveRight();\nInteract();";
        private const string Wrong = "MoveLeft();\nInteract();";

        /// <summary>The test level 1 as a playable level, so a session can run without loading assets.</summary>
        private sealed class FakeLevel : IPlayableLevel
        {
            private static readonly LevelGoal Goal = new LevelGoal { TerminalActive = true };

            public FakeLevel(int number, bool timed)
            {
                Number = number;
                IsTimed = timed;
            }

            public int Number { get; }
            public string Title => $"Prueba {Number}";
            public bool IsTimed { get; }

            public LevelRun Run(string source) => LevelRunner.Run(source, TestLevels.Level1);
            public LevelVerdict Evaluate(LevelRun run) => LevelValidator.Evaluate(run, TestLevels.Level1, Goal);
            public string Advise(string source, LevelRun run, LevelVerdict verdict) =>
                LevelAdvisor.Advise(source, run, verdict, Solution, "Contá las casillas.");
            public LevelDebrief Debrief(string source, LevelVerdict verdict) =>
                LevelDebrief.For(source, verdict, Solution, "Dos pasos y activar.");
            public LevelHelp CreateHelp() => new LevelHelp(new[] { "Pista" }, Solution, "Dos pasos y activar.");
        }

        private double now;
        private SaveData progress;

        private LevelSession Open(bool timed = true, int number = 3)
        {
            progress ??= new SaveData();
            return new LevelSession(new FakeLevel(number, timed), progress, () => now);
        }

        [SetUp]
        public void SetUp()
        {
            now = 1000;
            progress = null;
        }

        [Test]
        public void TheTimerStartsWhenTheLevelOpens()
        {
            var session = Open();
            now += 7;

            Assert.That(session.Timer.IsRunning, Is.True);
            Assert.That(session.ElapsedText, Is.EqualTo("00:07"));
        }

        [Test]
        public void ALevelWithoutTimerHasNoTimer()
        {
            var session = Open(timed: false);

            Assert.That(session.Timer, Is.Null);
            Assert.That(session.ElapsedText, Is.Null);
        }

        [Test]
        public void FailedRunsKeepTheTimerRunningAndHaveNoResult()
        {
            var session = Open();
            now += 10;

            var attempt = session.Submit(Wrong);
            now += 5;

            Assert.That(attempt.Result, Is.Null);
            Assert.That(attempt.Advice, Is.EqualTo("Contá las casillas."));
            Assert.That(session.Timer.IsRunning, Is.True);
            Assert.That(session.Timer.Elapsed, Is.EqualTo(15));
        }

        [Test]
        public void CompletingTheLevelStopsTheTimerAndShowsTheFinalTime()
        {
            var session = Open();
            now += 20;
            session.Submit(Wrong);
            now += 45;

            var attempt = session.Submit(Solution);
            now += 100;

            Assert.That(session.IsCompleted, Is.True);
            Assert.That(session.Timer.IsStopped, Is.True);
            Assert.That(attempt.Result.TimeSeconds, Is.EqualTo(65));
            Assert.That(attempt.Result.TimeText, Is.EqualTo("01:05"));
            Assert.That(attempt.Result.Assisted, Is.False);
            Assert.That(attempt.Result.IsNewBest, Is.True);
            Assert.That(attempt.Debrief.MatchesSolution, Is.True);
            Assert.That(session.ElapsedText, Is.EqualTo("01:05"));
        }

        [Test]
        public void TheFinalTimeIsSavedAsTheBestTimeOfTheLevel()
        {
            var session = Open();
            now += 30;
            session.Submit(Solution);

            var saved = progress.Find(3);
            Assert.That(saved.Completed, Is.True);
            Assert.That(saved.BestTimeSeconds, Is.EqualTo(30));
            Assert.That(saved.LastCode, Is.EqualTo(Solution));
        }

        [Test]
        public void ASlowerCompletionKeepsTheBestTime()
        {
            var first = Open();
            now += 30;
            first.Submit(Solution);

            var second = Open();
            now += 50;
            var attempt = second.Submit(Solution);

            Assert.That(attempt.Result.TimeText, Is.EqualTo("00:50"));
            Assert.That(attempt.Result.IsNewBest, Is.False);
            Assert.That(attempt.Result.BestTimeText, Is.EqualTo("00:30"));
            Assert.That(progress.Find(3).BestTimeSeconds, Is.EqualTo(30));
        }

        [Test]
        public void OpeningTheSolutionMarksTheTimeAsAssisted()
        {
            var session = Open();
            session.Help.OpenSolution();
            now += 12;

            var attempt = session.Submit(Solution);

            Assert.That(attempt.Result.Assisted, Is.True);
            Assert.That(progress.Find(3).BestTimeAssisted, Is.True);
        }

        [Test]
        public void ALevelWithoutTimerIsSavedAsCompletedWithoutATime()
        {
            var session = Open(timed: false, number: 1);
            now += 30;

            var attempt = session.Submit(Solution);

            Assert.That(attempt.Result.TimeSeconds, Is.Null);
            Assert.That(progress.Find(1).Completed, Is.True);
            Assert.That(progress.Find(1).HasBestTime, Is.False);
        }

        [Test]
        public void RunningAgainAfterCompletingDoesNotRecordASecondTime()
        {
            var session = Open();
            now += 30;
            session.Submit(Solution);
            now += 5;

            var again = session.Submit(Solution);

            Assert.That(again.Result, Is.Null);
            Assert.That(progress.Find(3).BestTimeSeconds, Is.EqualTo(30));
        }

        [Test]
        public void RetryingPutsTheTimerBackToZero()
        {
            var session = Open();
            now += 40;
            session.Submit(Wrong);

            session.Retry();
            now += 6;

            Assert.That(session.Timer.IsRunning, Is.True);
            Assert.That(session.ElapsedText, Is.EqualTo("00:06"));
        }

        [Test]
        public void TheFinalTimeOnlyCountsSinceTheLastRetry()
        {
            var session = Open();
            now += 40;
            session.Retry();
            now += 25;

            var attempt = session.Submit(Solution);

            Assert.That(attempt.Result.TimeSeconds, Is.EqualTo(25));
            Assert.That(progress.Find(3).BestTimeSeconds, Is.EqualTo(25));
        }

        [Test]
        public void RetryingAfterCompletingAllowsABetterTime()
        {
            var session = Open();
            now += 50;
            session.Submit(Solution);

            session.Retry();
            now += 20;
            var attempt = session.Submit(Solution);

            Assert.That(session.IsCompleted, Is.True);
            Assert.That(attempt.Result.TimeText, Is.EqualTo("00:20"));
            Assert.That(attempt.Result.IsNewBest, Is.True);
            Assert.That(progress.Find(3).BestTimeSeconds, Is.EqualTo(20));
        }

        [Test]
        public void RetryingKeepsTheAssistedMarkBecauseTheSolutionWasAlreadySeen()
        {
            var session = Open();
            session.Help.OpenSolution();

            session.Retry();
            now += 10;
            var attempt = session.Submit(Solution);

            Assert.That(attempt.Result.Assisted, Is.True);
        }

        [Test]
        public void RetryingALevelWithoutTimerOnlyAllowsCompletingItAgain()
        {
            var session = Open(timed: false, number: 1);
            session.Submit(Solution);

            session.Retry();

            Assert.That(session.Timer, Is.Null);
            Assert.That(session.IsCompleted, Is.False);
            Assert.That(session.Submit(Solution).Result, Is.Not.Null);
        }

        [Test]
        public void TheResultPanelShowsTheTimeWithTheAssistedMarkNextToIt()
        {
            var session = Open();
            session.Help.OpenSolution();
            now += 65;

            var result = session.Submit(Solution).Result;

            Assert.That(result.TimeLine, Is.EqualTo("Tiempo: 01:05 (Asistido)"));
            Assert.That(result.BestTimeLine, Is.EqualTo("¡Nuevo mejor tiempo!"));
        }

        [Test]
        public void TheListShowsTheTimeOfEachLevel()
        {
            var levels = new IPlayableLevel[] { new FakeLevel(3, true), new FakeLevel(1, false), new FakeLevel(2, false), new FakeLevel(4, true) };
            progress = new SaveData();
            progress.RecordCompletion(1, "x");
            progress.RecordCompletion(3, 65f, false, "x");
            progress.RecordCompletion(4, 9f, true, "x");

            var rows = LevelTimes.Rows(levels, progress);

            Assert.That(rows.Select(row => row.ToString()), Is.EqualTo(new[]
            {
                "Nivel 1 · Prueba 1: Completado",
                "Nivel 2 · Prueba 2: Sin completar",
                "Nivel 3 · Prueba 3: 01:05",
                "Nivel 4 · Prueba 4: 00:09 (Asistido)",
            }));
        }
    }
}
