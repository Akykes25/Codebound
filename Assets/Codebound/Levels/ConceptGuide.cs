using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Codebound.Levels
{
    /// <summary>One programming concept as the "Conceptos" tab explains it.</summary>
    [Serializable]
    public sealed class ConceptEntry
    {
        [SerializeField] private string title = string.Empty;
        [Tooltip("Número del nivel donde el jugador conoce este concepto.")]
        [SerializeField, Min(1)] private int introducedIn = 1;
        [SerializeField, TextArea(2, 6)] private string explanation = string.Empty;
        [Tooltip("Código corto que muestra el concepto. Tiene que compilar en su nivel y no resolverlo.")]
        [SerializeField, TextArea(2, 8)] private string example = string.Empty;

        public ConceptEntry()
        {
        }

        public ConceptEntry(string title, int introducedIn, string explanation, string example)
        {
            this.title = title;
            this.introducedIn = introducedIn;
            this.explanation = explanation;
            this.example = example;
        }

        public string Title => title;
        public int IntroducedIn => introducedIn;
        public string Explanation => explanation;
        public string Example => example;
    }

    /// <summary>
    /// The concepts of the slice, in the order the player learns them.
    /// A level shows the concepts introduced up to it, so the guide grows as the player advances.
    /// </summary>
    [CreateAssetMenu(fileName = "ConceptGuide", menuName = "Codebound/Guía de conceptos")]
    public sealed class ConceptGuide : ScriptableObject
    {
        [SerializeField] private List<ConceptEntry> concepts = new List<ConceptEntry>();

        public IReadOnlyList<ConceptEntry> Concepts => concepts;

        /// <summary>The concepts the player already knows when playing this level, in guide order.</summary>
        public IReadOnlyList<ConceptEntry> For(LevelDefinition level)
        {
            return LearnedBy(concepts, level.Number);
        }

        public static IReadOnlyList<ConceptEntry> LearnedBy(IEnumerable<ConceptEntry> entries, int levelNumber)
        {
            return entries.Where(entry => entry.IntroducedIn <= levelNumber).ToList();
        }
    }
}
