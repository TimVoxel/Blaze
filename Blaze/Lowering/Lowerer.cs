using Blaze.Binding;
using Blaze.Symbols;
using Mono.Cecil.Cil;
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
            //          a = a + 1;
            //      }
            //  
            //than rewrite that

            var op = BoundBinaryOperator.SafeBind(BoundBinaryOperatorKind.LessOrEquals, TypeSymbol.Int, TypeSymbol.Int);
            var plusOp = BoundBinaryOperator.SafeBind(BoundBinaryOperatorKind.Addition, TypeSymbol.Int, TypeSymbol.Int);

            var declarationStatement = new BoundVariableDeclarationStatement(node.Variable, node.LowerBound);

            var upperBound = new GlobalVariableSymbol(".upperBound", TypeSymbol.Int, true, node.UpperBound.ConstantValue);
            var upperBoundDeclarationStatement = new BoundVariableDeclarationStatement(upperBound, node.UpperBound);

            var variableExpression = new BoundVariableExpression(node.Variable);
            var upperBoundExpression = new BoundVariableExpression(upperBound);

            var condition = new BoundBinaryExpression(variableExpression, op, upperBoundExpression);
            var increment = new BoundExpressionStatement(new BoundAssignmentExpression(variableExpression, new BoundBinaryExpression(variableExpression, plusOp, new BoundLiteralExpression(1))));

            var whileBlock = new BoundBlockStatement(ImmutableArray.Create(node.Body, increment));
            var whileStatement = new BoundWhileStatement(condition, whileBlock, node.BreakLabel, GenerateLabel());
            var result = new BoundBlockStatement(ImmutableArray.Create<BoundStatement>(
                declarationStatement,
                upperBoundDeclarationStatement,
                whileStatement
            ));
            return RewriteStatement(result);
        }

        protected override BoundExpression RewriteBinaryExpression(BoundBinaryExpression node)
        {
            //namedType1 == namedType2
            //>
            //namedType1.f == namedType2.f && namedType1.y == namedType2.y && ... 
            
            BoundExpression RewriteAsFieldComparasons(NamedTypeSymbol type, BoundBinaryOperatorKind op)
            {
                var equations = new Queue<BoundExpression>();

                foreach (var field in type.Fields)
                {
                    var leftAccess = new BoundFieldAccessExpression(node.Left, field);
                    var rightAccess = new BoundFieldAccessExpression(node.Right, field);
                    var equalityOperator = BoundBinaryOperator.SafeBind(op, field.Type, field.Type);
                    var fieldsEqual = RewriteBinaryExpression(new BoundBinaryExpression(leftAccess, equalityOperator, rightAccess));
                    equations.Enqueue(fieldsEqual);
                }

                BoundExpression expression = equations.Dequeue();

                var connectOperator = BoundBinaryOperator.SafeBind(
                        op == BoundBinaryOperatorKind.Equals
                        ? BoundBinaryOperatorKind.LogicalMultiplication
                        : BoundBinaryOperatorKind.LogicalAddition,
                        TypeSymbol.Bool, TypeSymbol.Bool
                    );

                while (equations.Any())
                {
                    var current = equations.Dequeue();
                    expression = new BoundBinaryExpression(expression, connectOperator, current);
                }

                return expression;
            }

            //Rewrite equations between named types to have them compare every field

            if (node.Operator == BoundBinaryOperator.NamedTypeDoubleEqualsOperator)
            {
                NamedTypeSymbol type = (NamedTypeSymbol) node.Left.Type;
                return RewriteAsFieldComparasons(type, BoundBinaryOperatorKind.Equals);
            }
            else if (node.Operator == BoundBinaryOperator.NamedTypeNotEqualsOperator)
            {
                NamedTypeSymbol type = (NamedTypeSymbol)node.Left.Type;
                return RewriteAsFieldComparasons(type, BoundBinaryOperatorKind.NotEquals);
            }

            //Rewrite Yoda-code, for example "0 == a" turns into "a == 0"
            //This is needed to more easily apply emittion level optimisations
            if (node.Left is BoundLiteralExpression && node.Operator.OperatorKind == BoundBinaryOperatorKind.Equals || node.Operator.OperatorKind == BoundBinaryOperatorKind.Equals)
                node = new BoundBinaryExpression(node.Right, node.Operator, node.Left);
            if (node.Left is BoundVariableExpression v && v.Variable is EnumMemberSymbol && node.Operator.OperatorKind == BoundBinaryOperatorKind.Equals || node.Operator.OperatorKind == BoundBinaryOperatorKind.Equals)
                node = new BoundBinaryExpression(node.Right, node.Operator, node.Left);

            return node;
        }

        protected override BoundExpression RewriteCompoundAssignmentExpression(BoundCompoundAssignmentExpression node)
        {
            // a += b;
            // >
            // a = a + b;

            var binary = new BoundBinaryExpression(node.Left, node.Operator, node.Right);
            var assignment = new BoundAssignmentExpression(node.Left, binary);
            return RewriteExpression(assignment);
        }

        protected override BoundExpression RewriteIncrementExpression(BoundIncrementExpression node)
        {
            // a++;
            // >
            // a = a + 1;

            var oneLiteral = new BoundLiteralExpression(1);
            var binary = new BoundBinaryExpression(node.Operand, node.IncrementOperator, oneLiteral);
            var assignment = new BoundAssignmentExpression(node.Operand, binary);
            return RewriteExpression(assignment);
        }
    }
}
