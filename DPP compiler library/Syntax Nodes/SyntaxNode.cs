using DPP_Compiler.SyntaxTokens;
using DPP_Compiler.Text;
using System.Reflection;

namespace DPP_Compiler.Syntax_Nodes
{
    public abstract class SyntaxNode
    {
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

        public abstract IEnumerable<SyntaxNode> GetChildren();

        /* WTF??????
        {
            PropertyInfo[] properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in properties)
            {
                if (typeof(SyntaxNode).IsAssignableFrom(property.PropertyType))
                {
                    SyntaxNode? node = (SyntaxNode?) property.GetValue(this);
                    if (node != null)
                        yield return node;
                }
                else if (typeof(IEnumerable<SyntaxNode>).IsAssignableFrom(property.PropertyType))
                {
                    IEnumerable<SyntaxNode>? children = (IEnumerable<SyntaxNode>?) property.GetValue(this);
                    if (children != null)
                    {
                        foreach (SyntaxNode child in children)
                            yield return child;
                    }
                }
            }
        }
        */

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

    }
}
