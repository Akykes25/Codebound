using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Codebound.Gameplay
{
    /// <summary>Shows the time of each level, one line per level.</summary>
    public sealed class LevelTimesList : MonoBehaviour
    {
        [SerializeField] private Text list;

        public void Show(IEnumerable<LevelTimeRow> rows)
        {
            if (list != null)
            {
                list.text = string.Join("\n", rows.Select(row => row.ToString()));
            }
        }
    }
}
