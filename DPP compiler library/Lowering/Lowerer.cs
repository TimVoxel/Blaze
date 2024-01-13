using DPP_Compiler.Binding;
using System.Collections.Immutable;

namespace DPP_Compiler.Lowering
{
    internal sealed class Lowerer : BoundTreeRewriter
    {
        private Lowerer() 
        { 
            
        }

        public static BoundStatement Lower(BoundStatement statement)
        {
            Lowerer lowerer = new Lowerer();
            return lowerer.RewriteStatement(statement);
        }

        protected override BoundStatement RewriteForStatement(BoundForStatement node)
        {
            BoundBinaryOperator op = BoundBinaryOperator.SafeBind(BoundBinaryOperatorKind.LessOrEquals, typeof(int), typeof(int));
            BoundBinaryOperator plusOp = BoundBinaryOperator.SafeBind(BoundBinaryOperatorKind.Addition, typeof(int), typeof(int));

            BoundVariableDeclarationStatement declarationStatement = new BoundVariableDeclarationStatement(node.Variable, node.LowerBound);
            BoundVariableExpression variableExpression = new BoundVariableExpression(node.Variable);

            BoundBinaryExpression condition = new BoundBinaryExpression(variableExpression, op, node.UpperBound);
            BoundExpressionStatement increment = new BoundExpressionStatement(new BoundAssignmentExpression(node.Variable, new BoundBinaryExpression(variableExpression, plusOp, new BoundLiteralExpression(1))));

            BoundBlockStatement whileBlock = new BoundBlockStatement(ImmutableArray.Create(node.Body, increment));
            BoundWhileStatement whileStatement = new BoundWhileStatement(condition, whileBlock);
            BoundStatement result = new BoundBlockStatement(ImmutableArray.Create<BoundStatement>(declarationStatement, whileStatement));
            return RewriteStatement(result);
        }
    }
}
