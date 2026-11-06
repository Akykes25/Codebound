using System.Collections.Generic;

namespace Codebound.Language
{
    public enum ValueKind
    {
        Void,
        Int,
        Bool,
    }

    /// <summary>A game method the player's code is allowed to call.</summary>
    public sealed class MethodSignature
    {
        public MethodSignature(string name, ValueKind returns, params ValueKind[] parameters)
        {
            Name = name;
            Returns = returns;
            Parameters = parameters;
        }

        public string Name { get; }
        public ValueKind Returns { get; }
        public IReadOnlyList<ValueKind> Parameters { get; }
    }
}
