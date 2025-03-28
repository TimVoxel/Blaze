using System.Diagnostics;

namespace Blaze.Emit.Nodes
{
    public class TagCommand : CommandNode
    {
        public enum SubAction
        {
            Add,
            Remove,
            List
        }
        public string Selector { get; }
        public SubAction Action { get; }
        public string? TagName { get; }

        public override string Keyword => "tag";
        public override EmittionNodeKind Kind => EmittionNodeKind.TagCommand;

        public override string Text
        {
            get
            {
                if (Action == SubAction.List)
                    return $"{Keyword} {Selector} {Action.ToString().ToLower()}";

                Debug.Assert(TagName != null);
                return $"{Keyword} {Selector} {Action.ToString().ToLower()} {TagName}";
            }
        }

        public TagCommand(string selector, SubAction action, string? tagName = null)
        {
            Selector = selector;
            Action = action;
            TagName = tagName;
        }
    }
}
