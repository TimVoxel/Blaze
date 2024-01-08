namespace DPP_Compiler.Tests.CodeAnalysis
{
    public class EvaluatorTest
    {
        [Theory]
        [InlineData("1", 1)]
        [InlineData("+1", 1)]
        [InlineData("-1", -1)]
        [InlineData("1 + 2", 3)]
        [InlineData("1 - 2", -1)]
        [InlineData("1 * 2", 2)]
        [InlineData("9 / 3", 3)]
        [InlineData("30 * 20", 600)]
        [InlineData("(10- 20) * 7", -70)]
        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData("true && false", false)]
        [InlineData("true || false", true)]
        [InlineData("!true", false)]
        [InlineData("!false", true)]
        [InlineData("false == false", true)]
        [InlineData("true == false", false)]
        [InlineData("true == true", true)]
        [InlineData("10 == 10", true)]
        [InlineData("10 == -10", false)]
        [InlineData("10 != -10", true)]
        [InlineData("10 != 10", false)]
        public void Evaluator_Evaluate_Expression(string text, object expectedResult)
        {
            SyntaxTree tree = SyntaxTree.Parse(text);
            Compilation compilation = new Compilation(tree);
            Dictionary<VariableSymbol, object?> variables = new Dictionary<VariableSymbol, object?>();
            EvaluationResult evaluationResult = compilation.Evaluate(variables);

            Assert.Empty(evaluationResult.Diagnostics);
            Assert.Equal(evaluationResult.Value, expectedResult);
        }
    }
}
