using System.IO;
using Codebound.Progress;
using NUnit.Framework;

namespace Codebound.Tests.EditMode
{
    public class ProgressStoreTests
    {
        private string folder;
        private string path;

        [SetUp]
        public void CreateFolder()
        {
            folder = Path.Combine(Path.GetTempPath(), "codebound-progress-" + Path.GetRandomFileName());
            path = Path.Combine(folder, "progress.json");
        }

        [TearDown]
        public void DeleteFolder()
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }

        [Test]
        public void RoundTripKeepsEveryLevelField()
        {
            var store = new JsonProgressStore(path);
            var data = new SaveData();
            data.RecordAttempt(1, "MoveRight();");
            data.RecordCompletion(1, 42.5f, true, "MoveRight();\nInteract();");
            data.RecordAttempt(2, "MoveDown();");

            store.Save(data);
            var loaded = store.Load();

            Assert.That(loaded.Version, Is.EqualTo(SaveData.CurrentVersion));
            var first = loaded.Find(1);
            Assert.That(first.Completed, Is.True);
            Assert.That(first.HasBestTime, Is.True);
            Assert.That(first.BestTimeSeconds, Is.EqualTo(42.5f));
            Assert.That(first.BestTimeAssisted, Is.True);
            Assert.That(first.LastCode, Is.EqualTo("MoveRight();\nInteract();"));

            var second = loaded.Find(2);
            Assert.That(second.Completed, Is.False);
            Assert.That(second.HasBestTime, Is.False);
            Assert.That(second.LastCode, Is.EqualTo("MoveDown();"));

            Assert.That(loaded.Find(3), Is.Null);
        }

        [Test]
        public void MissingFileStartsEmpty()
        {
            var loaded = new JsonProgressStore(path).Load();

            Assert.That(loaded.Version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(loaded.Levels, Is.Empty);
        }

        [TestCase("{ this is not json")]
        [TestCase("")]
        [TestCase("null")]
        [TestCase("[1, 2, 3]")]
        public void CorruptFileStartsEmptyWithoutThrowing(string content)
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(path, content);

            SaveData loaded = null;
            Assert.DoesNotThrow(() => loaded = new JsonProgressStore(path).Load());

            Assert.That(loaded.Version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(loaded.Levels, Is.Empty);
        }

        [Test]
        public void FileWithoutVersionIsUpgradedAndKeepsItsLevels()
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(path,
                "{\"levels\":[{\"levelNumber\":1,\"completed\":true,\"hasBestTime\":true," +
                "\"bestTimeSeconds\":30.0,\"bestTimeAssisted\":false,\"lastCode\":\"MoveRight();\"}]}");

            var loaded = new JsonProgressStore(path).Load();

            Assert.That(loaded.Version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(loaded.Find(1).Completed, Is.True);
            Assert.That(loaded.Find(1).BestTimeSeconds, Is.EqualTo(30f));
            Assert.That(loaded.Find(1).LastCode, Is.EqualTo("MoveRight();"));
        }

        [Test]
        public void FileFromNewerVersionStartsEmpty()
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(path, "{\"version\":" + (SaveData.CurrentVersion + 1) + ",\"levels\":[]}");

            var loaded = new JsonProgressStore(path).Load();

            Assert.That(loaded.Version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(loaded.Levels, Is.Empty);
        }

        [Test]
        public void SaveCreatesTheFolderAndLeavesNoTemporaryFile()
        {
            var store = new JsonProgressStore(path);

            store.Save(new SaveData());
            store.Save(new SaveData());

            Assert.That(File.Exists(path), Is.True);
            Assert.That(Directory.GetFiles(folder), Has.Length.EqualTo(1));
        }

        [Test]
        public void SaveReplacesThePreviousFile()
        {
            var store = new JsonProgressStore(path);
            var first = new SaveData();
            first.RecordAttempt(1, "old");
            store.Save(first);

            var second = new SaveData();
            second.RecordAttempt(1, "new");
            store.Save(second);

            Assert.That(store.Load().Find(1).LastCode, Is.EqualTo("new"));
        }

        [Test]
        public void FasterCompletionReplacesBestTimeAndItsAssistedMark()
        {
            var data = new SaveData();
            data.RecordCompletion(1, 50f, true, "a");
            data.RecordCompletion(1, 40f, false, "b");

            var level = data.Find(1);
            Assert.That(level.BestTimeSeconds, Is.EqualTo(40f));
            Assert.That(level.BestTimeAssisted, Is.False);
            Assert.That(level.LastCode, Is.EqualTo("b"));
        }

        [Test]
        public void SlowerCompletionKeepsBestTimeButUpdatesLastCode()
        {
            var data = new SaveData();
            data.RecordCompletion(1, 40f, false, "a");
            data.RecordCompletion(1, 60f, true, "b");

            var level = data.Find(1);
            Assert.That(level.BestTimeSeconds, Is.EqualTo(40f));
            Assert.That(level.BestTimeAssisted, Is.False);
            Assert.That(level.LastCode, Is.EqualTo("b"));
            Assert.That(level.Completed, Is.True);
        }

        [Test]
        public void AttemptAfterCompletionKeepsTheLevelCompleted()
        {
            var data = new SaveData();
            data.RecordCompletion(1, 40f, false, "a");
            data.RecordAttempt(1, "broken");

            Assert.That(data.Find(1).Completed, Is.True);
            Assert.That(data.Find(1).LastCode, Is.EqualTo("broken"));
            Assert.That(data.Levels, Has.Count.EqualTo(1));
        }
    }
}
