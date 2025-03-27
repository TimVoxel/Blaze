using Blaze.Emit;

namespace Blaze.Symbols
{
    public class MacroEmittionVariableSymbol : EmittionVariableSymbol
    {
        public const string MACRO_PREFIX = "*macros";
        public string Accessor => $"$({Name})";

        public override string SaveName => $"*{MACRO_PREFIX}.{Name}";

        public MacroEmittionVariableSymbol(string name) : base(name, TypeSymbol.Object, true, null, DataLocation.Storage) { }
    }

    public class EmittionVariableSymbol : VariableSymbol
    {
        public override SymbolKind Kind => SymbolKind.EmittionVariable;

        public virtual string SaveName
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
