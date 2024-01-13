using DPP_Compiler.Binding;
using System.Collections.Immutable;

namespace DPP_Compiler.Lowering
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

        private LabelSymbol GenerateLabel()
        {
            var name = $"label{++_labelCount}";
            return new LabelSymbol(name);
        }

        protected override BoundStatement RewriteIfStatement(BoundIfStatement node)
        {
            if (node.ElseBody == null)
            {
                LabelSymbol endLabel = GenerateLabel();
                BoundConditionalGotoStatement gotoFalse = new BoundConditionalGotoStatement(endLabel, node.Condition, true);
                BoundLabelStatement endLabelStatement = new BoundLabelStatement(endLabel);
                BoundBlockStatement result = new BoundBlockStatement(ImmutableArray.Create(gotoFalse, node.Body, endLabelStatement));
                return RewriteStatement(result);
            }
            else
            {
                LabelSymbol elseLabel = GenerateLabel();
                LabelSymbol endLabel = GenerateLabel();

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
            LabelSymbol continueLabel = GenerateLabel();
            LabelSymbol checkLabel = GenerateLabel();
            LabelSymbol endLabel = GenerateLabel();

            BoundGotoStatement gotoCheck = new BoundGotoStatement(checkLabel);

            BoundLabelStatement continueLabelStatement = new BoundLabelStatement(continueLabel);
            BoundLabelStatement checkLabelStatement = new BoundLabelStatement(checkLabel);
            BoundLabelStatement endLabelStatement = new BoundLabelStatement(endLabel);

            BoundConditionalGotoStatement gotoTrue = new BoundConditionalGotoStatement(continueLabel, node.Condition, false);
            BoundBlockStatement result = new BoundBlockStatement(ImmutableArray.Create(
                gotoCheck,
                continueLabelStatement,
                node.Body,
                checkLabelStatement,
                gotoTrue,
                endLabelStatement
            ));
            return RewriteStatement(result);
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
