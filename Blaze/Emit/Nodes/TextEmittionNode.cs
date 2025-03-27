namespace Blaze.Emit.Nodes
{
    public abstract class TextEmittionNode : EmittionNode
    {
        public abstract string Text { get; }
        public abstract bool IsSingleLine { get; }

        public TextEmittionNode() : base() { }
    }

}
