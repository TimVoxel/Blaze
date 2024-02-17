namespace Blaze.Binding
{
    internal sealed class BoundConstant
    {
        public object Value { get; private set; }

        public BoundConstant(object value)
        {
            Value = value;
        }
    }
}