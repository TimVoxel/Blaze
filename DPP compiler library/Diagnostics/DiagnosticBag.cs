using System.Collections;
using DPP_Compiler.Text;

namespace DPP_Compiler.Diagnostics
{
    internal sealed class DiagnosticBag : IEnumerable<Diagnostic>
    {
        private List<Diagnostic> _diagnostics = new List<Diagnostic>();

        public IEnumerator<Diagnostic> GetEnumerator() => _diagnostics.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void Report(TextSpan span, string message)
        {
            Diagnostic diagnostic = new Diagnostic(span, message);
            _diagnostics.Add(diagnostic);
        }

        public void ReportInvalidNumber(TextSpan span, string text, Type type)
        {
            string message = $"The number \"{text}\" can not be represented by <{type}>";
            Report(span, message);
        }

        public void ReportStrayCharacter(int position, char character)
        {
            string message = $"Stray \'{character}\' in input\"";
            TextSpan span = new TextSpan(position, 1);
            Report(span, message);
        }

        public void ReportUnexpectedToken(TextSpan span, SyntaxKind kind, SyntaxKind expectedKind)
        {
            string message = $"Unexpected token <{kind}>, expected <{expectedKind}>";
            Report(span, message);
        }

        public void ReportUndefinedUnaryOperator(TextSpan span, string operatorText, Type operandType)
        {
            string message = $"Unary operator '{operatorText}' is not defined for type {operandType}";
            Report(span, message);
        }

        public void ReportUndefinedBinaryOperator(TextSpan span, string operatorText, Type leftType, Type rightType)
        {
            string message = $"Binary operator '{operatorText}' is not defined for types {leftType} and {rightType}";
            Report(span, message);
        }

        public void ReportUndefinedName(TextSpan span, string name)
        {
            string message = $"Variable \"{name}\" doesn't exist";
            Report(span, message);
        }

        public void ReportCannotConvert(TextSpan span, Type from, Type to)
        {
            string message = $"Can not convert type {from} to type {to}";
            Report(span, message);
        }

        public void AddRange(DiagnosticBag diagnostics) => _diagnostics.AddRange(diagnostics);

        public void ReportVariableAlreadyDeclared(TextSpan span, string name)
        {
            string message = $"Variable {name} is already declared";
            Report(span, message);
        }
    }
}
