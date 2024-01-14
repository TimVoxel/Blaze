using System.Reflection;

namespace DPP_Compiler.Binding
{
    internal abstract class BoundNode
    {
        public abstract BoundNodeKind Kind { get; }
        public abstract IEnumerable<BoundNode> GetChildren();

        public void WriteTo(TextWriter writer)
        {
            PrettyPrint(writer, this);
        }

        private static ConsoleColor GetColour(BoundNode node)
        {
            if (node is BoundExpression)
                return ConsoleColor.Blue;
            if (node is BoundStatement)
                return ConsoleColor.Green;

            return ConsoleColor.Yellow;
        }

        private static void PrettyPrint(TextWriter writer, BoundNode node, string indent = "", bool isLast = true)
        {
            bool isToConsole = writer == Console.Out;
            string marker = isLast ? "└──" : "├-─";

            if (isToConsole)
                Console.ForegroundColor = ConsoleColor.Gray;

            writer.Write(indent);
            writer.Write(marker);

            if (isToConsole)
                Console.ForegroundColor = GetColour(node);

            writer.Write(GetText(node) + " ");

            bool isFirstProperty = true;
            foreach (var property in node.GetProperties())
            {
                if (isFirstProperty)
                    isFirstProperty = false;
                else
                {
                    if (isToConsole)
                        Console.ForegroundColor = ConsoleColor.Gray;
                    writer.Write(", ");
                }

                if (isToConsole)
                    Console.ForegroundColor = ConsoleColor.Yellow;
                writer.Write(property.Name);
                if (isToConsole)
                    Console.ForegroundColor = ConsoleColor.Gray;
                writer.Write(" = ");
                if (isToConsole)
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                writer.Write(property.Value);
            }

            if (isToConsole)
                Console.ResetColor();

            writer.WriteLine();
            indent += isLast ? "   " : "│  ";

            BoundNode? last = node.GetChildren().LastOrDefault();
            foreach (BoundNode child in node.GetChildren())
                PrettyPrint(writer, child, indent, child == last);
        }


        private IEnumerable<(string Name, object Value)> GetProperties()
        {
            PropertyInfo[] properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (PropertyInfo property in properties)
            {
                if (property.Name == nameof(Kind) || property.Name == nameof(BoundBinaryExpression.Operator))
                    continue;

                if (typeof(BoundNode).IsAssignableFrom(property.PropertyType) || typeof(IEnumerable<BoundNode>).IsAssignableFrom(property.PropertyType))
                    continue;

                object? value = property.GetValue(this);
                if (value != null)
                    yield return (property.Name, value);
            }
        }

        private static string GetText(BoundNode node)
        {
            if (node is BoundBinaryExpression b)
                return b.Operator.OperatorKind.ToString() + " Expression";

            if (node is BoundUnaryExpression u)
                return u.Operator.OperatorKind.ToString() + " Expression";

            return node.Kind.ToString();
        }
    }
}