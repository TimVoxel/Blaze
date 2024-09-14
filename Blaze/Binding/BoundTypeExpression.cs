using Blaze.Symbols;

namespace Blaze.Binding
{
    internal sealed class BoundTypeExpression : BoundExpression
    {
        public TypeSymbol TypeSymbol { get; }

        public override BoundNodeKind Kind => BoundNodeKind.TypeExpression;
        public override TypeSymbol Type => TypeSymbol;

        public BoundTypeExpression(TypeSymbol symbol)
        {
            TypeSymbol = symbol;
        }
    }

    internal sealed class BoundEnumExpression : BoundExpression
    {
        public EnumSymbol EnumSymbol { get; }

        public override BoundNodeKind Kind => BoundNodeKind.EnumExpression;
        public override TypeSymbol Type => EnumSymbol;

        public BoundEnumExpression(EnumSymbol enumSymbol)
        {
            EnumSymbol = enumSymbol;
        }
    }

    internal sealed class BoundNamespaceExpression : BoundExpression
    {
        public NamespaceSymbol Namespace { get; }

        public override BoundNodeKind Kind => BoundNodeKind.NamespaceExpression;
        public override TypeSymbol Type => TypeSymbol.Void;

        public BoundNamespaceExpression(NamespaceSymbol namespaceSymbol)
        {
            Namespace = namespaceSymbol;
        }
    }

}