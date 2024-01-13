namespace DPP_Compiler.Binding
{
    internal sealed class BoundBinaryOperator
    {
        private static BoundBinaryOperator[] _operators =
        {
            new BoundBinaryOperator(SyntaxKind.PlusToken, BoundBinaryOperatorKind.Addition, typeof(int)),
            new BoundBinaryOperator(SyntaxKind.MinusToken, BoundBinaryOperatorKind.Subtraction, typeof(int)),
            new BoundBinaryOperator(SyntaxKind.StarToken, BoundBinaryOperatorKind.Multiplication, typeof(int)),
            new BoundBinaryOperator(SyntaxKind.SlashToken, BoundBinaryOperatorKind.Division, typeof(int)),
            new BoundBinaryOperator(SyntaxKind.DoubleEqualsToken, BoundBinaryOperatorKind.Equals, typeof(int), typeof(bool)),
            new BoundBinaryOperator(SyntaxKind.NotEqualsToken, BoundBinaryOperatorKind.NotEquals, typeof(int), typeof(bool)),
            new BoundBinaryOperator(SyntaxKind.LessToken, BoundBinaryOperatorKind.Less, typeof(int), typeof(bool)),
            new BoundBinaryOperator(SyntaxKind.LessOrEqualsToken, BoundBinaryOperatorKind.LessOrEquals, typeof(int), typeof(bool)),
            new BoundBinaryOperator(SyntaxKind.GreaterToken, BoundBinaryOperatorKind.Greater, typeof(int), typeof(bool)),
            new BoundBinaryOperator(SyntaxKind.GreaterOrEqualsToken, BoundBinaryOperatorKind.GreaterOrEquals, typeof(int), typeof(bool)),

            new BoundBinaryOperator(SyntaxKind.DoubleAmpersandToken, BoundBinaryOperatorKind.LogicalMultiplication, typeof(bool)),
            new BoundBinaryOperator(SyntaxKind.DoublePipeToken, BoundBinaryOperatorKind.LogicalAddition, typeof(bool)),
            new BoundBinaryOperator(SyntaxKind.DoubleEqualsToken, BoundBinaryOperatorKind.Equals, typeof(bool)),
            new BoundBinaryOperator(SyntaxKind.NotEqualsToken, BoundBinaryOperatorKind.NotEquals, typeof(bool)),
        };

        public SyntaxKind SyntaxKind { get; private set; }
        public BoundBinaryOperatorKind OperatorKind { get; private set; }
        public Type LeftType { get; private set; }
        public Type RightType { get; private set; }
        public Type ResultType { get; private set; }

        private BoundBinaryOperator(SyntaxKind kind, BoundBinaryOperatorKind operatorKind, Type leftType, Type rightType, Type resultType)
        {
            SyntaxKind = kind;
            OperatorKind = operatorKind;
            LeftType = leftType;
            RightType = rightType;
            ResultType = resultType;
        }

        private BoundBinaryOperator(SyntaxKind kind, BoundBinaryOperatorKind operatorKind, Type type) : this(kind, operatorKind, type, type, type) { }
        private BoundBinaryOperator(SyntaxKind kind, BoundBinaryOperatorKind operatorKind, Type operandType, Type resultType) : this(kind, operatorKind, operandType, operandType, resultType) { }

        internal static BoundBinaryOperator? Bind(SyntaxKind kind, Type leftType, Type rightType)
        {
            foreach (BoundBinaryOperator binary in _operators)
            {
                if (binary.SyntaxKind == kind && binary.LeftType == leftType && binary.RightType == rightType)
                    return binary;
            }
            return null;
        }

        internal static BoundBinaryOperator SafeBind(BoundBinaryOperatorKind kind, Type leftType, Type rightType)
        {
            foreach (BoundBinaryOperator binary in _operators)
            {
                if (binary.OperatorKind == kind && binary.LeftType == leftType && binary.RightType == rightType)
                    return binary;
            }
            throw new Exception($"Operator of kind {kind} is not defined for types {leftType} and {rightType}");
        }
    }
}