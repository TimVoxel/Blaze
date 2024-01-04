namespace Compiler_snapshot.Binding
{
    internal abstract class BoundNode
    {
        public abstract BoundNodeKind Kind { get; }

    }

    internal abstract class BoundExpression : BoundNode
    {
        public abstract Type Type { get; }
    }
}