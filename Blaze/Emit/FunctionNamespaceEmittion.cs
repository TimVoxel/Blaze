using System.Collections.Immutable;

namespace Blaze.Emit
{
    public class FunctionNamespaceEmittion : Emittion
    {
        public ImmutableArray<FunctionNamespaceEmittion> Children { get; }
        public ImmutableArray<FunctionEmittion> Functions { get; }

        public FunctionNamespaceEmittion(string name, ImmutableArray<FunctionNamespaceEmittion> children, ImmutableArray<FunctionEmittion> functions) : base(name)
        {
            Children = children;
            Functions = functions;
        }
    }
}
