using System.Linq;
using Codebound.Levels;
using NUnit.Framework;
using static Codebound.Tests.EditMode.LevelDataTests;

namespace Codebound.Tests.EditMode
{
    public class LevelTimerTests
    {
        private double now;

        private LevelTimer CreateTimer()
        {
            now = 100;
            return new LevelTimer(() => now);
        }

        [Test]
        public void ATimerThatWasNotStartedShowsZero()
        {
            var timer = CreateTimer();
            now += 10;

            Assert.That(timer.Elapsed, Is.EqualTo(0));
            Assert.That(timer.IsRunning, Is.False);
            Assert.That(timer.IsStopped, Is.False);
        }

        [Test]
        public void ARunningTimerCountsTheTimeSinceItStarted()
        {
            var timer = CreateTimer();

            timer.Start();
            now += 12.5;

            Assert.That(timer.IsRunning, Is.True);
            Assert.That(timer.Elapsed, Is.EqualTo(12.5));
        }

        [Test]
        public void AStoppedTimerKeepsItsFinalTimeAndCannotStartAgain()
        {
            var timer = CreateTimer();

            timer.Start();
            now += 42;
            timer.Stop();
            now += 30;
            timer.Start();
            now += 30;

            Assert.That(timer.IsStopped, Is.True);
            Assert.That(timer.IsRunning, Is.False);
            Assert.That(timer.Elapsed, Is.EqualTo(42));
        }

        [Test]
        public void RestartingStartsCountingFromZeroAgain()
        {
            var timer = CreateTimer();

            timer.Start();
            now += 20;
            timer.Stop();
            timer.Restart();
            now += 4;

            Assert.That(timer.IsRunning, Is.True);
            Assert.That(timer.IsStopped, Is.False);
            Assert.That(timer.Elapsed, Is.EqualTo(4));
        }

        [Test]
        public void StartingTwiceDoesNotResetTheTime()
        {
            var timer = CreateTimer();

            timer.Start();
            now += 5;
            timer.Start();
            now += 5;

            Assert.That(timer.Elapsed, Is.EqualTo(10));
        }

        [Test]
        public void TheTimeIsShownAsMinutesAndSeconds()
        {
            Assert.That(LevelTimer.Format(0), Is.EqualTo("00:00"));
            Assert.That(LevelTimer.Format(5.9), Is.EqualTo("00:05"));
            Assert.That(LevelTimer.Format(65), Is.EqualTo("01:05"));
            Assert.That(LevelTimer.Format(3725), Is.EqualTo("62:05"));
        }

        [Test]
        public void OnlyLevelsFromTheThirdOnAreTimed()
        {
            var timed = LoadLevels().Where(level => level.IsTimed).Select(level => level.Number);

            Assert.That(timed, Is.EqualTo(new[] { 3, 4, 5 }));
        }
    }
}
