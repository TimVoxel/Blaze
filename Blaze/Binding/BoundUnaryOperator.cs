using Blaze.Symbols;

namespace Blaze.Binding
{
    internal sealed class BoundUnaryOperator
    {
        private static BoundUnaryOperator[] _operators =
        {
            new BoundUnaryOperator(SyntaxKind.PlusToken, BoundUnaryOperatorKind.Identity, TypeSymbol.Int),
            new BoundUnaryOperator(SyntaxKind.MinusToken, BoundUnaryOperatorKind.Negation, TypeSymbol.Int),
            new BoundUnaryOperator(SyntaxKind.ExclamationSignToken, BoundUnaryOperatorKind.LogicalNegation, TypeSymbol.Bool)
        };

        public SyntaxKind SyntaxKind { get; private set; }
        public BoundUnaryOperatorKind OperatorKind { get; private set; }
        public TypeSymbol OperandType { get; private set; }
        public TypeSymbol ResultType { get; private set; }

        private BoundUnaryOperator(SyntaxKind kind, BoundUnaryOperatorKind operatorKind, TypeSymbol operandType, TypeSymbol resultType)
        {
            SyntaxKind = kind;
            OperatorKind = operatorKind;
            OperandType = operandType;
            ResultType = resultType;
        }

        private BoundUnaryOperator(SyntaxKind kind, BoundUnaryOperatorKind operatorKind, TypeSymbol operandType) : this(kind, operatorKind, operandType, operandType) { }

        internal static BoundUnaryOperator? Bind(SyntaxKind kind, TypeSymbol operandType)
        {
            foreach (BoundUnaryOperator unaryOperator in _operators)
            {
                if (unaryOperator.SyntaxKind == kind && unaryOperator.OperandType == operandType)
                    return unaryOperator;
            }
            return null;
        } 

        internal static BoundUnaryOperator SafeBind(SyntaxKind kind, TypeSymbol operandType)
        {
            foreach (BoundUnaryOperator unaryOperator in _operators)
            {
                if (unaryOperator.SyntaxKind == kind && unaryOperator.OperandType == operandType)
                    return unaryOperator;
            }
            throw new Exception($"Operator for syntax {kind} and operand type {operandType} does not exist");
        }
    }
}