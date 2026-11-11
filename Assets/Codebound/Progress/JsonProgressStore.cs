using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Codebound.Progress
{
    /// <summary>
    /// Reads and writes the progress as JSON. Saving writes a temporary file first and then
    /// replaces the real one, so a crash mid-save never leaves a half-written file.
    /// A missing, corrupt or unknown file loads as empty progress instead of throwing.
    /// </summary>
    public sealed class JsonProgressStore
    {
        public const string FileName = "progress.json";

        private readonly string path;

        public JsonProgressStore(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("The save path cannot be empty.", nameof(path));
            }

            this.path = path;
        }

        public string Path => path;

        /// <summary>The store the game uses: <c>persistentDataPath/progress.json</c>.</summary>
        public static JsonProgressStore CreateDefault()
        {
            return new JsonProgressStore(System.IO.Path.Combine(Application.persistentDataPath, FileName));
        }

        public SaveData Load()
        {
            if (!File.Exists(path))
            {
                return new SaveData();
            }

            try
            {
                var file = JsonUtility.FromJson<SaveFile>(File.ReadAllText(path));
                return FromFile(file);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is IOException ||
                                              exception is UnauthorizedAccessException)
            {
                return StartOver(exception.Message);
            }
        }

        public void Save(SaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var folder = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var file = new SaveFile { version = SaveData.CurrentVersion, levels = data.Levels.ToList() };
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(file, true));

            if (File.Exists(path))
            {
                File.Replace(temporary, path, null);
            }
            else
            {
                File.Move(temporary, path);
            }
        }

        private SaveData FromFile(SaveFile file)
        {
            if (file == null)
            {
                return StartOver("the file is empty");
            }

            // Version 0 means the file was written before the format had a version.
            // The fields have not changed since, so older files load as they are.
            if (file.version < 0 || file.version > SaveData.CurrentVersion)
            {
                return StartOver("unknown version " + file.version);
            }

            var levels = (file.levels ?? new List<LevelProgress>())
                .Where(level => level != null)
                .GroupBy(level => level.LevelNumber)
                .Select(group => group.First());
            return new SaveData(levels);
        }

        private SaveData StartOver(string reason)
        {
            Debug.LogWarning($"Codebound: could not read the progress at '{path}' ({reason}). Starting over.");
            return new SaveData();
        }

        /// <summary>The shape of the file on disk. It has no initializers so a missing field reads as 0.</summary>
        [Serializable]
        private sealed class SaveFile
        {
            public int version;
            public List<LevelProgress> levels;
        }
    }
}
