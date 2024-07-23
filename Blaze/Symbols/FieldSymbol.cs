using Blaze.Binding;
using System.Text;

namespace Blaze.Symbols
{
    public sealed class FieldSymbol : VariableSymbol, IMemberSymbol
    {
        public IMemberSymbol Parent { get; }

        public override SymbolKind Kind => SymbolKind.Field;

        public FieldSymbol(string name, IMemberSymbol parent, TypeSymbol type) : base(name, type, null)
        {
            Parent = parent;
        }

        public string GetFullName()
        {
            var nameBuilder = new StringBuilder();
            var nameStack = new Stack<string>();
            IMemberSymbol? previous = this;

            while (previous != null)
            {
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
