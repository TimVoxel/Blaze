using Blaze.Emit.Nodes;

namespace Blaze.Symbols
{
    public class EmittionVariableSymbol : VariableSymbol
    {
        public enum EmittionVariableLocation
        {
            Scoreboard,
            Storage
        }

        public override SymbolKind Kind => SymbolKind.EmittionVariable;

        public string SaveName => $"*{Name}";
        public EmittionVariableLocation Location { get; }
        public bool IsTemp { get; }

        public EmittionVariableSymbol(string name, TypeSymbol type, bool isTemp, EmittionVariableLocation? location = null) : base(name, type, false, null)
        {
            Location = location ?? (
                    type == TypeSymbol.Int || type == TypeSymbol.Bool || type is EnumSymbol e && e.IsIntEnum
                        ? EmittionVariableLocation.Scoreboard
                        : EmittionVariableLocation.Storage
                );

            IsTemp = isTemp;
        }
    }
}
