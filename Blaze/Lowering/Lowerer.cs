using Blaze.Binding;
using Blaze.Symbols;
using System.Collections.Immutable;

namespace Blaze.Lowering
{
    internal class Lowerer : BoundTreeRewriter
    {
        protected int _labelCount;

        protected Lowerer() 
        {
            _labelCount = 0;
        }

        public static BoundStatement Lower(BoundStatement statement)
        {
            var lowerer = new Lowerer();
            var result = lowerer.RewriteStatement(statement);
            return result;
        } 

        public static BoundBlockStatement DeepLower(BoundStatement statement)
        {
            var lowerer = new DeepLowerer();
            var result = lowerer.RewriteStatement(statement);
            return RemoveDeadCode(Flatten(result));
        }

        public static BoundBlockStatement RemoveDeadCode(BoundBlockStatement node)
        {
            var controlFlowGraph = ControlFlowGraph.Create(node);
            var reachableStatements = new HashSet<BoundStatement>(controlFlowGraph.Blocks.SelectMany(b => b.Statements));

            var builder = node.Statements.ToBuilder();
            for (int i = builder.Count - 1; i >= 0; i--)
                if (!reachableStatements.Contains(builder[i]))
                    builder.RemoveAt(i);

            return new BoundBlockStatement(builder.ToImmutable());
        }

        protected static BoundBlockStatement Flatten(BoundStatement statement)
        {
            var builder = ImmutableArray.CreateBuilder<BoundStatement>();
            var stack = new Stack<BoundStatement>();
            stack.Push(statement);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current is BoundBlockStatement block)
                {
                    foreach (var subStatement in block.Statements.Reverse())
                        stack.Push(subStatement);
                }
                else
                    builder.Add(current);
            }
            return new BoundBlockStatement(builder.ToImmutable());
        }

        protected BoundLabel GenerateLabel()
        {
            var name = $"sub{++_labelCount}";
            return new BoundLabel(name);
        }

        protected override BoundStatement RewriteForStatement(BoundForStatement node)
        {
            //source:
            //      let a = initial;
            //      let upperBound = final;
            //      while (a <= upperBound) {
            //          ...
            //          a = a - 1;
            //      }
            //  
            //than rewrite that

            var op = BoundBinaryOperator.SafeBind(BoundBinaryOperatorKind.LessOrEquals, TypeSymbol.Int, TypeSymbol.Int);
            var plusOp = BoundBinaryOperator.SafeBind(BoundBinaryOperatorKind.Addition, TypeSymbol.Int, TypeSymbol.Int);

            var declarationStatement = new BoundVariableDeclarationStatement(node.Variable, node.LowerBound);

            var upperBound = new GlobalVariableSymbol("upperBound", TypeSymbol.Int, node.UpperBound.ConstantValue);
            var upperBoundDeclarationStatement = new BoundVariableDeclarationStatement(upperBound, node.UpperBound);

            var variableExpression = new BoundVariableExpression(node.Variable);
            var upperBoundExpression = new BoundVariableExpression(upperBound);

            var condition = new BoundBinaryExpression(variableExpression, op, upperBoundExpression);
            //var continueLabelStatement = new BoundLabelStatement(node.ContinueLabel);
            var increment = new BoundExpressionStatement(new BoundAssignmentExpression(node.Variable, new BoundBinaryExpression(variableExpression, plusOp, new BoundLiteralExpression(1))));

            var whileBlock = new BoundBlockStatement(ImmutableArray.Create(node.Body, increment));
            var whileStatement = new BoundWhileStatement(condition, whileBlock, node.BreakLabel, GenerateLabel());
            var result = new BoundBlockStatement(ImmutableArray.Create<BoundStatement>(
                declarationStatement,
                upperBoundDeclarationStatement,
                whileStatement
            ));
            return RewriteStatement(result);
        }

        //This stuff doesn't seem to work correctly without constant variables duh
        /*
        protected override BoundStatement RewriteConditionalGotoStatement(BoundConditionalGotoStatement node)
        {

            if (node.Condition.ConstantValue != null)
            {
                var condition = (bool) node.Condition.ConstantValue.Value;
                condition = node.JumpIfFalse ? !condition : condition;

                if (condition)
                    return RewriteGotoStatement(new BoundGotoStatement(node.Label));
                else
                    return RewriteNopStatement(new BoundNopStatement());
            }
            return base.RewriteConditionalGotoStatement(node);
        }
        */
    }
}
