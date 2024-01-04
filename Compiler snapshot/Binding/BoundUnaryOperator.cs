namespace Compiler_snapshot.Binding
{
    internal sealed class BoundUnaryOperator
    {
        private static BoundUnaryOperator[] _operators =
        {
            new BoundUnaryOperator(SyntaxKind.PlusToken, BoundUnaryOperatorKind.Identity, typeof(int)),
            new BoundUnaryOperator(SyntaxKind.MinusToken, BoundUnaryOperatorKind.Negation, typeof(int)),
            new BoundUnaryOperator(SyntaxKind.ExclamationSignToken, BoundUnaryOperatorKind.LogicalNegation, typeof(bool))
        };

        public SyntaxKind SyntaxKind { get; private set; }
        public BoundUnaryOperatorKind OperatorKind { get; private set; }
        public Type OperandType { get; private set; }
        public Type ResultType { get; private set; }

        private BoundUnaryOperator(SyntaxKind kind, BoundUnaryOperatorKind operatorKind, Type operandType, Type resultType)
        {
            SyntaxKind = kind;
            OperatorKind = operatorKind;
            OperandType = operandType;
            ResultType = resultType;
        }

        private BoundUnaryOperator(SyntaxKind kind, BoundUnaryOperatorKind operatorKind, Type operandType) : this(kind, operatorKind, operandType, operandType) { }

        internal static BoundUnaryOperator? Bind(SyntaxKind kind, Type operandType)
        {
            foreach (BoundUnaryOperator unaryOperator in _operators)
            {
                if (unaryOperator.SyntaxKind == kind && unaryOperator.OperandType == operandType)
                    return unaryOperator;
            }
            return null;
        } 
    }
}