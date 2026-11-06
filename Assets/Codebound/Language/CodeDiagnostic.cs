namespace Codebound.Language
{
    /// <summary>A problem found in the player's code, located by 1-based line and column.</summary>
    public sealed class CodeDiagnostic
    {
        public CodeDiagnostic(int line, int column, string message)
        {
            Line = line;
            Column = column;
            Message = message;
        }

        public int Line { get; }
        public int Column { get; }
        public string Message { get; }

        public override string ToString()
        {
            return $"Línea {Line}, columna {Column}: {Message}";
        }
    }
}
