using Blaze.Syntax_Nodes;

namespace Blaze.Tests
{
    public class ParserTest
    {
        [Theory]
        [MemberData(nameof(GetBinaryOperatorPairsData))]
        public void Parser_Binary_Expression_HonorsPrecedences(SyntaxKind operator1, SyntaxKind operator2)
        {
            int operator1Precedence = SyntaxFacts.GetBinaryOperatorPrecedence(operator1);
            int operator2Precedence = SyntaxFacts.GetBinaryOperatorPrecedence(operator2);
            string? operator1Text = SyntaxFacts.GetText(operator1);
            string? operator2Text = SyntaxFacts.GetText(operator2);
            if (operator1Text == null || operator2Text == null) 
                return;

            string text = $"a {operator1Text} b {operator2Text} c;";
            ExpressionSyntax expression = ParseExpression(text);

            if (operator1Precedence >= operator2Precedence)
            {
                using (AssertingEnumerator enumerator = new AssertingEnumerator(expression))
                {
                    enumerator.AssertNode(SyntaxKind.BinaryExpression);
                    enumerator.AssertNode(SyntaxKind.BinaryExpression);
                    enumerator.AssertNode(SyntaxKind.IdentifierExpression);
                    enumerator.AssertToken(SyntaxKind.IdentifierToken, "a");
                    enumerator.AssertToken(operator1, operator1Text);
                    enumerator.AssertNode(SyntaxKind.IdentifierExpression);
                    enumerator.AssertToken(SyntaxKind.IdentifierToken, "b");
                    enumerator.AssertToken(operator2, operator2Text);
                    enumerator.AssertNode(SyntaxKind.IdentifierExpression);
                    enumerator.AssertToken(SyntaxKind.IdentifierToken, "c");
                }
            }
            else
            {
                using (AssertingEnumerator enumerator = new AssertingEnumerator(expression))
                {
                    enumerator.AssertNode(SyntaxKind.BinaryExpression);
                    enumerator.AssertNode(SyntaxKind.IdentifierExpression);
                    enumerator.AssertToken(SyntaxKind.IdentifierToken, "a");
                    enumerator.AssertToken(operator1, operator1Text);
                    enumerator.AssertNode(SyntaxKind.BinaryExpression);
                    enumerator.AssertNode(SyntaxKind.IdentifierExpression);
                    enumerator.AssertToken(SyntaxKind.IdentifierToken, "b");
                    enumerator.AssertToken(operator2, operator2Text);
                    enumerator.AssertNode(SyntaxKind.IdentifierExpression);
                    enumerator.AssertToken(SyntaxKind.IdentifierToken, "c");
                }
            }
        }

        private static ExpressionSyntax ParseExpression(string text)
        {
            SyntaxTree syntaxTree = SyntaxTree.Parse(text);
            CompilationUnitSyntax root = syntaxTree.Root;
            MemberSyntax member = Assert.Single(root.Namespaces);
            GlobalStatementSyntax globalStatement = Assert.IsType<GlobalStatementSyntax>(member);
            return Assert.IsType<ExpressionStatementSyntax>(globalStatement.Statement).Expression;
        }

        public static IEnumerable<object[]> GetBinaryOperatorPairsData()
        {
            foreach (SyntaxKind operator1 in SyntaxFacts.GetBinaryOperators())
                foreach (SyntaxKind operator2 in SyntaxFacts.GetBinaryOperators())
                    yield return new object[] { operator1, operator2 };
        }

        public static IEnumerable<object[]> GetUnaryOperatorPairsData()
        {
            foreach (SyntaxKind unary in SyntaxFacts.GetUnaryOperators())
                foreach (SyntaxKind binary in SyntaxFacts.GetBinaryOperators())
                    yield return new object[] { unary, binary };
        }

    }
}