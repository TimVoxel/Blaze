using System.Reflection;

namespace DPP_Compiler.Syntax_Nodes
{
    public abstract class SyntaxNode
    {
        public abstract SyntaxKind Kind { get; }

        public IEnumerable<SyntaxNode> GetChildren()
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
    }

    public abstract class ExpressionSyntax : SyntaxNode
    {

    }
}
