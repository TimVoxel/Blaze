using DPP_Compiler.SyntaxTokens;

namespace DPP_Compiler.Syntax_Nodes
{
    public sealed class FunctionDeclarationSyntax : MemberSyntax
    {
        public SyntaxToken FunctionKeyword { get; private set; }
        public SyntaxToken Identifier { get; private set; }
        public SyntaxToken OpenParen { get; private set; }
        public SeparatedSyntaxList<ParameterSyntax> Parameters { get; private set; }
        public SyntaxToken CloseParen { get; private set; }
        public ReturnTypeClauseSyntax? ReturnTypeClause { get; private set; }
        public BlockStatementSyntax Body { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.FunctionDeclaration;

        public FunctionDeclarationSyntax(SyntaxToken functionKeyword, SyntaxToken identifier, SyntaxToken openParen, SeparatedSyntaxList<ParameterSyntax> parameters, SyntaxToken closeParen, ReturnTypeClauseSyntax? returnTypeClause, BlockStatementSyntax body)
        {
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
