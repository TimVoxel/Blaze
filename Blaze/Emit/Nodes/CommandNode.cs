namespace Blaze.Emit.Nodes
{
    public abstract class CommandNode : TextEmittionNode
    {
        public override bool IsSingleLine => true;
        public abstract string Keyword { get; }
    }
}
