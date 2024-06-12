using Blaze.Binding;
using Blaze.Syntax_Nodes;
using System.Text;

namespace Blaze.Symbols
{
    public sealed class NamespaceSymbol : Symbol
    {
        public NamespaceSymbol? Parent { get; }
        public List<NamespaceSymbol> Children { get; }
        public NamespaceDeclarationSyntax? Declaration { get; }
        public BoundScope Scope { get; }

        public override SymbolKind Kind => SymbolKind.Namespace;

        public NamespaceSymbol(string name, BoundScope scope, NamespaceSymbol? parent, NamespaceDeclarationSyntax? declaration) : base(name)
        {
            Parent = parent;
            Scope = scope;
            Declaration = declaration;
            Children = new List<NamespaceSymbol>();
        }    

        public NamespaceSymbol? TryLookupChild(string name)
        {
            return Children.FirstOrDefault(n => n.Name == name);
        }

        public string GetFullName()
        {
            var nameBuilder = new StringBuilder();
            var nameStack = new Stack<string>();
            var previous = this;

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
