using UnityEngine;
using UnityEngine.UI;

namespace Codebound.Gameplay
{
    /// <summary>Shows the running time of the level. It hides itself in levels without a timer.</summary>
    public sealed class LevelTimerLabel : MonoBehaviour
    {
        [SerializeField] private Text label;

        private LevelSession session;

        public void Show(LevelSession levelSession)
        {
            session = levelSession;
            gameObject.SetActive(session.Timer != null);
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (session?.Timer != null && label != null)
            {
                label.text = session.ElapsedText;
            }
        }
    }
}
