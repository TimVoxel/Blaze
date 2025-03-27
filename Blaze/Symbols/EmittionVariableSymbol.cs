using Blaze.Emit;

namespace Blaze.Symbols
{
    public class EmittionVariableSymbol : VariableSymbol
    {
        public override SymbolKind Kind => SymbolKind.EmittionVariable;

        public string SaveName
        {
            get
            {
                return $"*{ScopedName}";    
            }
        } 

        public string ScopedName
        {
            get
            {
                if (ScopeIndex == null || ScopeIndex == 0)
                    return Name;
                else
                    return $"{Name}{ScopeIndex}";

            }
        }

        public DataLocation Location { get; }
        public bool IsTemp { get; }
        public int? ScopeIndex { get; }

        public EmittionVariableSymbol(string name, TypeSymbol type, bool isTemp, int? scopeIndex = null, DataLocation? location = null) : base(name, type, false, null)
        {
            Location = location ?? EmittionFacts.ToLocation(type);
            IsTemp = isTemp;
            ScopeIndex = scopeIndex;
        }
    }
}
