using DPP_Compiler.Syntax_Nodes;
using System.Collections.Immutable;

namespace DPP_Compiler.Symbols
{
    public sealed class FunctionSymbol : Symbol
    {
        public ImmutableArray<ParameterSymbol> Parameters { get; private set; }
        public TypeSymbol ReturnType { get; private set; }
        public FunctionDeclarationSyntax? Declaration { get; private set; }

        public override SymbolKind Kind => SymbolKind.Function;

        public FunctionSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol returnType, FunctionDeclarationSyntax? declaration = null) : base(name)
        {
            Parameters = parameters;
            ReturnType = returnType;
            Declaration = declaration;
        }
    }
}
