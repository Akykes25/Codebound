using UnityEngine;
using UnityEngine.UI;

namespace Codebound.Gameplay
{
    /// <summary>
    /// The panel shown when a level is completed: the final time, the best time, the "Asistido" mark
    /// next to the time, and why the solution works.
    /// </summary>
    public sealed class LevelResultPanel : MonoBehaviour
    {
        [SerializeField] private Text timeText;
        [SerializeField] private Text bestTimeText;
        [SerializeField] private Text debriefTitle;
        [SerializeField] private Text debriefText;

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Show(LevelAttempt attempt)
        {
            var result = attempt.Result;
            gameObject.SetActive(result != null);
            if (result == null)
            {
                return;
            }

            SetText(timeText, result.TimeLine);
            SetText(bestTimeText, result.BestTimeLine);
            SetText(debriefTitle, attempt.Debrief?.Title);
            SetText(debriefText, DebriefBody(attempt));
        }

        private static string DebriefBody(LevelAttempt attempt)
        {
            var debrief = attempt.Debrief;
            if (debrief == null)
            {
                return string.Empty;
            }

            return debrief.ReferenceSolution == null
                ? debrief.Explanation
                : $"{debrief.ReferenceSolution}\n\n{debrief.Explanation}";
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }
    }
}
