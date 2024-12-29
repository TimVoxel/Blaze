namespace Blaze.Symbols.BuiltIn
{
    internal sealed class BlazeFabricatedNamespace : BuiltInNamespace 
    {
        public BlazeFabricatedNamespace(BlazeNamespace blaze) : base("fabricated", blaze) { }
    }
}
