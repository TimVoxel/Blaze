namespace Blaze.Symbols.BuiltIn
{
    internal sealed class MathNamespace : BuiltInNamespace
    {
        public FunctionSymbol NegateFloat { get; }
        public FunctionSymbol NegateDouble { get; }
        public FunctionSymbol Add { get; }
        public FunctionSymbol Subtract { get; }
        public FunctionSymbol Multiply { get; }
        public FunctionSymbol Divide { get; }
        public FunctionSymbol PositionY { get; }

        public FunctionSymbol ToDouble { get; }
        public FunctionSymbol ToFloat { get; }

        public MathNamespace(BlazeNamespace parent) : base("math", parent)
        {
            NegateFloat = PrivateFunction("negate_float", TypeSymbol.Float, Parameter("sign", TypeSymbol.String), Parameter("a", TypeSymbol.Float));
            NegateDouble = PrivateFunction("negate_double", TypeSymbol.Double, Parameter("sign", TypeSymbol.String), Parameter("a", TypeSymbol.Double));
            Add = PrivateFunction("add", TypeSymbol.Double, Parameter("a", TypeSymbol.Double), Parameter("b", TypeSymbol.Double));
            Subtract = PrivateFunction("subtract", TypeSymbol.Double, Parameter("a", TypeSymbol.Double), Parameter("b", TypeSymbol.Double));
            Multiply = PrivateFunction("multiply", TypeSymbol.Double, Parameter("a", TypeSymbol.Double), Parameter("b", TypeSymbol.Double));
            Divide = PrivateFunction("divide", TypeSymbol.Double, Parameter("a", TypeSymbol.Double), Parameter("b", TypeSymbol.Double));
            PositionY = PrivateFunction("position_y", TypeSymbol.Void, Parameter("a", TypeSymbol.Double));

            ToDouble = PrivateFunction("to_double", TypeSymbol.Double, Parameter("a", TypeSymbol.Float));
            ToFloat = PrivateFunction("to_float", TypeSymbol.Float, Parameter("a", TypeSymbol.Double));
        }
    }
}
