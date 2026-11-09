using System;
using System.Collections.Generic;
using System.Linq;
using Codebound.Language;

namespace Codebound.Simulation
{
    /// <summary>Applies the game rules to each call a program makes.</summary>
    internal sealed class GridWorld : IProgramHost
    {
        private readonly LevelLayout layout;
        private readonly List<CallOutcome> outcomes = new List<CallOutcome>();

        public GridWorld(LevelLayout layout)
        {
            this.layout = layout;
            State = new WorldState(layout.Start);
        }

        public WorldState State { get; }
        public IReadOnlyList<CallOutcome> Outcomes => outcomes;

        public object Invoke(string method, IReadOnlyList<object> arguments)
        {
            var from = State.Position;
            var failure = Apply(method, arguments, out var result);
            outcomes.Add(new CallOutcome(from, State.Position, failure));
            return result;
        }

        /// <summary>Returns the reason the call failed, or null if it worked.</summary>
        private string Apply(string method, IReadOnlyList<object> arguments, out object result)
        {
            result = null;
            switch (method)
            {
                case GameApi.MoveRight:
                    return Move(1, 0);
                case GameApi.MoveLeft:
                    return Move(-1, 0);
                case GameApi.MoveUp:
                    return Move(0, -1);
                case GameApi.MoveDown:
                    return Move(0, 1);
                case GameApi.Interact:
                    return Interact();
                case GameApi.ChargeCore:
                    return ChargeCore((int)arguments[0]);
                case GameApi.HasKey:
                    result = State.HasKey;
                    return null;
                case GameApi.CollectKey:
                    return CollectKey();
                case GameApi.OpenGate:
                    return OpenGate();
                default:
                    throw new InvalidOperationException($"Method outside the catalog: {method}");
            }
        }

        private string Move(int deltaX, int deltaY)
        {
            var target = new GridPosition(State.Position.X + deltaX, State.Position.Y + deltaY);
            if (IsBlocked(target))
            {
                return "El camino está bloqueado.";
            }

            State.Position = target;
            if (layout.Modules.Contains(target))
            {
                State.ActivateModule(target);
            }

            return null;
        }

        private bool IsBlocked(GridPosition cell)
        {
            var outside = cell.X < 0 || cell.Y < 0 || cell.X >= layout.Width || cell.Y >= layout.Height;
            return outside
                || layout.Walls.Contains(cell)
                || cell.Equals(layout.Terminal)
                || cell.Equals(layout.Core)
                || (cell.Equals(layout.PowerDoor) && !State.PowerDoorOpen)
                || (cell.Equals(layout.Key) && !State.HasKey)
                || (layout.Gates.Contains(cell) && !State.OpenGates.Contains(cell));
        }

        private string Interact()
        {
            if (IsNextToRobot(layout.Terminal))
            {
                State.TerminalActive = true;
                return null;
            }

            if (!IsNextToRobot(layout.PowerDoor))
            {
                return "No hay nada para activar al lado del robot.";
            }

            if (State.CoreCharge != layout.RequiredCharge)
            {
                return $"La puerta necesita que el núcleo tenga carga {layout.RequiredCharge}.";
            }

            State.PowerDoorOpen = true;
            return null;
        }

        private string ChargeCore(int charge)
        {
            if (!IsNextToRobot(layout.Core))
            {
                return "No hay ningún núcleo al lado del robot.";
            }

            State.CoreCharge = charge;
            return null;
        }

        private string CollectKey()
        {
            if (State.HasKey || !IsNextToRobot(layout.Key))
            {
                return "No hay ninguna llave al lado del robot.";
            }

            State.HasKey = true;
            return null;
        }

        private string OpenGate()
        {
            var closedGates = layout.Gates.Where(gate => !State.OpenGates.Contains(gate) && gate.IsNextTo(State.Position)).ToList();
            if (closedGates.Count == 0)
            {
                return "No hay ninguna puerta cerrada al lado del robot.";
            }

            if (!State.HasKey)
            {
                return "Para abrir esta puerta hace falta la llave.";
            }

            State.OpenGate(closedGates[0]);
            return null;
        }

        private bool IsNextToRobot(GridPosition? cell)
        {
            return cell.HasValue && cell.Value.IsNextTo(State.Position);
        }
    }

    internal readonly struct CallOutcome
    {
        public CallOutcome(GridPosition from, GridPosition to, string failure)
        {
            From = from;
            To = to;
            Failure = failure;
        }

        public GridPosition From { get; }
        public GridPosition To { get; }
        public string Failure { get; }
    }
}
