namespace DPP_Compiler
{
    public sealed class VariableSymbol
    {
        public string Name { get; private set; }
        public Type Type { get; private set; }

        internal VariableSymbol(string name, Type type)
        {
            Name = name;
            Type = type;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    internal sealed class LabelSymbol
    {
        public string Name { get; private set; }

        internal LabelSymbol(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
