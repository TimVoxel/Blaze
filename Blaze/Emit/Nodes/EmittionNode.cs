namespace Blaze.Emit.Nodes
{
    public abstract class EmittionNode
    {
        public abstract EmittionNodeKind Kind { get; }

        public EmittionNode() { }

        public override string ToString()
        {
            using (StringWriter writer = new StringWriter())
            {
                this.WriteTo(writer);
                return writer.ToString();
            }
        }
    }
}
