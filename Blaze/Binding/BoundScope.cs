using System.Collections.Immutable;
using Blaze.Symbols;

namespace Blaze.Binding
{
    public sealed class BoundScope
    {
        private Dictionary<string, VariableSymbol>? _variables;

        public BoundScope? Parent { get; private set; }

        public BoundScope(BoundScope? parent)
        {
            Parent = parent;
        }

        public bool TryDeclareVariable(VariableSymbol variable)
        {
            if (_variables == null)
                _variables = new Dictionary<string, VariableSymbol>();

            if (_variables.ContainsKey(variable.Name)) return false;
            _variables.Add(variable.Name, variable);
            return true;
        }

        public VariableSymbol? TryLookupVariable(string name)
        {
            VariableSymbol? variable = null;

            if (_variables != null && _variables.TryGetValue(name, out variable))
                return variable;

            if (Parent != null)
                return Parent.TryLookupVariable(name);

            return variable;
        }

        public ImmutableArray<VariableSymbol> GetDeclaredVariables()
        {
            if (_variables == null) return ImmutableArray<VariableSymbol>.Empty;
            return _variables.Values.ToImmutableArray();
        }
    }
}