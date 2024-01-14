namespace DPP_Compiler.Symbols
{
    public sealed class TypeSymbol : Symbol
    {
        public static readonly TypeSymbol Error = new TypeSymbol("error");

        public static readonly TypeSymbol Int = new TypeSymbol("int");
        public static readonly TypeSymbol Bool = new TypeSymbol("bool");
        public static readonly TypeSymbol String = new TypeSymbol("string");

        public override SymbolKind Kind => SymbolKind.Type;
        public bool IsError => this == Error;

        private TypeSymbol(string name) : base(name) { }
    }
}
