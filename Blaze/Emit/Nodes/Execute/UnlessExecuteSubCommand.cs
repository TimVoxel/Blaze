namespace Blaze.Emit.Nodes.Execute
{
    public class UnlessExecuteSubCommand : ExecuteSubCommand
    {
        public ExecuteConditionalClause ConditionClause { get; }

        public override string Text => $"unless {ConditionClause.Text}";
        public override ExecuteSubCommandKind Kind => ExecuteSubCommandKind.Unless;

        public UnlessExecuteSubCommand(ExecuteConditionalClause conditionalClause)
        {
            ConditionClause = conditionalClause;
        }
    }
}
