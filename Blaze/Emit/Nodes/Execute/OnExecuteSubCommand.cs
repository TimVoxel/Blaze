namespace Blaze.Emit.Nodes.Execute
{
    public class OnExecuteSubCommand : ExecuteSubCommand
    {
        public enum RelationType
        {
            Attacker,
            Controller,
            Leasher,
            Origin,
            Owner,
            Passengers,
            Target,
            Vehicle
        }

        public RelationType Relation { get; }

        public override string Text => $"on {Relation}";
        public override ExecuteSubCommandKind Kind => ExecuteSubCommandKind.On;

        public OnExecuteSubCommand(RelationType relation)
        {
            Relation = relation;
        }
    }

}
