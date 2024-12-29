namespace Blaze.Symbols.BuiltIn
{
    internal sealed class BlazeNamespace : BuiltInNamespace 
    {
        public MathNamespace Math { get; }
        public BlazeFabricatedNamespace Fabricated { get; }

        public BlazeNamespace() : base("blaze")
        {
            Math = new MathNamespace(this);
            Fabricated = new BlazeFabricatedNamespace(this);
        }
    }
}
