namespace Blaze.Symbols.BuiltIn
{
    internal sealed class BlazeNamespace : BuiltInNamespace 
    {
        public MathNamespace Math { get; }
        public BlazeFabricatedNamespace Fabricated { get; }

        public FunctionSymbol AssignStSt { get; }
        public FunctionSymbol AssignStSc { get; }

        public FunctionSymbol StrConcat { get; }

        public BlazeNamespace() : base("blaze")
        {
            Math = new MathNamespace(this);
            Fabricated = new BlazeFabricatedNamespace(this);

            AssignStSt = PrivateFunction("assign_st_st", TypeSymbol.Void);
            AssignStSc = PrivateFunction("assign_st_sc", TypeSymbol.Void);
            StrConcat = PrivateFunction("str_concat", TypeSymbol.String);
        }
    }
}
