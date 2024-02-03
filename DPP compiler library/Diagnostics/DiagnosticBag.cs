using System.Collections;
using DPP_Compiler.Symbols;
using DPP_Compiler.Text;

namespace DPP_Compiler.Diagnostics
{
    internal sealed class DiagnosticBag : IEnumerable<Diagnostic>
    {
        private List<Diagnostic> _diagnostics = new List<Diagnostic>();

        public IEnumerator<Diagnostic> GetEnumerator() => _diagnostics.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void AddRange(IEnumerable<Diagnostic> diagnostics) => _diagnostics.AddRange(diagnostics);

        private void Report(TextSpan span, string message)
        {
            Diagnostic diagnostic = new Diagnostic(span, message);
            _diagnostics.Add(diagnostic);
        }

        public void ReportStrayCharacter(int position, char character)
        {
            string message = $"Stray \'{character}\' in input";
            TextSpan span = new TextSpan(position, 1);
            Report(span, message);
        }

        public void ReportInvalidNumber(TextSpan span, string text, TypeSymbol type)
        {
            string message = $"The number \"{text}\" can not be represented by <{type}>";
            Report(span, message);
        }

        public void ReportUnterminatedString(TextSpan span)
        {
            string message = $"Unterminated string literal";
            Report(span, message);
        }

        public void ReportUnexpectedToken(TextSpan span, SyntaxKind kind, SyntaxKind expectedKind)
        {
            string message = $"Unexpected token <{kind}>, expected <{expectedKind}>";
            Report(span, message);
        }

        public void ReportUndefinedUnaryOperator(TextSpan span, string operatorText, TypeSymbol operandType)
        {
            string message = $"Unary operator '{operatorText}' is not defined for type {operandType}";
            Report(span, message);
        }

        public void ReportUndefinedBinaryOperator(TextSpan span, string operatorText, TypeSymbol leftType, TypeSymbol rightType)
        {
            string message = $"Binary operator '{operatorText}' is not defined for types {leftType} and {rightType}";
            Report(span, message);
        }

        public void ReportUndefinedName(TextSpan span, string name)
        {
            string message = $"Variable \"{name}\" doesn't exist";
            Report(span, message);
        }

        public void ReportCannotConvert(TextSpan span, TypeSymbol from, TypeSymbol to)
        {
            string message = $"Can not convert type {from} to type {to}";
            Report(span, message);
        }

        public void ReportCannotConvertImplicitly(TextSpan span, TypeSymbol from, TypeSymbol to)
        {
            string message = $"Can not implicitly convert type {from} to type {to}. An explicit conversion exists (are you missing a cast?)";
            Report(span, message);
        }

        public void ReportVariableAlreadyDeclared(TextSpan span, string name)
        {
            string message = $"Variable \"{name}\" is already declared";
            Report(span, message);
        }

        public void ReportUndefinedFunction(TextSpan span, string text)
        {
            string message = $"Function \"{text}\" doesn't exist";
            Report(span, message);
        }

        public void ReportUndefinedType(TextSpan span, string text)
        {
            string message = $"Type {text} doesn't exist";
            Report(span, message);
        }

        public void ReportWrongArgumentCount(TextSpan span, string name, int expectedCount, int actualCount)
        {
            string message = $"Function {name} requires {expectedCount} arguments, but {actualCount} were given";
            Report(span, message);
        }

        public void ReportWrongArgumentType(TextSpan span, string name, string parameterName, TypeSymbol expectedType, TypeSymbol actualType)
        {
            string message = $"Parameter \"{parameterName}\" of function \"{name}\" is of type {expectedType}, but was given a value of type {actualType}";
            Report(span, message);
        }


        public void ReportExpressionMustHaveValue(TextSpan span)
        {
            string message = $"Expression must have a value";
            Report(span, message);
        }

        public void ReportParameterAlreadyDeclared(TextSpan span, string name)
        {
            string message = $"Parameter \"{name}\" is already declared";
            Report(span, message);
        }

        public void ReportFunctionAlreadyDeclared(TextSpan span, string name)
        {
            string message = $"Function \"{name}\" is already declared ";
            Report(span, message);
        }

        public void ReportInvalidBreakOrContinue(TextSpan span, string text)
        {
            string message = $"No enclosing loop of which to {text}";
            Report(span, message);
        }

        internal void ReportReturnOutsideFunction(TextSpan span)
        {
            string message = "Return statements can't be used outside of functions";
            Report(span, message);
        }

        public void ReportInvalidReturnExpression(TextSpan span, string functionName)
        {
            string message = $"Function \"{functionName}\" does not return a value, return can not be followed by an expression";
            Report(span, message);
        }

        public void ReportMissingReturnExpression(TextSpan span, string functionName, TypeSymbol typeSymbol)
        {
            string message = $"Function \"{functionName}\" must return a value of type {typeSymbol}, but no expression was given after return";
            Report(span, message);
        }
    }
}
