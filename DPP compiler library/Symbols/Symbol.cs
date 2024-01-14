namespace DPP_Compiler.Symbols
{
    public abstract class Symbol
    {
        public string Name { get; private set; }

        public abstract SymbolKind Kind { get; }

        private protected Symbol(string name)
        {
            Name = name;
        }

        public override string ToString() => Name;
    }
}
