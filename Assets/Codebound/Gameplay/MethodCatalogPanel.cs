using System.Collections.Generic;
using Codebound.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Codebound.Gameplay
{
    /// <summary>
    /// Shows the methods the level allows. It is read-only on purpose: the player still types the code.
    /// Connect a "Métodos" button to <see cref="Toggle"/> to open and close it.
    /// </summary>
    public sealed class MethodCatalogPanel : MonoBehaviour
    {
        [SerializeField] private Text title;
        [SerializeField] private Text list;

        public void Show(IEnumerable<ActionEntry> entries)
        {
            if (title != null)
            {
                title.text = MethodCatalogText.Title;
            }

            if (list != null)
            {
                list.text = MethodCatalogText.Format(entries);
            }
        }

        public void Toggle()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }
}
