using System.Collections.Generic;
using Codebound.Language;

namespace Codebound.Simulation
{
    /// <summary>The methods a player's program can call.</summary>
    public static class GameApi
    {
        public const string MoveRight = "MoveRight";
        public const string MoveLeft = "MoveLeft";
        public const string MoveUp = "MoveUp";
        public const string MoveDown = "MoveDown";
        public const string Interact = "Interact";
        public const string ChargeCore = "ChargeCore";
        public const string HasKey = "HasKey";
        public const string CollectKey = "CollectKey";
        public const string OpenGate = "OpenGate";

        public static IReadOnlyList<MethodSignature> Catalog { get; } = new[]
        {
            new MethodSignature(MoveRight, ValueKind.Void),
            new MethodSignature(MoveLeft, ValueKind.Void),
            new MethodSignature(MoveUp, ValueKind.Void),
            new MethodSignature(MoveDown, ValueKind.Void),
            new MethodSignature(Interact, ValueKind.Void),
            new MethodSignature(ChargeCore, ValueKind.Void, ValueKind.Int),
            new MethodSignature(HasKey, ValueKind.Bool),
            new MethodSignature(CollectKey, ValueKind.Void),
            new MethodSignature(OpenGate, ValueKind.Void),
        };
    }
}
