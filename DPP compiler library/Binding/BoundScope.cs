using System.Collections.Immutable;

namespace DPP_Compiler.Binding
{
    internal sealed class BoundScope
    {
        private readonly Dictionary<string, VariableSymbol> _variables = new Dictionary<string, VariableSymbol>();

        public BoundScope? Parent { get; private set; }

        public BoundScope(BoundScope? parent)
        {
            Parent = parent;
        }

        public bool TryDeclare(VariableSymbol variable)
        {
            if (_variables.ContainsKey(variable.Name)) return false;
            _variables.Add(variable.Name, variable);
            return true;
        }

        public bool TryLookup(string name, out VariableSymbol? variable)
        {
            if (_variables.TryGetValue(name, out variable))
                return true;

            if (Parent == null)
                return false;

            return Parent.TryLookup(name, out variable);
        }
    
        public ImmutableArray<VariableSymbol> GetDeclaredVariables() => _variables.Values.ToImmutableArray();
    }
}