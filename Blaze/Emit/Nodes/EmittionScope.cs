using Blaze.Symbols;
using System.Collections.Immutable;
using static Blaze.Symbols.EmittionVariableSymbol;

namespace Blaze.Emit.Nodes
{
    public sealed class EmittionScope
    {
        private Dictionary<string, Dictionary<EmittionVariableLocation, EmittionVariableSymbol>>? _variables;
        public EmittionScope? Parent { get; }
        public int NestIndex { get; }
       
        public EmittionScope Root
        {
            get
            {
                var current = this;

                while (current.Parent != null)
                    current = current.Parent;

                return current;
            }
        }

        public EmittionScope(EmittionScope? parent)
        {
            Parent = parent;

            if (parent == null)
                NestIndex = -1;
            else
                NestIndex = parent.NestIndex + 1;
        }

        public EmittionVariableSymbol LookupOrDeclare(string name, TypeSymbol type, bool makeTemp, bool useScoping, EmittionVariableLocation? location = null)
        {
            var searchLocation = location ?? EmittionFacts.ToLocation(type);
            var variable = TryLookupVariable(name, searchLocation);

            if (variable == null)
            {
                int? nestIndex = useScoping ? NestIndex : null;
                variable = new EmittionVariableSymbol(name, type, makeTemp, nestIndex, searchLocation);
                Declare(variable);
            }
            return variable;
        }

        public EmittionVariableSymbol Declare(string name, TypeSymbol type, bool makeTemp, EmittionVariableLocation? location = null)
        {
            var variable = new EmittionVariableSymbol(name, type, makeTemp, NestIndex, location);
            Declare(variable);
            return variable;
        }

        public void Declare(EmittionVariableSymbol emittionVariable)
        {
            if (_variables == null)
                _variables = new Dictionary<string, Dictionary<EmittionVariableLocation, EmittionVariableSymbol>>();

            if (!_variables.ContainsKey(emittionVariable.Name))
            {
                _variables.Add(emittionVariable.Name, new Dictionary<EmittionVariableLocation, EmittionVariableSymbol>());
                _variables[emittionVariable.Name][emittionVariable.Location] = emittionVariable;
            }
            else
            {
                if (!_variables[emittionVariable.Name].ContainsKey(emittionVariable.Location))
                    _variables[emittionVariable.Name][emittionVariable.Location] = emittionVariable;
            }
        }

        public EmittionVariableSymbol? TryLookupVariable(string name, EmittionVariableLocation location)
        {
            if (_variables != null)
                if (_variables.TryGetValue(name, out var variables) && variables.TryGetValue(location, out var variable))
                    return variable;

            return Parent?.TryLookupVariable(name, location);
        }

        public ImmutableArray<EmittionVariableSymbol> GetLocals()
        {
            if (_variables == null)
                return ImmutableArray<EmittionVariableSymbol>.Empty;

            return _variables.Values.SelectMany(t => t.Values).ToImmutableArray();
        }
    }

    /*

    public class ScoreboardPlayersCommand : ScoreboardCommand
    {
        enum SubAction 
        {
            Add,
            Remove,
            Reset
            
        }
           
        internal ScoreboardPlayersCommand()
        {

        }
    }*/
}
