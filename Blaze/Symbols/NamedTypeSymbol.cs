using System.Text;

namespace Blaze.Symbols
{
    public sealed class NamedTypeSymbol : TypeSymbol, IMemberSymbol
    {
        public List<IMemberSymbol> Members { get; }
        public IMemberSymbol Parent { get; }
        public ConstructorSymbol? Constructor { get; }
        public NamedTypeSymbol? Base { get; }

        public bool IsAbstract { get; }

        public IEnumerable<FunctionSymbol> Methods => Members.OfType<FunctionSymbol>();
        public IEnumerable<FieldSymbol> Fields => Members.OfType<FieldSymbol>();
        
        public override SymbolKind Kind => SymbolKind.NamedType;

        public NamedTypeSymbol(string name, NamedTypeSymbol? baseType, IMemberSymbol parent, ConstructorSymbol? constructor, bool isAbstract) : base(name)
        {
            Members = new List<IMemberSymbol>();
            Parent = parent;
            Constructor = constructor;
            Base = baseType;
            IsAbstract = isAbstract;
        }

        public T? TryLookup<T>(string name) where T : IMemberSymbol
        {
            T? result = Members.OfType<T>().FirstOrDefault(m => m.Name == name);
            if (result == null && Base != null)
                return Base.TryLookup<T>(name);
            return result;
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
