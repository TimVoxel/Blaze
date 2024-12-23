using System.Collections;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Blaze.Binding;
using Blaze.Symbols;
using Blaze.Text;

namespace Blaze.Diagnostics
{
    internal sealed class DiagnosticBag : IEnumerable<Diagnostic>
    {
        private List<Diagnostic> _diagnostics = new List<Diagnostic>();

        public IEnumerator<Diagnostic> GetEnumerator() => _diagnostics.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void AddRange(IEnumerable<Diagnostic> diagnostics) => _diagnostics.AddRange(diagnostics);

        private void Report(TextLocation location, string message)
        {
            Diagnostic diagnostic = new Diagnostic(location, message);
            _diagnostics.Add(diagnostic);
        }

        public void ReportStrayCharacter(TextLocation location, char character)
        {
            string message = $"Stray \'{character}\' in input";
            Report(location, message);
        }

        public void ReportInvalidNumber(TextLocation location, string text, TypeSymbol type)
        {
            string message = $"The number \"{text}\" can not be represented by <{type}>";
            Report(location, message);
        }

        public void ReportUnterminatedString(TextLocation location)
        {
            string message = $"Unterminated string literal";
            Report(location, message);
        }

        public void ReportUnterminatedMultiLineComment(TextLocation location)
        {
            string message = $"Unterminated multiline comment";
            Report(location, message);
        }

        public void ReportUnexpectedToken(TextLocation location, SyntaxKind kind, SyntaxKind expectedKind)
        {
            string message = $"Unexpected token <{kind}>, expected <{expectedKind}>";
            Report(location, message);
        }

        public void ReportDuplicateFunctionModifier(TextLocation location)
        {
            string message = "Duplicate function modifiers are not allowed";
            Report(location, message);
        }

        public void ReportUndefinedUnaryOperator(TextLocation location, string operatorText, TypeSymbol operandType)
        {
            string message = $"Unary operator '{operatorText}' is not defined for type {operandType}";
            Report(location, message);
        }

        public void ReportUndefinedBinaryOperator(TextLocation location, string operatorText, TypeSymbol leftType, TypeSymbol rightType)
        {
            string message = $"Binary operator '{operatorText}' is not defined for types {leftType.Name} and {rightType.Name}";
            Report(location, message);
        }

        public void ReportUndefinedIncrementOperator(TextLocation location, string operatorText, TypeSymbol leftType)
        {
            string message = $"Operator '{operatorText}' can not be applied to operand of type {leftType}";
            Report(location, message);
        }

        public void ReportUndefinedName(TextLocation location, string name)
        {
            string message = $"The name \"{name}\" does not refer to a defined entity";
            Report(location, message);
        }

        public void ReportCannotConvert(TextLocation location, TypeSymbol from, TypeSymbol to)
        {
            string message = $"Can not convert type {from} to type {to}";
            Report(location, message);
        }

        public void ReportCannotConvertImplicitly(TextLocation location, TypeSymbol from, TypeSymbol to)
        {
            string message = $"Can not implicitly convert type {from} to type {to}. An explicit conversion exists (are you missing a cast?)";
            Report(location, message);
        }

        public void ReportVariableAlreadyDeclared(TextLocation location, string name)
        {
            string message = $"Variable \"{name}\" is already declared";
            Report(location, message);
        }

        public void ReportVariableNameIsADeclaredField(TextLocation location, string name)
        {
            string message = $"Name \"{name}\" is already taken by a field in the namespace";
            Report(location, message);
        }

        public void ReportUndefinedFunction(TextLocation location, string text)
        {
            string message = $"Function \"{text}\" doesn't exist";
            Report(location, message);
        }

        public void ReportUndefinedType(TextLocation location, string text)
        {
            string message = $"Type {text} doesn't exist";
            Report(location, message);
        }

        public void ReportUndefinedClass(TextLocation location, string text)
        {
            string message = $"Class {text} doesn't exist";
            Report(location, message);
        }

