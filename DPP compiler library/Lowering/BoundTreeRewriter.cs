using DPP_Compiler.Binding;
using System.Collections.Immutable;

namespace DPP_Compiler.Lowering
{
    internal abstract class BoundTreeRewriter
    {
        public virtual BoundStatement RewriteStatement(BoundStatement node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.BlockStatement:
                    return RewriteBlockStatement((BoundBlockStatement)node);
                case BoundNodeKind.ExpressionStatement:
                    return RewriteExpressionStatement((BoundExpressionStatement)node);
                case BoundNodeKind.VariableDeclarationStatement:
                    return RewriteVariableDeclarationStatement((BoundVariableDeclarationStatement)node);
                case BoundNodeKind.IfStatement:
                    return RewriteIfStatement((BoundIfStatement)node);
                case BoundNodeKind.WhileStatement:
                    return RewriteWhileStatement((BoundWhileStatement)node);
                case BoundNodeKind.ForStatement:
                    return RewriteForStatement((BoundForStatement)node);
                default:
                    throw new Exception($"Unexpected node {node.Kind}");
            }
        }

        protected virtual BoundStatement RewriteBlockStatement(BoundBlockStatement node)
        {
            ImmutableArray<BoundStatement>.Builder? builder = null;
            
            for (int i = 0; i < node.Statements.Length; i++)
            {
                BoundStatement oldStatement = node.Statements[i];
                BoundStatement rewriten = RewriteStatement(oldStatement);
                if (rewriten != oldStatement)
                {
                    if (builder == null)
                    {
                        builder = ImmutableArray.CreateBuilder<BoundStatement>(node.Statements.Length);
                        for (int j = 0; j < i; j++)
                            builder.Add(node.Statements[j]);
                    }
                }
                if (builder != null)
                    builder.Add(rewriten);   
            }
            if (builder == null)
                return node;

            return new BoundBlockStatement(builder.MoveToImmutable());
        }

        protected virtual BoundStatement RewriteExpressionStatement(BoundExpressionStatement node)
        {
            BoundExpression expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
                return node;
            return new BoundExpressionStatement(expression);
        }
    
        protected virtual BoundStatement RewriteVariableDeclarationStatement(BoundVariableDeclarationStatement node)
        {
            BoundExpression initializer = RewriteExpression(node.Initializer);
            if (initializer == node.Initializer)
                return node;

            return new BoundVariableDeclarationStatement(node.Variable, initializer);
        }

        protected virtual BoundStatement RewriteIfStatement(BoundIfStatement node)
        {
            BoundExpression condition = RewriteExpression(node.Condition);
            BoundStatement body = RewriteStatement(node.Body);
            BoundStatement? elseBody = node.ElseBody == null ? null : RewriteStatement(node.ElseBody);

            if (condition == node.Condition && body == node.Body && elseBody == node.ElseBody)
                return node;

            return new BoundIfStatement(condition, body, elseBody);
        }

        protected virtual BoundStatement RewriteWhileStatement(BoundWhileStatement node)
        {
            BoundExpression condition = RewriteExpression(node.Condition);
            BoundStatement body = RewriteStatement(node.Body);

            if (condition == node.Condition && body == node.Body)
                return node;

            return new BoundWhileStatement(condition, body);
        }

        protected virtual BoundStatement RewriteForStatement(BoundForStatement node)
        {
            BoundExpression lowerBound = RewriteExpression(node.LowerBound);
            BoundExpression upperBound = RewriteExpression(node.UpperBound);
            BoundStatement body = RewriteStatement(node.Body);
            if (lowerBound == node.LowerBound && upperBound == node.UpperBound && body == node.Body)
                return node;

            return new BoundForStatement(node.Variable, lowerBound, upperBound, body);
        }

        public virtual BoundExpression RewriteExpression(BoundExpression node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.LiteralExpression:
                    return RewriteLiteralExpression((BoundLiteralExpression)node);
                case BoundNodeKind.VariableExpression:
                    return RewriteVariableExpression((BoundVariableExpression)node);
                case BoundNodeKind.UnaryExpression:
                    return RewriteUnaryExpression((BoundUnaryExpression)node);
                case BoundNodeKind.BinaryExpression:
                    return RewriteBinaryExpression((BoundBinaryExpression)node);
                case BoundNodeKind.AssignmentExpression:
                    return RewriteAssignmentExpression((BoundAssignmentExpression)node);
                default:
                    throw new Exception($"Unexpected node {node.Kind}");
            }
        }

        protected virtual BoundExpression RewriteLiteralExpression(BoundLiteralExpression node) => node;

        protected virtual BoundExpression RewriteVariableExpression(BoundVariableExpression node) => node;

        protected virtual BoundExpression RewriteUnaryExpression(BoundUnaryExpression node)
        {
            BoundExpression operand = RewriteExpression(node.Operand);
            if (operand == node.Operand)
                return node;

            return new BoundUnaryExpression(node.Operator, operand);
        }

        protected virtual BoundExpression RewriteBinaryExpression(BoundBinaryExpression node)
        {
            BoundExpression left = RewriteExpression(node.Left);
            BoundExpression right = RewriteExpression(node.Right);

            if (left == node.Left && right == node.Right)
                return node;

            return new BoundBinaryExpression(left, node.Operator, right);
        }

        protected virtual BoundExpression RewriteAssignmentExpression(BoundAssignmentExpression node)
        {
            BoundExpression expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
                return node;

            return new BoundAssignmentExpression(node.Variable, expression);
        }
    }
}
