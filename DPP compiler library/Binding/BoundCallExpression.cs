using System.Collections.Immutable;
using DPP_Compiler.Symbols;

namespace DPP_Compiler.Binding
{
    internal sealed class BoundCallExpression : BoundExpression
    {
        public FunctionSymbol Function { get; private set; }
        public ImmutableArray<BoundExpression> Arguments { get; private set; }

        public override BoundNodeKind Kind => BoundNodeKind.CallExpression;
        public override TypeSymbol Type => Function.ReturnType;

        public BoundCallExpression(FunctionSymbol function, ImmutableArray<BoundExpression> arguments)
        {
            Function = function;
            Arguments = arguments;
        }

        public override IEnumerable<BoundNode> GetChildren()
        {
            foreach (BoundExpression expression in Arguments)
                yield return expression;
        }
    }
}