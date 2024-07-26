using Blaze.Syntax_Nodes;
using System.Collections.Immutable;
using System.Text;

namespace Blaze.Symbols
{
    public class FunctionSymbol : Symbol, IMemberSymbol
    {
        private string? _addressName;
        public ImmutableArray<ParameterSymbol> Parameters { get; }
        public TypeSymbol ReturnType { get; }
        public FunctionDeclarationSyntax? Declaration { get; }
        public IMemberSymbol? Parent { get; }

        public virtual string AddressName
        {
            get
            {
                //We calculate the call name once
                //Then cache it and use the cached value

                if (_addressName != null)
                    return _addressName;

                var builder = new StringBuilder();
                var stack = new Stack<IMemberSymbol>();
                stack.Push(this);

                var previous = Parent;
                while (previous != null)
                {
                    if (previous is NamespaceSymbol ns && ns.IsGlobal)
                        break;
                    stack.Push(previous);
                    previous = previous.Parent;
                }
                while (stack.Any())
                {
                    builder.Append(stack.Pop().Name);
                    if (stack.Count > 0)
                        builder.Append("/");
                }

                _addressName = builder.ToString();
                return _addressName;
            }
        }

        public override SymbolKind Kind => SymbolKind.Function;

        public FunctionSymbol(string name, IMemberSymbol? parent, ImmutableArray<ParameterSymbol> parameters, TypeSymbol returnType, FunctionDeclarationSyntax? declaration = null) : base(name)
        {
            Parent = parent;
            Parameters = parameters;
            ReturnType = returnType;
            Declaration = declaration;
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
