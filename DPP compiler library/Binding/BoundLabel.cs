namespace DPP_Compiler.Binding
{
    internal sealed class BoundLabel
    {
        public string Name { get; private set; }

        internal BoundLabel(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
