using Blaze.Text;

namespace Blaze.Diagnostics
{
    public sealed class Diagnostic
    {
        public TextLocation Location { get; private set; }
        public string Message { get; private set; }

        public Diagnostic(TextLocation location, string message)
        {
            Location = location;
            Message = message;
        }

        public override string ToString() => Message;
    }
}
