using System.Collections;
using System.Collections.Generic;
using Codebound.Levels;
using Codebound.Progress;
using UnityEngine;

namespace Codebound.Gameplay
{
    /// <summary>
    /// Plays one level: starts its session (and timer) when the screen opens, runs the player's code,
    /// saves the progress, and at the end shows the final time and the time of each level.
    /// Connect the editor's "Ejecutar" button to <see cref="RunCode"/>.
    /// </summary>
    public sealed class LevelScreen : MonoBehaviour
    {
        [SerializeField] private LevelDefinition level;
        [Tooltip("Todos los niveles, para la lista de tiempos.")]
        [SerializeField] private List<LevelDefinition> allLevels = new List<LevelDefinition>();
        [SerializeField] private PlayerActor player;
        [SerializeField] private LevelTimerLabel timerLabel;
        [SerializeField] private LevelResultPanel resultPanel;
        [SerializeField] private LevelTimesList timesList;
        [SerializeField] private ActionCatalog actionCatalog;
        [SerializeField] private MethodCatalogPanel catalogPanel;

        private JsonProgressStore store;
        private SaveData progress;

        public LevelSession Session { get; private set; }

        private void Start()
        {
            store = JsonProgressStore.CreateDefault();
            progress = store.Load();
            Session = new LevelSession(level, progress, () => Time.realtimeSinceStartupAsDouble);

            if (timerLabel != null)
            {
                timerLabel.Show(Session);
            }

            if (resultPanel != null)
            {
                resultPanel.Hide();
            }

            ShowTimes();

            if (catalogPanel != null && actionCatalog != null)
            {
                catalogPanel.Show(actionCatalog.For(level));
            }
        }

        public void RunCode(string source)
        {
            var attempt = Session.Submit(source);
            store.Save(progress);
            StartCoroutine(PlayThenShow(attempt));
        }

        private IEnumerator PlayThenShow(LevelAttempt attempt)
        {
            if (player != null)
            {
                player.PlaceAt(level.CreateLayout().Start);
                yield return player.Play(attempt.Run.Steps);
            }

            if (attempt.Result != null)
            {
                if (resultPanel != null)
                {
                    resultPanel.Show(attempt);
                }

                ShowTimes();
            }
        }

        private void ShowTimes()
        {
            if (timesList != null)
            {
                timesList.Show(LevelTimes.Rows(allLevels, progress));
            }
        }
    }
}