        public void ReportWrongArgumentCount(TextLocation location, string name, int expectedCount, int actualCount)
        {
            string message = $"Function {name} requires {expectedCount} arguments, but {actualCount} were given";
            Report(location, message);
        }

        public void ReportWrongConstructorArgumentCount(TextLocation location, string name, int expectedCount, int actualCount)
        {
            string message = $"Constructor of class {name} requires {expectedCount} arguments, but {actualCount} were given";
            Report(location, message);
        }

        public void ReportExpressionMustHaveValue(TextLocation location)
        {
            string message = $"Expression must have a value";
            Report(location, message);
        }

        public void ReportParameterAlreadyDeclared(TextLocation location, string name)
        {
            string message = $"Parameter \"{name}\" is already declared";
            Report(location, message);
        }

        public void ReportFunctionAlreadyDeclared(TextLocation location, string name)
        {
            string message = $"Function \"{name}\" is already declared ";
            Report(location, message);
        }

        public void ReportInvalidBreakOrContinue(TextLocation location, string text)
        {
            string message = $"No enclosing loop of which to {text}";
            Report(location, message);
        }

        internal void ReportReturnOutsideFunction(TextLocation location)
        {
            string message = "Return statements can't be used outside of functions";
            Report(location, message);
        }

        public void ReportInvalidReturnExpression(TextLocation location, string functionName)
        {
            string message = $"Function \"{functionName}\" does not return a value, return can not be followed by an expression";
            Report(location, message);
        }

        public void ReportMissingReturnExpression(TextLocation location, string functionName, TypeSymbol typeSymbol)
        {
            string message = $"Function \"{functionName}\" must return a value of type {typeSymbol}, but no expression was given after return";
            Report(location, message);
        }

        public void ReportAllPathsMustReturn(TextLocation location)
        {
            string message = $"Not all code paths return a value";
            Report(location, message);
        }

        public void ReportInvalidExpressionStatement(TextLocation location)
        {
            string message = "Only assignment, increment, decrement and call expressions can be used as a statement";
            Report(location, message);
        }

        public void ReportNamespaceAlreadyDeclared(TextLocation location, string name)
        {
            string message = $"Namespace \"{name}\" is already declared ";
            Report(location, message);
        }

        public void ReportUpperCaseInFunctionName(TextLocation location, string name)
        {
            string message = $"Function name \"{name}\" contains upper case letters, which are illegal in function names";
            Report(location, message);
        }
        public void ReportUpperCaseInNamespaceName(TextLocation location, string name)
        {
            string message = $"Namespace name \"{name}\" contains upper case letters, which are illegal in namespace names";
            Report(location, message);
        }

        public void ReportInvalidMemberAccessExpressionKind(TextLocation location)
        {
            string message = "Invalid member access expression, only function calls or other member access expressions are allowed";
            Report(location, message);
        }

        public void ReportUndefinedNamespace(TextLocation location, string name)
        {
            string message = $"Namespace \"{name}\" doesn't exist";
            Report(location, message);
        }

        public void ReportUsingNotInTheBeginningOfTheFile(TextLocation location)
        {
            string message = $"Usings must precede namespace declarations";
            Report(location, message);
        }

        public void ReportInvalidLeftHandAssignmentExpression(TextLocation location, SyntaxKind actualKind)
        {
            string message = $"Expression of kind {actualKind} cannot be assigned to";
            Report(location, message);
        }

        public void ReportInvalidIncrementExpression(TextLocation location, SyntaxKind actualKind)
        {
            string message = $"Expression of kind {actualKind} cannot be incremented/decremented";
            Report(location, message);
        }

        public void ReportInvalidMemberAccessLeftKind(TextLocation location, SyntaxKind actualKind)
        {
            string message = $"Expression of kind {actualKind} does not have any members, and thus cannot be accessed";
            Report(location, message);
        }

        public void ReportInvalidMemberAccess(TextLocation location, string name)
        {
            string message = $"Expression of type {name} cannot be accessed";
            Report(location, message);
        }

