using Blaze.SyntaxTokens;
using System.Collections.Immutable;
using System.Collections;

namespace Blaze.Syntax_Nodes
{
    public sealed class SeparatedSyntaxList<T> : IEnumerable<T> where T : SyntaxNode
    {
        private readonly ImmutableArray<SyntaxNode> _separatorsAndNodes;

        public int Count => (_separatorsAndNodes.Length + 1) / 2;

        public T this[int index] => (T) _separatorsAndNodes[index * 2];

        internal SeparatedSyntaxList(ImmutableArray<SyntaxNode> separatorsAndNodes)
        {
            _separatorsAndNodes = separatorsAndNodes;
        }

        public ImmutableArray<SyntaxNode> GetWithSeparators() => _separatorsAndNodes;

        public SyntaxToken? GetSeparator(int index)
        {
            if (index == Count - 1)
                return null;

            return (SyntaxToken)_separatorsAndNodes[index * 2 + 1];
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
