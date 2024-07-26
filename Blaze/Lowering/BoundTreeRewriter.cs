using Blaze.Binding;
using Blaze.Diagnostics;
using System.Collections.Immutable;

namespace Blaze.Lowering
{
    internal abstract class BoundTreeRewriter
    {
        public virtual BoundStatement RewriteStatement(BoundStatement node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.NopStatement:
                    return RewriteNopStatement((BoundNopStatement)node);
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
                case BoundNodeKind.DoWhileStatement:
                    return RewriteDoWhileStatement((BoundDoWhileStatement)node);
                case BoundNodeKind.ForStatement:
                    return RewriteForStatement((BoundForStatement)node);
                case BoundNodeKind.ContinueStatement:
                    return RewriteContinueStatement((BoundContinueStatement)node);
                case BoundNodeKind.BreakStatement:
                    return RewriteBreakStatement((BoundBreakStatement)node);
                case BoundNodeKind.LabelStatement:
                    return RewriteLabelStatement((BoundLabelStatement)node);
                case BoundNodeKind.ConditionalGotoStatement:
                    return RewriteConditionalGotoStatement((BoundConditionalGotoStatement)node);
                case BoundNodeKind.GoToStatement:
                    return RewriteGotoStatement((BoundGotoStatement)node);
                case BoundNodeKind.ReturnStatement:
                    return RewriteReturnStatement((BoundReturnStatement)node);
                default:
                    throw new Exception($"Unexpected node {node.Kind}");
            }
        }

        protected virtual BoundStatement RewriteBreakStatement(BoundBreakStatement node) => node;
        protected virtual BoundStatement RewriteContinueStatement(BoundContinueStatement node) => node;

        protected virtual BoundStatement RewriteNopStatement(BoundNopStatement node) => node;

        protected virtual BoundStatement RewriteDoWhileStatement(BoundDoWhileStatement node)
        {
            var statement = RewriteStatement(node.Body);
            var condition = RewriteExpression(node.Condition);

            if (statement == node.Body && condition == node.Condition)
                return node;

            return new BoundDoWhileStatement(statement, condition, node.BreakLabel, node.ContinueLabel);
        }

        protected virtual BoundStatement RewriteGotoStatement(BoundGotoStatement node) => node;

        protected virtual BoundStatement RewriteConditionalGotoStatement(BoundConditionalGotoStatement node)
        {
            var condition = RewriteExpression(node.Condition);
            if (condition == node.Condition)
                return node;
            return new BoundConditionalGotoStatement(node.Label, condition);
        }

        protected virtual BoundStatement RewriteLabelStatement(BoundLabelStatement node) => node;

