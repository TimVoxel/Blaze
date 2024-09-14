using System.Text;

namespace Blaze.Symbols
{
    public sealed class EnumSymbol : TypeSymbol, IMemberSymbol
    {
        public IMemberSymbol Parent { get; }
        public List<EnumMemberSymbol> Members { get; }
        public override SymbolKind Kind => SymbolKind.Enum;

        public EnumSymbol(IMemberSymbol parent, string name) : base(name)
        {
            Parent = parent;
            Members = new List<EnumMemberSymbol>();
        }

        public EnumMemberSymbol? TryLookup(string name) => Members.FirstOrDefault(m => m.Name == name);

        public T? TryLookup<T>(string name) where T : IMemberSymbol
            => Members.OfType<T>().FirstOrDefault(m => m.Name == name);

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
