namespace Blaze.Symbols.BuiltIn
{
    internal sealed class MinecraftNamespace : BuiltInNamespace
    {
        public ChatNamespace Chat { get; }
        public GeneralNamespace General { get; }

        public MinecraftNamespace() : base("minecraft")
        {
            Chat = new ChatNamespace(this);
            General = new GeneralNamespace(this);
        }

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

        internal sealed class GeneralNamespace : BuiltInNamespace
        {
            public NamedTypeSymbol Pos { get; }

            public FunctionSymbol RunCommand { get; }
            public FunctionSymbol DatapackEnable { get; }
            public FunctionSymbol DatapackDisable { get; }
            public FunctionSymbol SetDatapackEnabled { get; }
            public FunctionSymbol GetDatapackCount { get; }
            public FunctionSymbol GetEnabledDatapackCount { get; }
            public FunctionSymbol GetAvailableDatapackCount { get; }
            
            public GeneralNamespace(MinecraftNamespace parent) : base("general", parent)
            {
                //Pos class
                var x = Parameter("x", TypeSymbol.Int);
                var y = Parameter("y", TypeSymbol.Int);
                var z = Parameter("z", TypeSymbol.Int);

                Pos = Class("Pos", Constructor(x, y, z));
                var xField = Field(Pos, "x", TypeSymbol.Int);
                var yField = Field(Pos, "y", TypeSymbol.Int);
                var zField = Field(Pos, "z", TypeSymbol.Int);

                var posConstructorBlock = AssignFieldsBlock(Pos, x, y, z);
                Pos.Constructor.FunctionBody = posConstructorBlock;

                //Functions
                RunCommand = Function("run_command", TypeSymbol.Void, Parameter("command", TypeSymbol.String));
                DatapackEnable = Function("datapack_enable", TypeSymbol.Void, Parameter("pack", TypeSymbol.String));
                DatapackDisable = Function("datapack_disable", TypeSymbol.Void, Parameter("pack", TypeSymbol.String));
                SetDatapackEnabled = Function("set_datapack_enabled", TypeSymbol.Void, Parameter("pack", TypeSymbol.String), Parameter("value", TypeSymbol.Bool));
                GetDatapackCount = Function("get_datapack_count", TypeSymbol.Int);
                GetEnabledDatapackCount = Function("get_enabled_datapack_count", TypeSymbol.Int);
                GetAvailableDatapackCount = Function("get_available_datapack_count", TypeSymbol.Int);
            }

        }
    }
}
