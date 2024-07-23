namespace Blaze.Binding
{
    public sealed class BoundConstant
    {
        public object Value { get; private set; }

        public BoundConstant(object value)
        {
            Value = value;
        }
    }
}