namespace Blaze.Syntax_Nodes
{
    public abstract class DeclarationSyntax : MemberSyntax 
    {
        public DeclarationSyntax(SyntaxTree tree) : base(tree) { }
    }
}
