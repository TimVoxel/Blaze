using Blaze.Symbols;
using System.Diagnostics;

namespace Blaze.Binding
{
    internal sealed class BoundBinaryOperator
    {
        public readonly static BoundBinaryOperator NamedTypeDoubleEqualsOperator
            = new BoundBinaryOperator(SyntaxKind.DoubleEqualsToken, BoundBinaryOperatorKind.Equals, TypeSymbol.Object, TypeSymbol.Bool);
        public readonly static BoundBinaryOperator NamedTypeNotEqualsOperator
            = new BoundBinaryOperator(SyntaxKind.NotEqualsToken, BoundBinaryOperatorKind.NotEquals, TypeSymbol.Object, TypeSymbol.Bool);

        public readonly static BoundBinaryOperator EnumValueEqualsOperator
            = new BoundBinaryOperator(SyntaxKind.DoubleEqualsToken, BoundBinaryOperatorKind.Equals, TypeSymbol.Object, TypeSymbol.Bool);
        public readonly static BoundBinaryOperator EnumValueNotEqualsOperator
            = new BoundBinaryOperator(SyntaxKind.NotEqualsToken, BoundBinaryOperatorKind.NotEquals, TypeSymbol.Object, TypeSymbol.Bool); 

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

            new BoundBinaryOperator(SyntaxKind.PlusToken, BoundBinaryOperatorKind.Addition, TypeSymbol.Float),
            new BoundBinaryOperator(SyntaxKind.MinusToken, BoundBinaryOperatorKind.Subtraction, TypeSymbol.Float),
            new BoundBinaryOperator(SyntaxKind.StarToken, BoundBinaryOperatorKind.Multiplication, TypeSymbol.Float),
            new BoundBinaryOperator(SyntaxKind.SlashToken, BoundBinaryOperatorKind.Division, TypeSymbol.Float),
            new BoundBinaryOperator(SyntaxKind.DoubleEqualsToken, BoundBinaryOperatorKind.Equals, TypeSymbol.Float, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.NotEqualsToken, BoundBinaryOperatorKind.NotEquals, TypeSymbol.Float, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.LessToken, BoundBinaryOperatorKind.Less, TypeSymbol.Float, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.LessOrEqualsToken, BoundBinaryOperatorKind.LessOrEquals, TypeSymbol.Float, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.GreaterToken, BoundBinaryOperatorKind.Greater, TypeSymbol.Float, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.GreaterOrEqualsToken, BoundBinaryOperatorKind.GreaterOrEquals, TypeSymbol.Float, TypeSymbol.Bool),

            new BoundBinaryOperator(SyntaxKind.PlusToken, BoundBinaryOperatorKind.Addition, TypeSymbol.Double),
            new BoundBinaryOperator(SyntaxKind.MinusToken, BoundBinaryOperatorKind.Subtraction, TypeSymbol.Double),
            new BoundBinaryOperator(SyntaxKind.StarToken, BoundBinaryOperatorKind.Multiplication, TypeSymbol.Double),
            new BoundBinaryOperator(SyntaxKind.SlashToken, BoundBinaryOperatorKind.Division, TypeSymbol.Double),
            new BoundBinaryOperator(SyntaxKind.DoubleEqualsToken, BoundBinaryOperatorKind.Equals, TypeSymbol.Double, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.NotEqualsToken, BoundBinaryOperatorKind.NotEquals, TypeSymbol.Double, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.LessToken, BoundBinaryOperatorKind.Less, TypeSymbol.Double, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.LessOrEqualsToken, BoundBinaryOperatorKind.LessOrEquals, TypeSymbol.Double, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.GreaterToken, BoundBinaryOperatorKind.Greater, TypeSymbol.Double, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.GreaterOrEqualsToken, BoundBinaryOperatorKind.GreaterOrEquals, TypeSymbol.Double, TypeSymbol.Bool),

            new BoundBinaryOperator(SyntaxKind.DoubleAmpersandToken, BoundBinaryOperatorKind.LogicalMultiplication, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.DoublePipeToken, BoundBinaryOperatorKind.LogicalAddition, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.DoubleEqualsToken, BoundBinaryOperatorKind.Equals, TypeSymbol.Bool),
            new BoundBinaryOperator(SyntaxKind.NotEqualsToken, BoundBinaryOperatorKind.NotEquals, TypeSymbol.Bool),

            //new BoundBinaryOperator(SyntaxKind.PlusToken, BoundBinaryOperatorKind.Addition, TypeSymbol.String),
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
            if (TryBindEnumOrNamedTypeOperator(kind, leftType, rightType, out BoundBinaryOperator? op))
                return op;

            foreach (BoundBinaryOperator binary in _operators)
                if (binary.SyntaxKind == kind && binary.LeftType == leftType && binary.RightType == rightType)
                    return binary;
            
            return null;
        }

        internal static BoundBinaryOperator SafeBind(BoundBinaryOperatorKind kind, TypeSymbol leftType, TypeSymbol rightType)
        {
            if (TryBindEnumOrNamedTypeOperator(kind, leftType, rightType, out BoundBinaryOperator? op))
            {
                Debug.Assert(op != null);
                return op;
            }

            foreach (BoundBinaryOperator binary in _operators)
            {
                if (binary.OperatorKind == kind && binary.LeftType == leftType && binary.RightType == rightType)
                    return binary;
            }
            throw new Exception($"Operator of kind {kind} is not defined for types {leftType} and {rightType}");
        }

        private static bool TryBindEnumOrNamedTypeOperator(BoundBinaryOperatorKind kind, TypeSymbol leftType, TypeSymbol rightType, out BoundBinaryOperator? op)
        {
            op = null;

            if (leftType == rightType)
            {
                if (leftType is NamedTypeSymbol && rightType is NamedTypeSymbol)
                {
                    if (kind == BoundBinaryOperatorKind.Equals)
                        op = NamedTypeDoubleEqualsOperator;
                    else if (kind == BoundBinaryOperatorKind.NotEquals)
                        op = NamedTypeNotEqualsOperator;
                }
                else if (leftType is EnumSymbol && rightType is EnumSymbol)
                {
                    if (kind == BoundBinaryOperatorKind.Equals)
                        op = EnumValueEqualsOperator;
                    else if (kind == BoundBinaryOperatorKind.NotEquals)
                        op = EnumValueNotEqualsOperator;
                }
            }
            return op != null;
        }

        private static bool TryBindEnumOrNamedTypeOperator(SyntaxKind kind, TypeSymbol leftType, TypeSymbol rightType, out BoundBinaryOperator? op)
        {
            BoundBinaryOperatorKind? operatorKind = null;

            if (kind == SyntaxKind.DoubleEqualsToken)
                operatorKind = BoundBinaryOperatorKind.Equals;
            else if (kind == SyntaxKind.NotEqualsToken)
                operatorKind = BoundBinaryOperatorKind.NotEquals;
            
            if (operatorKind != null)
                return TryBindEnumOrNamedTypeOperator((BoundBinaryOperatorKind) operatorKind, leftType, rightType, out op);
            else
            {
                op = null;
                return false;
            }
        }
    }
}