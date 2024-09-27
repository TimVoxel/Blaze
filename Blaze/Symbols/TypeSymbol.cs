namespace Blaze.Symbols
{
    public class TypeSymbol : Symbol
    {
        public static readonly TypeSymbol Error = new TypeSymbol("error");
        public static readonly TypeSymbol Void = new TypeSymbol("void");

        public static readonly TypeSymbol Object = new TypeSymbol("object");
        public static readonly TypeSymbol Int = new TypeSymbol("int");
        public static readonly TypeSymbol Float = new TypeSymbol("float");
        public static readonly TypeSymbol Double = new TypeSymbol("double");
        public static readonly TypeSymbol Bool = new TypeSymbol("bool");
        public static readonly TypeSymbol String = new TypeSymbol("string");

        //TODO: not sure how I would represent this without generics,
        //For now just a placeholder for function expression types
        public static readonly TypeSymbol Function = new TypeSymbol("function");

        private static IEnumerable<TypeSymbol> GetDefinableTypes()
        {
            yield return Object;
            yield return Int;
            yield return Float;
            yield return Double;
            yield return Bool;
            yield return String;
        }

        public override SymbolKind Kind => SymbolKind.Type;
        public bool IsError => this == Error;

        internal TypeSymbol(string name) : base(name) { }

        public static TypeSymbol? Lookup(string name) => GetDefinableTypes().FirstOrDefault(t => t.Name == name);
    }
}
