using Blaze.Symbols;

namespace Blaze.Binding
{
    internal sealed class BoundBinaryOperator
    {
        public readonly static BoundBinaryOperator NamedTypeDoubleEqualsOperator
            = new BoundBinaryOperator(SyntaxKind.DoubleEqualsToken, BoundBinaryOperatorKind.Equals, TypeSymbol.Object, TypeSymbol.Bool);

        private static BoundBinaryOperator[] _operators =
        {
            new BoundBinaryOperator(SyntaxKind.PlusToken, BoundBinaryOperatorKind.Addition, TypeSymbol.Int),
            new BoundBinaryOperator(SyntaxKind.MinusToken, BoundBinaryOperatorKind.Subtraction, TypeSymbol.Int),
            new BoundBinaryOperator(SyntaxKind.StarToken, BoundBinaryOperatorKind.Multiplication, TypeSymbol.Int),
            new BoundBinaryOperator(SyntaxKind.SlashToken, BoundBinaryOperatorKind.Division, TypeSymbol.Int),
            new BoundBinaryOperator(SyntaxKind.DoubleEqualsToken, BoundBinaryOperatorKind.Equals, TypeSymbol.Int, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.NotEqualsToken, BoundBinaryOperatorKind.NotEquals, TypeSymbol.Int, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.LessToken, BoundBinaryOperatorKind.Less, TypeSymbol.Int, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.LessOrEqualsToken, BoundBinaryOperatorKind.LessOrEquals, TypeSymbol.Int, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.GreaterToken, BoundBinaryOperatorKind.Greater, TypeSymbol.Int, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.GreaterOrEqualsToken, BoundBinaryOperatorKind.GreaterOrEquals, TypeSymbol.Int, TypeSymbol.Bool),

            new BoundBinaryOperator(SyntaxKind.DoubleAmpersandToken, BoundBinaryOperatorKind.LogicalMultiplication, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.DoublePipeToken, BoundBinaryOperatorKind.LogicalAddition, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.DoubleEqualsToken, BoundBinaryOperatorKind.Equals, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.NotEqualsToken, BoundBinaryOperatorKind.NotEquals, TypeSymbol.Bool),

            new BoundBinaryOperator(SyntaxKind.PlusToken, BoundBinaryOperatorKind.Addition, TypeSymbol.String),
            new BoundBinaryOperator(SyntaxKind.DoubleEqualsToken, BoundBinaryOperatorKind.Equals, TypeSymbol.String, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.NotEqualsToken, BoundBinaryOperatorKind.NotEquals, TypeSymbol.String, TypeSymbol.Bool),

            new BoundBinaryOperator(SyntaxKind.DoubleEqualsToken, BoundBinaryOperatorKind.Equals, TypeSymbol.Object, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.NotEqualsToken, BoundBinaryOperatorKind.NotEquals, TypeSymbol.Object, TypeSymbol.Bool),
        };

        public SyntaxKind SyntaxKind { get; private set; }
        public BoundBinaryOperatorKind OperatorKind { get; private set; }
        public TypeSymbol LeftType { get; private set; }
        public TypeSymbol RightType { get; private set; }
        public TypeSymbol ResultType { get; private set; }

        private BoundBinaryOperator(SyntaxKind kind, BoundBinaryOperatorKind operatorKind, TypeSymbol leftType, TypeSymbol rightType, TypeSymbol resultType)
        {
            SyntaxKind = kind;
            OperatorKind = operatorKind;
            LeftType = leftType;
            RightType = rightType;
            ResultType = resultType;
        }

        private BoundBinaryOperator(SyntaxKind kind, BoundBinaryOperatorKind operatorKind, TypeSymbol type) : this(kind, operatorKind, type, type, type) { }
        private BoundBinaryOperator(SyntaxKind kind, BoundBinaryOperatorKind operatorKind, TypeSymbol operandType, TypeSymbol resultType) : this(kind, operatorKind, operandType, operandType, resultType) { }

        internal static BoundBinaryOperator? Bind(SyntaxKind kind, TypeSymbol leftType, TypeSymbol rightType)
        {
            if (leftType is NamedTypeSymbol && rightType is NamedTypeSymbol && leftType == rightType)
                return NamedTypeDoubleEqualsOperator;

            foreach (BoundBinaryOperator binary in _operators)
            {
                if (binary.SyntaxKind == kind && binary.LeftType == leftType && binary.RightType == rightType)
                    return binary;
            }
            return null;
        }

        internal static BoundBinaryOperator SafeBind(BoundBinaryOperatorKind kind, TypeSymbol leftType, TypeSymbol rightType)
        {
            if (leftType is NamedTypeSymbol && rightType is NamedTypeSymbol && leftType == rightType)
                return NamedTypeDoubleEqualsOperator;

            foreach (BoundBinaryOperator binary in _operators)
            {
                if (binary.OperatorKind == kind && binary.LeftType == leftType && binary.RightType == rightType)
                    return binary;
            }
            throw new Exception($"Operator of kind {kind} is not defined for types {leftType} and {rightType}");
        }
    }
}