namespace Blaze.Symbols.BuiltIn
{
    internal sealed class ChatNamespace : BuiltInNamespace
    {
        public FunctionSymbol Say { get; }
        public FunctionSymbol Print { get; }

        public ChatNamespace(MinecraftNamespace parent) : base("chat", parent)
        {
            Say = Function("say", TypeSymbol.Void, Parameter("text", TypeSymbol.String));
            Print = Function("print", TypeSymbol.Void, Parameter("text", TypeSymbol.String));
        }
    }
}
