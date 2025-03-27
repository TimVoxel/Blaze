using Blaze.Emit.Data;
using System.Diagnostics;

namespace Blaze.Emit.Nodes
{
    public class ForceloadCommand : CommandNode
    {
        public enum SubAction
        {
            Add,
            Remove,
            Query
        }

        public SubAction Action { get; }
        public Coordinates2? Start { get; }
        public Coordinates2? End { get; }

        public bool? RemoveAll { get; }

        public override string Keyword => "forceload";
        public override EmittionNodeKind Kind => EmittionNodeKind.ForceloadCommand;

        public override string Text
        {
            get
            {
                if (Action == SubAction.Query)
                {
                    if (Start != null)
                        return $"{Keyword} {Action.ToString().ToLower()} {Start.X} {Start.Z}";
                    else
                        return $"{Keyword} {Action.ToString().ToLower()}";
                }
                else
                {
                    Debug.Assert(Start != null);

                    if (End != null)
                        return $"{Keyword} {Action.ToString().ToLower()} {Start.X} {Start.Z} {End.X} {End.Z}";
                    else
                    {
                        if (Action == SubAction.Remove)
                        {
                            if (RemoveAll != null && (bool)RemoveAll)
                                return $"{Keyword} {Action.ToString().ToLower()} all";
                            else
                                return $"{Keyword} {Action.ToString().ToLower()} {Start.X} {Start.Z}";
                        }
                        else
                        {
                            Debug.Assert(RemoveAll == null);
                            return $"{Keyword} {Action.ToString().ToLower()} {Start.X} {Start.Z}";
                        }
                    }
                }
            }
        }

        public ForceloadCommand(SubAction action, Coordinates2? start, Coordinates2? end = null, bool? removeAll = null)
        {
            Action = action;
            Start = start;
            End = end;
            RemoveAll = removeAll;
        }
    }
}
