namespace Blaze.Symbols
{
    public sealed class TypeSymbol : Symbol
    {
        public static readonly TypeSymbol Error = new TypeSymbol("error");
        public static readonly TypeSymbol Void = new TypeSymbol("void");

        public static readonly TypeSymbol Object = new TypeSymbol("object");
        public static readonly TypeSymbol Int = new TypeSymbol("int");
        public static readonly TypeSymbol Bool = new TypeSymbol("bool");
        public static readonly TypeSymbol String = new TypeSymbol("string");

        private static IEnumerable<TypeSymbol> GetDefinableTypes()
        {
            yield return Object;
            yield return Int;
            yield return Bool;
            yield return String;
        }

        public override SymbolKind Kind => SymbolKind.Type;
        public bool IsError => this == Error;

        private TypeSymbol(string name) : base(name) { }

        public static TypeSymbol? Lookup(string name)
        {
            foreach (TypeSymbol type in GetDefinableTypes())
                if (type.Name == name)
                    return type;

            return null;
        }
    }
}
