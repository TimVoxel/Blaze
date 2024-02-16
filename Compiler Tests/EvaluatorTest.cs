using Blaze.Symbols;
using Blaze.Text;

namespace Blaze.Tests.CodeAnalysis
{
    public class EvaluatorTest
    {
        [Theory]
        [InlineData("1;", 1)]
        [InlineData("+1;", 1)]
        [InlineData("-1;", -1)]
        [InlineData("1 + 2;", 3)]
        [InlineData("1 - 2;", -1)]
        [InlineData("1 * 2;", 2)]
        [InlineData("9 / 3;", 3)]
        [InlineData("30 * 20;", 600)]
        [InlineData("(10- 20) * 7;", -70)]
        [InlineData("true;", true)]
        [InlineData("false;", false)]
        [InlineData("true && false;", false)]
        [InlineData("true || false;", true)]
        [InlineData("!true;", false)]
        [InlineData("!false;", true)]
        [InlineData("false == false;", true)]
        [InlineData("true == false;", false)]
        [InlineData("true == true;", true)]
        [InlineData("10 == 10;", true)]
        [InlineData("10 == -10;", false)]
        [InlineData("10 != -10;", true)]
        [InlineData("10 != 10;", false)]
        [InlineData("3 < 4;", true)]
        [InlineData("5 < 4;", false)]
        [InlineData("3 > 4;", false)]
        [InlineData("4 > 3;", true)]
        [InlineData("6 <= 10;", true)]
        [InlineData("6 <= 3;", false)]
        [InlineData("6 <= 6;", true)]
        [InlineData("6 >= 10;", false)]
        [InlineData("6 >= 3;", true)]
        [InlineData("6 >= 6;", true)]
        [InlineData("{ let a = 10; (a = 20) * a; }", 400)]
        [InlineData("{ let a = 10; if (a == 10) let c = true; }", true)]
        [InlineData("{ let a = 10; if (a == 69) let c = true; }", 10)]
        [InlineData("{ let a = 10; if (a == 69) {let c = true;} else let c = 69; }", 69)]
        [InlineData("{ let i = 10; let result = 0; while (i > 0) { result = result + i; i = i - 1;} result = result; }", 55)]
        [InlineData("{ let result = 0; for (i = 1..10) { result = result + i; } result = result; }", 55)]
        public void Evaluator_Evaluate_Expression(string text, object expectedResult)
        {
            AssertValue(text, expectedResult);
        }

        private void AssertValue(string text, object expectedResult)
        {
            SyntaxTree tree = SyntaxTree.Parse(text);
            Compilation compilation = Compilation.CreateScript(null, tree);

            Dictionary<VariableSymbol, object?> variables = new Dictionary<VariableSymbol, object?>();
            EvaluationResult evaluationResult = compilation.Evaluate(variables);

            Assert.Empty(evaluationResult.Diagnostics);
            Assert.Equal(expectedResult, evaluationResult.Value);
        }

        [Fact]
        public void Evaluator_VariableDeclaration_Reportes_Redeclaration()
        {
            string text = @"{
                let x = 10;
                let y = 100;
                {
                    let x = 10;
                }
                let [x] = 5; }
            ";

            string diagnosticText = "Variable \"x\" is already declared";

            AssertDiagnostics(text, diagnosticText);
        }

        [Fact]
        public void Evaluator_IdentifierExpression_NameReports_Undefined()
        {
            string text = @"[a] * 10;"; 
            string diagnosticText = "Variable \"a\" doesn't exist";

            AssertDiagnostics(text, diagnosticText);
        }

        [Fact]
        public void Evaluator_IdentifierExpression_Reports_NoErrorForInsertedToken()
        {
            string text = @"";
            string diagnosticText = @"";
            AssertDiagnostics(text, diagnosticText);
        }

