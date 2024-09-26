using System.Text;
using Blaze.Binding;

namespace Blaze.Symbols
{
    public class StringEnumMemberSymbol : EnumMemberSymbol
    {
        public string UnderlyingValue { get; }

        public StringEnumMemberSymbol(EnumSymbol parent, string name, string underlyingValue) : base(parent, name, underlyingValue)
        {
            UnderlyingValue = underlyingValue;
        }
    }

    public class IntEnumMemberSymbol : EnumMemberSymbol
    {
        public int UnderlyingValue { get; }

        public IntEnumMemberSymbol(EnumSymbol parent, string name, int underlyingValue) : base(parent, name, underlyingValue)
        {
            UnderlyingValue = underlyingValue;
        }
    }

    public abstract class EnumMemberSymbol : VariableSymbol, IMemberSymbol
    {   
        public IMemberSymbol? Parent { get; }
        public override SymbolKind Kind => SymbolKind.EnumMember;

        public EnumMemberSymbol(EnumSymbol parent, string name, object underlyingValue) : base(name, parent, true, new BoundConstant(underlyingValue))
        {
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
