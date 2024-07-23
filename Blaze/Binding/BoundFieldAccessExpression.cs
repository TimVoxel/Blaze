using Blaze.Symbols;

namespace Blaze.Binding
{
    internal sealed class BoundFieldAccessExpression : BoundExpression
    { 
        public BoundExpression Instance { get; }
        public FieldSymbol Field { get; }

        public override BoundNodeKind Kind => BoundNodeKind.FieldAccessExpression;
        public override TypeSymbol Type => Field.Type;

        public BoundFieldAccessExpression(BoundExpression instance, FieldSymbol field)
        {
            Instance = instance;
            Field = field;
        }
    }
}