        [Fact]
        public void Evaluator_Assignment_NameReports_Undefined()
        {
            string text = @"[a] = 10;";
            string diagnosticText = "Variable \"a\" doesn't exist";

            AssertDiagnostics(text, diagnosticText);
        }

        [Fact]
        public void Evaluator_BlockStatement_NoInfiniteLoop()
        {
            string text = @"{
                ([][][][]
            ";
            string diagnosticText = @"
                Unexpected token <EndOfFileToken>, expected <IdentifierToken>
                Unexpected token <EndOfFileToken>, expected <CloseParenToken>
                Unexpected token <EndOfFileToken>, expected <SemicolonToken>
                Unexpected token <EndOfFileToken>, expected <CloseBraceToken>";

            AssertDiagnostics(text, diagnosticText);
        }


        [Fact]
        public void Evaluator_Cant_Convert_Types()
        {
            string text = @"{
                let a = 10;
                a = [true];
            }";
            string diagnosticText = "Can not convert type bool to type int";

            AssertDiagnostics(text, diagnosticText);
        }

        [Fact]
        public void Unary_Reports_Undefined_Operator()
        {
            string text = @"[+]true;";
            string diagnosticText = "Unary operator '+' is not defined for type bool";
            AssertDiagnostics(text, diagnosticText);
        }

        [Fact]
        public void Binary_Reports_Undefined_Operator()
        {
            string text = @"true [&&] 10;";
            string diagnosticText = "Binary operator '&&' is not defined for types bool and int";
            AssertDiagnostics(text, diagnosticText);
        }


        [Fact]
        public void Evaluator_If_Cant_Convert_Types()
        {
            string text = @"{
                let a = 10;
                if ([a])
                    a = 20;
            }";
            string diagnosticText = "Can not convert type int to type bool";

            AssertDiagnostics(text, diagnosticText);
        }

        [Fact]
        public void Evaluator_While_Cant_Convert_Types()
        {
            string text = @"{
                let a = 10;
                while ([a])
                    a = a - 1;
            }";
            string diagnosticText = "Can not convert type int to type bool";

            AssertDiagnostics(text, diagnosticText);
        }

        [Fact]
        public void Evaluator_For_Cant_Convert_Types()
        {
            string text = @"{
                let a = 10;
                for (i = [false] .. 10)
                    a = a - 1;
            }";
            string diagnosticText = "Can not convert type bool to type int";

            AssertDiagnostics(text, diagnosticText);
        }

        [Fact]
        public void Evaluator_For_Cant_Convert_Types2()
        {
            string text = @"{
                let a = 10;
                for (i = 10 .. [true])
                    a = a - 1;
            }";
            string diagnosticText = "Can not convert type bool to type int";

            AssertDiagnostics(text, diagnosticText);
        }

        private void AssertDiagnostics(string text, string diagnosticText)
        {
            AnnotatedText annotatedText = AnnotatedText.Parse(text);
            SyntaxTree syntaxTree = SyntaxTree.Parse(annotatedText.Text);
            Compilation compilation = Compilation.CreateScript(null, syntaxTree);

            EvaluationResult result = compilation.Evaluate(new Dictionary<VariableSymbol, object?>());

            List<string> expectedDiagnostics = AnnotatedText.UnindentLines(diagnosticText);

            if (annotatedText.Spans.Length != expectedDiagnostics.Count)
                throw new Exception("ERROR: Must mark as many spans as there are expected diagnostics");

            Assert.Equal(expectedDiagnostics.Count, result.Diagnostics.Length);

            for (int i = 0; i < expectedDiagnostics.Count; i++)
            {
                string expectedMessage = expectedDiagnostics[i];
                string actualMessage = result.Diagnostics[i].Message;
                Assert.Equal(expectedMessage, actualMessage);

                TextSpan expectedSpan = annotatedText.Spans[i];
                TextSpan actualSpan = result.Diagnostics[i].Location.Span;
                Assert.Equal(expectedSpan, actualSpan);
            }
        }
    }
}
