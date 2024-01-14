using DPP_Compiler.Symbols;

namespace DPP_Compiler.Binding
{
    internal sealed class BoundBinaryExpression : BoundExpression
    {
        public BoundExpression Left { get; private set; }
        public BoundBinaryOperator Operator { get; private set; }
        public BoundExpression Right { get; private set; }

        public override BoundNodeKind Kind => BoundNodeKind.BinaryExpression;
        public override TypeSymbol Type => Operator.ResultType;

        public BoundBinaryExpression(BoundExpression left, BoundBinaryOperator op, BoundExpression right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }

        public override IEnumerable<BoundNode> GetChildren()
        {
            yield return Left;
            yield return Right;
        }
    }
}