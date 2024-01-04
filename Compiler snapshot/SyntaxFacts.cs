namespace Compiler_snapshot
{
    internal static class SyntaxFacts
    {
        public static int GetBinaryOperatorPrecedence(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.StarToken:
                case SyntaxKind.SlashToken:
                    return 4;
                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
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
                    return 6;
                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                    return 5;

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
    }
}