        protected virtual BoundStatement RewriteBlockStatement(BoundBlockStatement node)
        {
            ImmutableArray<BoundStatement>.Builder? builder = null;
            
            for (int i = 0; i < node.Statements.Length; i++)
            {
                var oldStatement = node.Statements[i];
                var rewriten = RewriteStatement(oldStatement);
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

        protected virtual BoundStatement RewriteReturnStatement(BoundReturnStatement node)
        {
            if (node.Expression == null)
                return node;

            var rewrittenExression = RewriteExpression(node.Expression);
            if (node.Expression == rewrittenExression)
                return node;

            return new BoundReturnStatement(rewrittenExression);
        }

        protected virtual BoundStatement RewriteExpressionStatement(BoundExpressionStatement node)
        {
            var expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
                return node;
            return new BoundExpressionStatement(expression);
        }
    
        protected virtual BoundStatement RewriteVariableDeclarationStatement(BoundVariableDeclarationStatement node)
        {
            var initializer = RewriteExpression(node.Initializer);
            if (initializer == node.Initializer)
                return node;

            return new BoundVariableDeclarationStatement(node.Variable, initializer);
        }

        protected virtual BoundStatement RewriteIfStatement(BoundIfStatement node)
        {
            var condition = RewriteExpression(node.Condition);
            var body = RewriteStatement(node.Body);
            var elseBody = node.ElseBody == null ? null : RewriteStatement(node.ElseBody);

            if (condition == node.Condition && body == node.Body && elseBody == node.ElseBody)
                return node;

            return new BoundIfStatement(condition, body, elseBody);
        }

        protected virtual BoundStatement RewriteWhileStatement(BoundWhileStatement node)
        {
            var condition = RewriteExpression(node.Condition);
            var body = RewriteStatement(node.Body);

            if (condition == node.Condition && body == node.Body)
                return node;

            return new BoundWhileStatement(condition, body, node.BreakLabel, node.ContinueLabel);
        }

        protected virtual BoundStatement RewriteForStatement(BoundForStatement node)
        {
            var lowerBound = RewriteExpression(node.LowerBound);
            var upperBound = RewriteExpression(node.UpperBound);
            var body = RewriteStatement(node.Body);
            if (lowerBound == node.LowerBound && upperBound == node.UpperBound && body == node.Body)
                return node;

            return new BoundForStatement(node.Variable, lowerBound, upperBound, body, node.BreakLabel, node.ContinueLabel);
        }

        public virtual BoundExpression RewriteExpression(BoundExpression node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.ErrorExpression:
                    return RewriteErrorExpression((BoundErrorExpression)node);
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
                case BoundNodeKind.CompoundAssignmentExpression:
                    return RewriteCompoundAssignmentExpression((BoundCompoundAssignmentExpression)node);
                case BoundNodeKind.CallExpression:
                    return RewriteCallExpression((BoundCallExpression)node);
                case BoundNodeKind.ConversionExpression:
                    return RewriteConversionExpression((BoundConversionExpression)node);
                case BoundNodeKind.IncrementExpression:
                    return RewriteIncrementExpression((BoundIncrementExpression)node);
                case BoundNodeKind.ObjectCreationExpression:
                    return RewriteNewExpression((BoundObjectCreationExpression)node);
                case BoundNodeKind.FieldAccessExpression:
                    return RewriteFieldAccessExpression((BoundFieldAccessExpression)node);
                case BoundNodeKind.FunctionExpression:
                    return RewriteFunctionExpression((BoundFunctionExpression)node);
                case BoundNodeKind.MethodAccessExpression:
                    return RewriteMethodAccessExpression((BoundMethodAccessExpression)node);
                default:
                    throw new Exception($"Unexpected node {node.Kind}");
            }
        }

        protected virtual BoundExpression RewriteFieldAccessExpression(BoundFieldAccessExpression node)
        {
            var rewrittenInstanceExpression = RewriteExpression(node.Instance);
            if (rewrittenInstanceExpression == node.Instance)
                return node;
            return new BoundFieldAccessExpression(rewrittenInstanceExpression, node.Field);
        }
       
        protected virtual BoundExpression RewriteFunctionExpression(BoundFunctionExpression node) => node;

        protected virtual BoundExpression RewriteMethodAccessExpression(BoundMethodAccessExpression node)
        {
            var rewrittenAccessed = node.Instance;
            if (rewrittenAccessed == node.Instance)
                return node;
            return new BoundMethodAccessExpression(rewrittenAccessed, node.Method);
        }

        protected virtual BoundExpression RewriteIncrementExpression(BoundIncrementExpression node)
        {
            return node;
        }

        protected virtual BoundExpression RewriteConversionExpression(BoundConversionExpression node)
        {
            var expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
                return node;
            return new BoundConversionExpression(node.Type, expression);
        }

        protected virtual BoundExpression RewriteNewExpression(BoundObjectCreationExpression node)
        {
            var builder = RewriteArguments(node.Arguments);
            if (builder == null)
                return node;
            return new BoundObjectCreationExpression(node.NamedType, builder.MoveToImmutable());
        }

        protected virtual BoundExpression RewriteErrorExpression(BoundErrorExpression node) => node;

        protected virtual BoundExpression RewriteLiteralExpression(BoundLiteralExpression node) => node;

        protected virtual BoundExpression RewriteVariableExpression(BoundVariableExpression node) => node;

        protected virtual BoundExpression RewriteUnaryExpression(BoundUnaryExpression node)
        {
            var operand = RewriteExpression(node.Operand);
            if (operand == node.Operand)
                return node;

            return new BoundUnaryExpression(node.Operator, operand);
        }

        protected virtual BoundExpression RewriteBinaryExpression(BoundBinaryExpression node)
        {
            var left = RewriteExpression(node.Left);
            var right = RewriteExpression(node.Right);

            if (left == node.Left && right == node.Right)
                return node;

            return new BoundBinaryExpression(left, node.Operator, right);
        }

        protected virtual BoundExpression RewriteAssignmentExpression(BoundAssignmentExpression node)
        {
            var left = RewriteExpression(node.Left);
            var right = RewriteExpression(node.Right);

            if (right == node.Right && left == node.Left)
                return node;

            return new BoundAssignmentExpression(left, right);
        }

        protected virtual BoundExpression RewriteCompoundAssignmentExpression(BoundCompoundAssignmentExpression node)
        {
            var left = RewriteExpression(node.Left);
            var right = RewriteExpression(node.Right);

            if (right == node.Right && left == node.Left)
                return node;

            return new BoundCompoundAssignmentExpression(left, node.Operator, right);
        }

        protected virtual BoundExpression RewriteCallExpression(BoundCallExpression node)
        {
            var identifier = RewriteExpression(node.Identifier);
            var builder = RewriteArguments(node.Arguments);
            if (builder == null)
                return node;
            return new BoundCallExpression(identifier, node.Function, builder.MoveToImmutable());
        }

        private ImmutableArray<BoundExpression>.Builder? RewriteArguments(ImmutableArray<BoundExpression> arguments)
        {
            ImmutableArray<BoundExpression>.Builder? builder = null;

            for (int i = 0; i < arguments.Length; i++)
            {
                var oldArgument = arguments[i];
                var rewritenArgument = RewriteExpression(oldArgument);
                if (rewritenArgument != oldArgument)
                {
                    if (builder == null)
                    {
                        builder = ImmutableArray.CreateBuilder<BoundExpression>(arguments.Length);
                        for (int j = 0; j < i; j++)
                            builder.Add(arguments[j]);
                    }
                }
                if (builder != null)
                    builder.Add(rewritenArgument);
            }
            return builder;
        }
    }
}
