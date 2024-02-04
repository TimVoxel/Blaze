namespace Blaze.Binding
{
    internal abstract class BoundLoopStatement : BoundStatement
    {
        public BoundStatement Body { get; private set; }
        public BoundLabel BreakLabel { get; private set; }
        public BoundLabel ContinueLabel { get; private set; }

        public BoundLoopStatement(BoundStatement body, BoundLabel breakLabel, BoundLabel continueLabel)
        {
            Body = body;
            BreakLabel = breakLabel;
            ContinueLabel = continueLabel;
        }
    }
}