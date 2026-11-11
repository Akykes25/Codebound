using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Codebound.Levels
{
    /// <summary>How an action of the game is shown to the player.</summary>
    [Serializable]
    public sealed class ActionEntry
    {
        [Tooltip("Nombre exacto del método en GameApi.")]
        [SerializeField] private string method = string.Empty;
        [Tooltip("Cómo se escribe, por ejemplo ChargeCore(int carga).")]
        [SerializeField] private string usage = string.Empty;
        [SerializeField, TextArea(2, 4)] private string description = string.Empty;

        public ActionEntry()
        {
        }

        public ActionEntry(string method, string usage, string description)
        {
            this.method = method;
            this.usage = usage;
            this.description = description;
        }

        public string Method => method;
        public string Usage => usage;
        public string Description => description;
    }

    /// <summary>
    /// The texts that explain each game action. The signatures themselves live in GameApi,
    /// so the code the player writes is checked against one source only.
    /// </summary>
    [CreateAssetMenu(fileName = "ActionCatalog", menuName = "Codebound/Catálogo de acciones")]
    public sealed class ActionCatalog : ScriptableObject
    {
        [SerializeField] private List<ActionEntry> actions = new List<ActionEntry>();

        public IReadOnlyList<ActionEntry> Actions => actions;

        /// <summary>The entries a level allows, in catalog order.</summary>
        public IReadOnlyList<ActionEntry> For(LevelDefinition level)
        {
            return actions.Where(action => level.AvailableActions.Contains(action.Method)).ToList();
        }
    }
}
