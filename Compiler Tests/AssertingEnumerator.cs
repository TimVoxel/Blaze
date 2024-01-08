using DPP_Compiler.Syntax_Nodes;
using DPP_Compiler.SyntaxTokens;

namespace DPP_Compiler.Tests
{
    internal sealed class AssertingEnumerator : IDisposable
    {
        private IEnumerator<SyntaxNode> _enumerator;
        private bool _hasErrors;

        public AssertingEnumerator(SyntaxNode node)
        {
            _enumerator = Flatten(node).GetEnumerator();
        }

        private bool MarkFailed()
        {
            _hasErrors = true;
            return false;
        }

        private static IEnumerable<SyntaxNode> Flatten(SyntaxNode node)
        {
            Stack<SyntaxNode> stack = new Stack<SyntaxNode>();
            stack.Push(node);

            while (stack.Count > 0)
            {
                SyntaxNode currentNode = stack.Pop();
                yield return currentNode;

                foreach (SyntaxNode child in currentNode.GetChildren().Reverse())
                    stack.Push(child);
            }
        }

        public void AssertToken(SyntaxKind kind, string text)
        {
            try
            {
                Assert.True(_enumerator.MoveNext());
                SyntaxToken token = Assert.IsType<SyntaxToken>(_enumerator.Current);
                Assert.Equal(kind, token.Kind);
                Assert.Equal(text, token.Text);
            }
            catch when (MarkFailed())
            {
                throw;
            }
        }

        public void AssertNode(SyntaxKind kind)
        {
            try
            {
                Assert.True(_enumerator.MoveNext());
                Assert.IsNotType<SyntaxToken>(_enumerator.Current);
                Assert.Equal(kind, _enumerator.Current.Kind);
            }
            catch when (MarkFailed())
            {
                throw;
            }
        }

        public void Dispose()
        {
            if (!_hasErrors)
                Assert.False(_enumerator.MoveNext());
            _enumerator.Dispose();
        }
    }
}