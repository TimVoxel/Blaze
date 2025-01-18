namespace Blaze.Symbols.BuiltIn
{
    internal sealed class GeneralNamespace : BuiltInNamespace
    {
        public GamerulesNamespace Gamerules { get; }
        public WeatherNamespace Weather { get; }

        public EnumSymbol Difficulty { get; }

        public NamedTypeSymbol Vector3 { get; }
        public NamedTypeSymbol Vector2 { get; }
        public NamedTypeSymbol Vector3f { get; }
        public NamedTypeSymbol Vector2f { get; }
        public NamedTypeSymbol Vector3Int { get; }
        public NamedTypeSymbol Vector2Int { get; }

        public FieldSymbol DifficultyField { get; }

        public FunctionSymbol RunCommand { get; }
        public FunctionSymbol DatapackEnable { get; }
        public FunctionSymbol DatapackDisable { get; }
        public FunctionSymbol SetDatapackEnabled { get; }
        public FunctionSymbol GetDatapackCount { get; }
        public FunctionSymbol GetEnabledDatapackCount { get; }
        public FunctionSymbol GetAvailableDatapackCount { get; }
            
        public GeneralNamespace(MinecraftNamespace parent) : base("general", parent)
        {
            //Nested namespaces
            Gamerules = new GamerulesNamespace(this);
            Weather = new WeatherNamespace(this);

            //Classes
            Difficulty = Enum("Difficulty", true);
            DeclareEnumMember("Peaceful", Difficulty, 0);
            DeclareEnumMember("Easy", Difficulty, 1);
            DeclareEnumMember("Normal", Difficulty, 2);
            DeclareEnumMember("Hard", Difficulty, 3);

            Vector3 = Class("Vector3", null, false,
                    ("x", TypeSymbol.Double),
                    ("y", TypeSymbol.Double),
                    ("z", TypeSymbol.Double)
                );

            Vector2 = Class("Vector2", null, false,
                    ("x", TypeSymbol.Double),
                    ("y", TypeSymbol.Double)
                );

            Vector3f = Class("Vector3f", null, false,
                    ("x", TypeSymbol.Float),
                    ("y", TypeSymbol.Float),
                    ("z", TypeSymbol.Float)
                );

            Vector2f = Class("Vector2f", null, false,
                    ("y", TypeSymbol.Float),
                    ("x", TypeSymbol.Float)
                );

            Vector3Int = Class("Vector3Int", null, false,
                    ("x", TypeSymbol.Int),
                    ("y", TypeSymbol.Int),
                    ("z", TypeSymbol.Int)
                );

            Vector2Int = Class("Vector2Int", null, false,
                    ("x", TypeSymbol.Int),
                    ("y", TypeSymbol.Int)
                );

            //Functions
            RunCommand = Function("run_command", TypeSymbol.Void, Parameter("command", TypeSymbol.String));
            DatapackEnable = Function("datapack_enable", TypeSymbol.Void, Parameter("pack", TypeSymbol.String));
            DatapackDisable = Function("datapack_disable", TypeSymbol.Void, Parameter("pack", TypeSymbol.String));
            SetDatapackEnabled = Function("set_datapack_enabled", TypeSymbol.Void, Parameter("pack", TypeSymbol.String), Parameter("value", TypeSymbol.Bool));
            GetDatapackCount = Function("get_datapack_count", TypeSymbol.Int);
            GetEnabledDatapackCount = Function("get_enabled_datapack_count", TypeSymbol.Int);
            GetAvailableDatapackCount = Function("get_available_datapack_count", TypeSymbol.Int);

            //Fields
            DifficultyField = AddField(Symbol, "difficulty", Difficulty);
        }
    }
}
