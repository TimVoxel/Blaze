using Blaze.SyntaxTokens;
using System.Collections.Immutable;

namespace Blaze.Syntax_Nodes
{
    public sealed class FunctionDeclarationSyntax : MemberSyntax
    {
        public ImmutableArray<SyntaxToken> Modifiers { get; }
        public SyntaxToken FunctionKeyword { get; }
        public SyntaxToken Identifier { get; }
        public SyntaxToken OpenParen { get; }
        public SeparatedSyntaxList<ParameterSyntax> Parameters { get; }
        public SyntaxToken CloseParen { get; }
        public ReturnTypeClauseSyntax? ReturnTypeClause { get; }
        public BlockStatementSyntax Body { get; }

        public override SyntaxKind Kind => SyntaxKind.FunctionDeclaration;

        internal FunctionDeclarationSyntax(SyntaxTree tree, ImmutableArray<SyntaxToken> modifiers, SyntaxToken functionKeyword, SyntaxToken identifier, SyntaxToken openParen, SeparatedSyntaxList<ParameterSyntax> parameters, SyntaxToken closeParen, ReturnTypeClauseSyntax? returnTypeClause, BlockStatementSyntax body) : base(tree)
        {
            Modifiers = modifiers;
            FunctionKeyword = functionKeyword;
            Identifier = identifier;
            OpenParen = openParen;
            Parameters = parameters;
            CloseParen = closeParen;
            ReturnTypeClause = returnTypeClause;
            Body = body;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            foreach (SyntaxToken modifier in Modifiers)
                yield return modifier;

            yield return FunctionKeyword;
            yield return Identifier;
            
            yield return OpenParen;
            foreach (SyntaxNode node in Parameters.GetWithSeparators())
                yield return node;
            yield return CloseParen;
            if (ReturnTypeClause != null)
                yield return ReturnTypeClause;
            yield return Body;
        }
    }
}
