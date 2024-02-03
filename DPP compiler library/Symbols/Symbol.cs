using System.Diagnostics.SymbolStore;

namespace DPP_Compiler.Symbols
{
    public abstract class Symbol
    {
        public string Name { get; private set; }

        public abstract SymbolKind Kind { get; }

        internal Symbol(string name)
        {
            Name = name;
        }

        public void WriteTo(TextWriter writer)
        {
            SymbolPrinter.WriteTo(this, writer);
        }

        public override string ToString()
        {
            using (StringWriter writer = new StringWriter())
            {
                WriteTo(writer);
                return writer.ToString();
            }
        }
    }
}
