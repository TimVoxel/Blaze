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
            Gamerules = new GamerulesNamespace(this);
            Weather = new WeatherNamespace(this);

            Difficulty = Enum("Difficulty", true);
            DeclareEnumMember("Peaceful", Difficulty, 0);
            DeclareEnumMember("Easy", Difficulty, 1);
            DeclareEnumMember("Normal", Difficulty, 2);
            DeclareEnumMember("Hard", Difficulty, 3);

            DifficultyField = Field(Symbol, "difficulty", Difficulty);

            //Pos class
            var x = Parameter("x", TypeSymbol.Double);
            var y = Parameter("y", TypeSymbol.Double);
            var z = Parameter("z", TypeSymbol.Double);
            var vec3constructor = Constructor(x, y, z);
            var vec2constructor = Constructor(x, y);

            var xf = Parameter("x", TypeSymbol.Float);
            var yf = Parameter("y", TypeSymbol.Float);
            var zf = Parameter("z", TypeSymbol.Float);
            var vec3fConstructor = Constructor(xf, yf, zf);
            var vec2fConstructor = Constructor(xf, yf);

            Vector3 = Class("Vector3", vec3constructor);
            Vector2 = Class("Vector2", vec2constructor);
            Vector3f = Class("Vector3f", vec3fConstructor);
            Vector2f = Class("Vector2f", vec2fConstructor);

            Field(Vector3, "x", TypeSymbol.Double);
            Field(Vector3, "y", TypeSymbol.Double);
            Field(Vector3, "z", TypeSymbol.Double);

            Field(Vector2, "x", TypeSymbol.Double);
            Field(Vector2, "y", TypeSymbol.Double);

            Field(Vector3f, "x", TypeSymbol.Float);
            Field(Vector3f, "y", TypeSymbol.Float);
            Field(Vector3f, "z", TypeSymbol.Float);

            Field(Vector2f, "x", TypeSymbol.Float);
            Field(Vector2f, "y", TypeSymbol.Float);

            var vec3constructorBlock = AssignFieldsBlock(Vector3, x, y, z);
            vec3constructor.FunctionBody = vec3constructorBlock;

            var vec2constructorBlock = AssignFieldsBlock(Vector2, x, y);
            vec2constructor.FunctionBody = vec2constructorBlock;

            var vec3fConstructorBlock = AssignFieldsBlock(Vector3f, xf, yf, zf);
            vec3fConstructor.FunctionBody = vec3fConstructorBlock;

            var vec2fConstructorBlock = AssignFieldsBlock(Vector2f, xf, yf);
            vec2fConstructor.FunctionBody = vec2fConstructorBlock;

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
