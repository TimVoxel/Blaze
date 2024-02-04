using Blaze.Binding;
using Blaze.Symbols;
using System.Collections.Immutable;

namespace Blaze.Lowering
{
    internal sealed class Lowerer : BoundTreeRewriter
    {
        private int _labelCount;

        private Lowerer() 
        {
            _labelCount = 0;
        }

        public static BoundBlockStatement Lower(BoundStatement statement)
        {
            Lowerer lowerer = new Lowerer();
            BoundStatement result = lowerer.RewriteStatement(statement);
            return Flatten(result);
        }

        private static BoundBlockStatement Flatten(BoundStatement statement)
        {
            ImmutableArray<BoundStatement>.Builder builder = ImmutableArray.CreateBuilder<BoundStatement>();
            Stack<BoundStatement> stack = new Stack<BoundStatement>();
            stack.Push(statement);

            while (stack.Count > 0)
            {
                BoundStatement current = stack.Pop();
                if (current is BoundBlockStatement block)
                {
                    foreach (BoundStatement subStatement in block.Statements.Reverse())
                        stack.Push(subStatement);
                }
                else
                    builder.Add(current);
            }
            return new BoundBlockStatement(builder.ToImmutable());
        }

        private BoundLabel GenerateLabel()
        {
            var name = $"label{++_labelCount}";
            return new BoundLabel(name);
        }

        protected override BoundStatement RewriteIfStatement(BoundIfStatement node)
        {
            if (node.ElseBody == null)
            {
                BoundLabel endLabel = GenerateLabel();
                BoundConditionalGotoStatement gotoFalse = new BoundConditionalGotoStatement(endLabel, node.Condition, true);
                BoundLabelStatement endLabelStatement = new BoundLabelStatement(endLabel);
                BoundBlockStatement result = new BoundBlockStatement(ImmutableArray.Create(gotoFalse, node.Body, endLabelStatement));
                return RewriteStatement(result);
            }
            else
            {
                BoundLabel elseLabel = GenerateLabel();
                BoundLabel endLabel = GenerateLabel();

                BoundConditionalGotoStatement gotoFalse = new BoundConditionalGotoStatement(elseLabel, node.Condition, true);
                BoundGotoStatement gotoEnd = new BoundGotoStatement(endLabel);
                BoundLabelStatement elseLabelStatement = new BoundLabelStatement(elseLabel);
                BoundLabelStatement endLabelStatement = new BoundLabelStatement(endLabel);
                BoundBlockStatement result = new BoundBlockStatement(ImmutableArray.Create(
                    gotoFalse,
                    node.Body,
                    gotoEnd, 
                    elseLabelStatement,
                    node.ElseBody,
                    endLabelStatement
                ));
                return RewriteStatement(result);
            }
        }

        protected override BoundStatement RewriteWhileStatement(BoundWhileStatement node)
        {
            BoundLabel checkLabel = GenerateLabel();
            BoundLabelStatement breakLabelStatement = new BoundLabelStatement(node.BreakLabel);
            BoundLabelStatement continueLabelStatement = new BoundLabelStatement(node.ContinueLabel);
            BoundLabelStatement checkLabelStatement = new BoundLabelStatement(checkLabel);
            BoundGotoStatement gotoCheck = new BoundGotoStatement(checkLabel);

            BoundConditionalGotoStatement gotoTrue = new BoundConditionalGotoStatement(node.ContinueLabel, node.Condition, false);
            BoundBlockStatement result = new BoundBlockStatement(ImmutableArray.Create(
                gotoCheck,
                continueLabelStatement,
                node.Body,
                checkLabelStatement,
                gotoTrue,
                breakLabelStatement
            ));
            return RewriteStatement(result);
        }

        protected override BoundStatement RewriteDoWhileStatement(BoundDoWhileStatement node)
        {
            BoundLabelStatement breakLabelStatement = new BoundLabelStatement(node.BreakLabel);
            BoundLabelStatement continueLabelStatement = new BoundLabelStatement(node.ContinueLabel);
            
            BoundConditionalGotoStatement gotoTrue = new BoundConditionalGotoStatement(node.ContinueLabel, node.Condition, false);
            BoundBlockStatement result = new BoundBlockStatement(ImmutableArray.Create(
                continueLabelStatement,
                node.Body,
                gotoTrue,
                breakLabelStatement
            ));
            return RewriteStatement(result);
        }

        protected override BoundStatement RewriteForStatement(BoundForStatement node)
        {
            BoundBinaryOperator op = BoundBinaryOperator.SafeBind(BoundBinaryOperatorKind.LessOrEquals, TypeSymbol.Int, TypeSymbol.Int);
            BoundBinaryOperator plusOp = BoundBinaryOperator.SafeBind(BoundBinaryOperatorKind.Addition, TypeSymbol.Int, TypeSymbol.Int);

            BoundVariableDeclarationStatement declarationStatement = new BoundVariableDeclarationStatement(node.Variable, node.LowerBound);

            GlobalVariableSymbol upperBound = new GlobalVariableSymbol("upperBound", TypeSymbol.Int);
            BoundVariableDeclarationStatement upperBoundDeclarationStatement = new BoundVariableDeclarationStatement(upperBound, node.UpperBound);

            BoundVariableExpression variableExpression = new BoundVariableExpression(node.Variable);
            BoundVariableExpression upperBoundExpression = new BoundVariableExpression(upperBound);

            BoundBinaryExpression condition = new BoundBinaryExpression(variableExpression, op, upperBoundExpression);
            BoundLabelStatement continueLabelStatement = new BoundLabelStatement(node.ContinueLabel);
            BoundExpressionStatement increment = new BoundExpressionStatement(new BoundAssignmentExpression(node.Variable, new BoundBinaryExpression(variableExpression, plusOp, new BoundLiteralExpression(1))));

            BoundBlockStatement whileBlock = new BoundBlockStatement(ImmutableArray.Create(node.Body, continueLabelStatement, increment));
            BoundWhileStatement whileStatement = new BoundWhileStatement(condition, whileBlock, node.BreakLabel, GenerateLabel());
            BoundStatement result = new BoundBlockStatement(ImmutableArray.Create<BoundStatement>(
                declarationStatement,
                upperBoundDeclarationStatement,
                whileStatement
            ));
            return RewriteStatement(result);
        }
    }
}
