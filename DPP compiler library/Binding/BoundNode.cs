namespace DPP_Compiler.Binding
{
    internal abstract class BoundNode
    {
        public abstract BoundNodeKind Kind { get; }

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