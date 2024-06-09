using System.Collections.Immutable;
using Blaze.Symbols;

namespace Blaze.Binding
{
    public sealed class BoundScope
    {
        private Dictionary<string, VariableSymbol>? _variables;
        private Dictionary<string, FunctionSymbol>? _functions;

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

        public bool TryDeclareFunction(FunctionSymbol function)
        {
            if (_functions == null)
                _functions = new Dictionary<string, FunctionSymbol>();

            if (_functions.ContainsKey(function.Name.ToLower())) return false;
            _functions.Add(function.Name.ToLower(), function);
            return true;
        }

        public FunctionSymbol? TryLookupFunction(string name)
        {
            FunctionSymbol? function = null;

            if (_functions != null && _functions.TryGetValue(name.ToLower(), out function))
                return function;

            if (Parent != null)
                return Parent.TryLookupFunction(name.ToLower());

            return function;
        }

        public ImmutableArray<VariableSymbol> GetDeclaredVariables()
        {
            if (_variables == null) return ImmutableArray<VariableSymbol>.Empty;
            return _variables.Values.ToImmutableArray();
        }

        public ImmutableArray<FunctionSymbol> GetDeclaredFunctions()
        {
            if (_functions == null) return ImmutableArray<FunctionSymbol>.Empty;
            return _functions.Values.ToImmutableArray();
        }
    }
}