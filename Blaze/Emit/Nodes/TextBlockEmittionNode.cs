using System.Collections.Immutable;
using System.Text;

namespace Blaze.Emit.Nodes
{
    public class TextBlockEmittionNode : TextEmittionNode
    {
        public ImmutableArray<TextEmittionNode> Lines { get; }
        public override string Text
        {
            get
            {
                var builder = new StringBuilder();

                foreach (var node in Lines)
                    builder.AppendLine(node.Text);

                return builder.ToString();
            }
        }

        public override bool IsSingleLine => Lines.Length <= 1;
        public override EmittionNodeKind Kind => EmittionNodeKind.TextBlock;

        public TextBlockEmittionNode(ImmutableArray<TextEmittionNode> lines)
        {
            Lines = lines;
        }
    }
}
