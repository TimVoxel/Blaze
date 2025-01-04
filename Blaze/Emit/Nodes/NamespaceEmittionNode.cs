using Blaze.Symbols;
using System.Collections.Immutable;

namespace Blaze.Emit.Nodes
{
    public sealed class NamespaceEmittionNode : StructureEmittionNode
    {
        public override EmittionNodeKind Kind => EmittionNodeKind.Namespace;

        public ImmutableArray<StructureEmittionNode> Children { get; }

        public IEnumerable<MinecraftFunction> Functions => Children.OfType<MinecraftFunction>();
        public IEnumerable<NamespaceEmittionNode> NestedNamespaces => Children.OfType<NamespaceEmittionNode>();

        public NamespaceEmittionNode(IMemberSymbol symbol, string name, ImmutableArray<StructureEmittionNode> children) : base(symbol, name) 
        {
            Children = children;
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
