using Blaze.Binding;
using System.Collections.Immutable;

namespace Blaze.Symbols
{
    public sealed class ConstructorSymbol : FunctionSymbol
    {
        internal BoundBlockStatement? FunctionBody { get; set; }

        public override string AddressName => string.Empty;
        
        public ConstructorSymbol(ImmutableArray<ParameterSymbol> parameters)
            : base(string.Empty, null, parameters, TypeSymbol.Void, null)
        {
        }
    }
}
