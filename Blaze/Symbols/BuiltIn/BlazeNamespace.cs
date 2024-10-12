namespace Blaze.Symbols.BuiltIn
{
    internal sealed class BlazeNamespace : BuiltInNamespace 
    {
        public MathNamespace Math { get; }

        public BlazeNamespace() : base("blaze")
        {
            Math = new MathNamespace(this);
        }
    }
}
