namespace DPP_Compiler
{
    public static class SyntaxFacts
    {
        public static SyntaxKind[] AllSyntaxKinds => (SyntaxKind[])Enum.GetValues(typeof(SyntaxKind));
        public static SyntaxKind[] AllTokenKinds => AllSyntaxKinds.Where(k => k.ToString().EndsWith("Keyword") || k.ToString().EndsWith("Token")).ToArray();

        public static int GetBinaryOperatorPrecedence(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.StarToken:
                case SyntaxKind.SlashToken:
                    return 5;
                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                    return 4;
                case SyntaxKind.DoubleEqualsToken:
                case SyntaxKind.NotEqualsToken:
                    return 3;
                case SyntaxKind.DoubleAmpersandToken:
                    return 2;
                case SyntaxKind.DoublePipeToken:
                    return 1;

                default: return 0;
            }
        }

        public static int GetUnaryOperatorPrecedence(this SyntaxKind kind)
        {
            switch (kind) 
            {
                case SyntaxKind.ExclamationSignToken:
                    return 7;
                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                    return 6;

                default: return 0;
            }
        }

        public static SyntaxKind GetKeywordKind(string text)
        {
            switch (text)
            {
                case "true":
                    return SyntaxKind.TrueKeyword;
                case "false":
                    return SyntaxKind.FalseKeyword;
                default: 
                    return SyntaxKind.IdentifierToken;
            }
        }

        public static IEnumerable<SyntaxKind> GetBinaryOperators()
        {
            foreach (SyntaxKind kind in AllSyntaxKinds)
                if (GetBinaryOperatorPrecedence(kind) > 0)
                    yield return kind;
        }

        public static IEnumerable<SyntaxKind> GetUnaryOperators()
        {
            foreach (SyntaxKind kind in AllSyntaxKinds)
                if (GetUnaryOperatorPrecedence(kind) > 0)
                    yield return kind;
        }

        public static string? GetText(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.FalseKeyword: 
                    return "false";
                case SyntaxKind.TrueKeyword:
                    return "true";
                case SyntaxKind.SemicolonToken:
                    return ";";
                case SyntaxKind.OpenBraceToken:
                    return "{";
                case SyntaxKind.CloseBraceToken:
                    return "}";
                case SyntaxKind.PlusToken:
                    return "+";
                case SyntaxKind.MinusToken:
                    return "-";
                case SyntaxKind.StarToken:
                    return "*";
                case SyntaxKind.SlashToken:
                    return "/";
                case SyntaxKind.OpenParenToken:
                    return "(";
                case SyntaxKind.CloseParenToken:
                    return ")";
                case SyntaxKind.ExclamationSignToken:
                    return "!";
                case SyntaxKind.EqualsToken:
                    return "=";
                case SyntaxKind.DoubleEqualsToken:
                    return "==";
                case SyntaxKind.DoubleAmpersandToken:
                    return "&&";
                case SyntaxKind.DoublePipeToken:
                    return "||";
                case SyntaxKind.NotEqualsToken:
                    return "!=";
                default:
                    return null;
            }
        }
    }
}
