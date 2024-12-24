using Blaze.Text;

namespace Blaze.Diagnostics
{
    public sealed class Diagnostic
    {
        public TextLocation Location { get; }
        public string Message { get; }
        public IDiagnosticsSource Source { get; }

        public Diagnostic(TextLocation location, string message, IDiagnosticsSource source)
        {
            Location = location;
            Message = message;
            Source = source;
        }

        public override string ToString() => Message;
    }
}
