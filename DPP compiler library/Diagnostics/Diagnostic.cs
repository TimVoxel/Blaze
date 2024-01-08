using DPP_Compiler.Text;

namespace DPP_Compiler.Diagnostics
{
    public sealed class Diagnostic
    {
        public TextSpan Span { get; private set; }
        public string Message { get; private set; }

        public Diagnostic(TextSpan span, string message)
        {
            Span = span;
            Message = message;
        }

        public override string ToString() => Message;
    }
}
