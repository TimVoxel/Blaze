using Blaze.Syntax_Nodes;
using System.Collections.Immutable;
using System.Text;

namespace Blaze.Symbols
{
    public sealed class FunctionSymbol : Symbol
    {
        private string? _addressName;

        public NamespaceSymbol ParentNamespace { get; }
        public ImmutableArray<ParameterSymbol> Parameters { get; }
        public TypeSymbol ReturnType { get; }
        public FunctionDeclarationSyntax? Declaration { get; }

        public string AddressName
        {
            get
            {
                //We calculate the call name once
                //Then cache it and use the cached value

                if (_addressName != null)
                    return _addressName;

                var builder = new StringBuilder();
                var stack = new Stack<Symbol>();
                stack.Push(this);

                var previous = ParentNamespace;
                while (previous != null)
                {
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

        public FunctionSymbol(string name, NamespaceSymbol parentNamespace, ImmutableArray<ParameterSymbol> parameters, TypeSymbol returnType, FunctionDeclarationSyntax? declaration = null) : base(name)
        {
            ParentNamespace = parentNamespace;
            Parameters = parameters;
            ReturnType = returnType;
            Declaration = declaration;
        }
    }
}
