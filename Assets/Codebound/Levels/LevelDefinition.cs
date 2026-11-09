using System.Collections.Generic;
using System.Linq;
using Codebound.Language;
using Codebound.Simulation;
using UnityEngine;

namespace Codebound.Levels
{
    /// <summary>
    /// Everything a level needs: its texts, its grid, its goal and the actions it allows.
    /// The grid uses the same coordinates as the simulation: x grows right and y grows down.
    /// </summary>
    [CreateAssetMenu(fileName = "Level", menuName = "Codebound/Nivel")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [Header("Presentación")]
        [SerializeField, Min(1)] private int number = 1;
        [SerializeField] private string title = string.Empty;
        [SerializeField, TextArea(2, 4)] private string objective = string.Empty;
        [SerializeField] private string concept = string.Empty;
        [SerializeField, TextArea(4, 12)] private string starterCode = string.Empty;
        [SerializeField] private bool isBoss;

        [Header("Grilla")]
        [SerializeField] private Vector2Int size = new Vector2Int(6, 3);
        [SerializeField] private Vector2Int start;
        [SerializeField] private List<Vector2Int> walls = new List<Vector2Int>();
        [SerializeField] private List<Vector2Int> modules = new List<Vector2Int>();
        [SerializeField] private List<Vector2Int> gates = new List<Vector2Int>();
        [SerializeField] private bool hasTerminal;
        [SerializeField] private Vector2Int terminal;
        [SerializeField] private bool hasCore;
        [SerializeField] private Vector2Int core;
        [SerializeField] private bool hasPowerDoor;
        [SerializeField] private Vector2Int powerDoor;
        [SerializeField, Min(0)] private int requiredCharge;
        [SerializeField] private bool hasKey;
        [SerializeField] private Vector2Int key;

        [Header("Objetivo")]
        [SerializeField] private bool requireTerminalActive;
        [SerializeField] private bool requireAllModules;
        [Tooltip("El núcleo tiene que terminar con la carga que pide la puerta.")]
        [SerializeField] private bool requireCoreCharge;
        [SerializeField] private bool requirePowerDoorOpen;
        [SerializeField] private bool requireAllGatesOpen;
        [SerializeField] private bool requireFinalPosition;
        [SerializeField] private Vector2Int finalPosition;
        [SerializeField] private ProgramConcept requiredConcepts;

        [Header("Acciones")]
        [Tooltip("Los métodos de GameApi que el jugador puede usar en este nivel.")]
        [SerializeField] private List<string> availableActions = new List<string>();

        public int Number => number;
        public string Title => title;
        public string Objective => objective;
        public string Concept => concept;
        public string StarterCode => starterCode;
        public bool IsBoss => isBoss;
        public IReadOnlyList<string> AvailableActions => availableActions;

        public LevelLayout CreateLayout()
        {
            return new LevelLayout
            {
                Width = size.x,
                Height = size.y,
                Start = ToCell(start),
                Walls = walls.Select(ToCell).ToArray(),
                Modules = modules.Select(ToCell).ToArray(),
                Gates = gates.Select(ToCell).ToArray(),
                Terminal = hasTerminal ? ToCell(terminal) : (GridPosition?)null,
                Core = hasCore ? ToCell(core) : (GridPosition?)null,
                PowerDoor = hasPowerDoor ? ToCell(powerDoor) : (GridPosition?)null,
                RequiredCharge = requiredCharge,
                Key = hasKey ? ToCell(key) : (GridPosition?)null,
            };
        }

        public LevelGoal CreateGoal()
        {
            return new LevelGoal
            {
                TerminalActive = requireTerminalActive,
                AllModulesActive = requireAllModules,
                CoreCharge = requireCoreCharge ? requiredCharge : (int?)null,
                PowerDoorOpen = requirePowerDoorOpen,
                AllGatesOpen = requireAllGatesOpen,
                FinalPosition = requireFinalPosition ? ToCell(finalPosition) : (GridPosition?)null,
                RequiredConcepts = requiredConcepts,
            };
        }

        /// <summary>Runs the player's code on a fresh copy of this level, with only its actions allowed.</summary>
        public LevelRun Run(string source)
        {
            return LevelRunner.Run(source, CreateLayout(), availableActions);
        }

        public LevelVerdict Evaluate(LevelRun run)
        {
            return LevelValidator.Evaluate(run, CreateLayout(), CreateGoal());
        }

        private static GridPosition ToCell(Vector2Int cell)
        {
            return new GridPosition(cell.x, cell.y);
        }
    }
}
