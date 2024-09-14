using System.Text;
using Blaze.Binding;

namespace Blaze.Symbols
{
    public sealed class EnumMemberSymbol : VariableSymbol, IMemberSymbol
    {
        public int UnderlyingValue { get; }
        public IMemberSymbol? Parent { get; }
        public override SymbolKind Kind => SymbolKind.EnumMember;

        public EnumMemberSymbol(EnumSymbol parent, string name, int underlyingValue) : base(name, parent, true, new BoundConstant(underlyingValue))
        {
            Parent = parent;
            UnderlyingValue = underlyingValue;
        }

        public string GetFullName()
        {
            var nameBuilder = new StringBuilder();
            var nameStack = new Stack<string>();
            IMemberSymbol? previous = this;

            while (previous != null)
            {
                if (previous is NamespaceSymbol ns && ns.IsGlobal)
                    break;
                nameStack.Push(previous.Name);
                previous = previous.Parent;
            }

            while (nameStack.Any())
            {
                nameBuilder.Append(nameStack.Pop());
                if (nameStack.Count >= 1)
                    nameBuilder.Append(".");
            }
            return nameBuilder.ToString();
        }
    }
}
