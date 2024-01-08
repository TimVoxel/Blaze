using DPP_Compiler.SyntaxTokens;

namespace DPP_Compiler.Tests
{
    public class SyntaxFactTest
    {
        [Theory]
        [MemberData(nameof(GetSyntaxKindData))]
        public void SyntaxFact_GetText_RundTrips(SyntaxKind kind)
        {
            string? text = SyntaxFacts.GetText(kind);
            if (text == null)
                return;

            IEnumerable<SyntaxToken> tokens = SyntaxTree.ParseTokens(text);
            SyntaxToken token = Assert.Single(tokens);

            Assert.Equal(kind, token.Kind);
            Assert.Equal(text, token.Text);
        }

        public static IEnumerable<object[]> GetSyntaxKindData()
        {
            foreach (SyntaxKind kind in SyntaxFacts.AllSyntaxKinds)
                yield return new object[] { kind };
        }
    }
}