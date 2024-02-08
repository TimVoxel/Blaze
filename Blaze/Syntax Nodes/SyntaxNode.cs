using Blaze.SyntaxTokens;
using Blaze.Text;

namespace Blaze.Syntax_Nodes
{
    public abstract class SyntaxNode
    {
        public SyntaxTree Tree { get; private set; }
        public abstract SyntaxKind Kind { get; }

        public virtual TextSpan Span
        {
            get
            {
                TextSpan first = GetChildren().First().Span;
                TextSpan last = GetChildren().Last().Span;
                return TextSpan.FromBounds(first.Start, last.End);
            }
        }
        public virtual TextLocation Location => new TextLocation(Tree.Text, Span);

        protected SyntaxNode(SyntaxTree tree)
        {
            Tree = tree;
        }

        public abstract IEnumerable<SyntaxNode> GetChildren();

        public SyntaxToken GetLastToken()
        {
            if (this is SyntaxToken token)
                return token;

            return GetChildren().Last().GetLastToken();
        }

        public void WriteTo(TextWriter writer)
        {
            PrettyPrint(writer, this);
        }

        public static void PrettyPrint(TextWriter writer, SyntaxNode node, string indent = "", bool isLast = true)
        {
            bool isToConsole = writer == Console.Out;
            string marker = isLast ? "└──" : "├-─";

            if (isToConsole)
                Console.ForegroundColor = ConsoleColor.DarkGray;

            writer.Write(indent);
            writer.Write(marker);

            if (isToConsole)
                Console.ResetColor();

            if (isToConsole)
                Console.ForegroundColor = node is SyntaxToken ? ConsoleColor.Blue : ConsoleColor.Cyan;

            writer.Write(node.Kind);

            if (isToConsole)
                Console.ResetColor();

            if (node is SyntaxToken t && t.Value != null)
                writer.Write(" " + t.Value);
            
            writer.WriteLine();

            indent += isLast ? "   " : "│  ";

            var last = node.GetChildren().LastOrDefault();

            foreach (SyntaxNode child in node.GetChildren())
                PrettyPrint(writer, child, indent, child == last);
        }

        public override string ToString()
        {
            using (StringWriter writer = new StringWriter())
            {
                WriteTo(writer);
                return writer.ToString();
            }
        }
    }

    public abstract class ExpressionSyntax : SyntaxNode
    {
        public ExpressionSyntax(SyntaxTree tree) : base(tree) { }
    }
}
