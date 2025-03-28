using Blaze.Symbols;
using System.Collections.Immutable;

namespace Blaze.Emit.Nodes
{
    public sealed class NamespaceEmittionNode : StructureEmittionNode
    {
        public override EmittionNodeKind Kind => EmittionNodeKind.Namespace;

        public MinecraftFunction? LoadFunction { get; }
        public MinecraftFunction? TickFunction { get; }

        public ImmutableArray<StructureEmittionNode> Children { get; }
        public IEnumerable<MinecraftFunction> Functions => Children.OfType<MinecraftFunction>();
        public IEnumerable<NamespaceEmittionNode> NestedNamespaces => Children.OfType<NamespaceEmittionNode>();

        public NamespaceEmittionNode(IMemberSymbol symbol, string name, ImmutableArray<StructureEmittionNode> children, MinecraftFunction? loadFunction, MinecraftFunction? tickFunction) : base(symbol, name) 
        {
            Children = children;
            LoadFunction = loadFunction;
            TickFunction = tickFunction;
        }
    }
}