        public void ReportUndefinedMemberOfType(TextLocation location, string typeName, string memberName)
        {
            string message = $"Named type {typeName} has no member named {memberName}";
            Report(location, message);
        }

        public void ReportUndefinedMemberOfNamespace(TextLocation location, string namespaceName, string memberName)
        {
            string message = $"Namespace {namespaceName} has no member named {memberName}";
            Report(location, message);
        }

        public void ReportInvalidCallIdentifier(TextLocation location, BoundNodeKind kind)
        {
            string message = $"Expression of kind {kind} cannot be used as an identifier for a call";
            Report(location, message);
        }

        public void ReportInvalidObjectCreationIdentifier(TextLocation location, BoundNodeKind kind)
        {
            string message = $"Expression of kind {kind} cannot be used as an identifier for a type";
            Report(location, message);
        }

        public void ReportReturningNamedType(TextLocation location)
        {
            string message = "Returning objects of named types is unsupported";
            Report(location, message);
        }

        public void ReportGlobalStatement(TextLocation location)
        {
            string message = "Statement outside of a function";
            Report(location, message);
        }

        public void ReportFieldAlreadyDeclared(TextLocation location, string identifierText)
        {
            string message = $"Field {identifierText} is already declared";
            Report(location, message);
        }

        public void ReportEnumAlreadyDeclared(TextLocation location, string identifierText)
        {
            string message = $"Enum {identifierText} is already declared";
            Report(location, message);
        }

        public void ReportEnumMemberAlreadyDeclared(TextLocation location, string memberName, string identifierText)
        {
            string message = $"Enum {identifierText} already has a member named \"{memberName}\"";
            Report(location, message);
        }

        public void ReportInvalidFieldInitializer(TextLocation location)
        {
            string message = $"Field initializer should have a constant value";
            Report(location, message);
        }

        public void ReportSecondLoadFunction(TextLocation location, string namespaceName)
        {
            string message = $"Namespace {namespaceName} already has a load function";
            Report(location, message);
        }

        public void ReportSecondTickFunction(TextLocation location, string namespaceName)
        {
            string message = $"Namespace {namespaceName} already has a tick function";
            Report(location, message);
        }

        public void ReportLoadFunctionWithParameters(TextLocation location)
        {
            string message = "Load functions cannot have parameters";
            Report(location, message);
        }

        public void ReportTickFunctionWithParameters(TextLocation location)
        {
            string message = "Tick functions cannot have parameters";
            Report(location, message);
        }

        public void ReportUndefinedEnumMember(TextLocation location, string name, string memberName)
        {
            string message = $"Enum {name} has no member named \"{memberName}\"";
            Report(location, message);
        }

        public void ReportAssigningToReadOnly(TextLocation location, BoundNodeKind kind)
        {
            string expressionKindName = "ERROR";
            if (kind == BoundNodeKind.VariableExpression)
                expressionKindName = "Variable";
            else
                expressionKindName = "Field";

            var message = $"{expressionKindName} is read-only and cannot be assigned to";
            Report(location, message);
        }

        public void ReportFunctionIsPrivate(TextLocation location, string name)
        {
            string message = $"Function \"{name}\" is inaccessable due to its protection level";
            Report(location, message);
        }

        public void ReportInstantiationOfAbstractClass(TextLocation location, string name)
        {
            string message = $"Cannot create an instance of abstract type \"{name}\"";
            Report(location, message);
        }

        public void ReportInstantiationWithoutConstructor(TextLocation location, string name)
        {
            string message = $"Type \"{name}\" does not have an accessible constructor";
            Report(location, message);
        }

        public void ReportExpectedType(TextLocation location, BoundNodeKind kind)
        {
            string message = $"Expected type, got {kind}";
            Report(location, message);
        }

        public void ReportInfo(TextLocation location, string text)
        {
            Report(location, text);
        }
    }
}
