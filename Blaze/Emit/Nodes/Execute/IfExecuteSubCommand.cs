namespace Blaze.Emit.Nodes.Execute
{
    public class IfExecuteSubCommand : ExecuteSubCommand 
    {
        public ExecuteConditionalClause ConditionClause { get; }

        public override string Text => $"if {ConditionClause.Text}";
        public override ExecuteSubCommandKind Kind => ExecuteSubCommandKind.If;

        public IfExecuteSubCommand(ExecuteConditionalClause conditionalClause)
        {
            ConditionClause = conditionalClause;
        }

    }
}
