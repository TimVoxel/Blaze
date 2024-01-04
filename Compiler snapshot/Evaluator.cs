using Compiler_snapshot.Syntax_Nodes;
using Compiler_snapshot.SyntaxTokens;

namespace Compiler_snapshot
{
    public class Evaluator
    {
        private readonly ExpressionSyntax _root;

        public Evaluator(ExpressionSyntax root)
        {
            _root = root;
        }

        public int Evaluate()
        {
            return EvaluateExpression(_root);
        }

        private int EvaluateExpression(ExpressionSyntax node)
        {
            if (node is NumberExpressionSyntax number)
            {
                if (number.Token.Value == null) throw new Exception($"Empty number token detected");
                return (int)number.Token.Value;
            }
                
            if (node is BinaryExpressionSyntax binary)
            {
                ExpressionSyntax left = binary.Left;
                ExpressionSyntax right = binary.Right;

                switch (binary.Operator.Kind)
                {
                    case SyntaxKind.Plus:
                        return EvaluateExpression(left) + EvaluateExpression(right);
                    case SyntaxKind.Minus:
                        return EvaluateExpression(left) - EvaluateExpression(right);
                    case SyntaxKind.Star:
                        return EvaluateExpression(left) * EvaluateExpression(right);
                    case SyntaxKind.Slash:
                        return EvaluateExpression(left) / EvaluateExpression(right);
                    default: throw new Exception($"Unexpected binary operator {binary.Operator.Kind}");
                }
            }

            if (node is ParenthesizedExpressionSyntax paren)
                return EvaluateExpression(paren.Expression);
        
            throw new Exception($"Unexpected node {node.Kind}");
        }
    }
}
