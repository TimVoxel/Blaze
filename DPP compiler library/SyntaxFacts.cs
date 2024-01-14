using DPP_Compiler.SyntaxTokens;

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
                    return 6;
                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                    return 5;
                case SyntaxKind.LessToken:
                case SyntaxKind.LessOrEqualsToken:
                case SyntaxKind.GreaterToken:
                case SyntaxKind.GreaterOrEqualsToken:
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
                    return 8;
                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                    return 7;

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
                case "let":
                    return SyntaxKind.LetKeyword;
                case "if":
                    return SyntaxKind.IfKeyword;
                case "else":
                    return SyntaxKind.ElseKeyword;
                case "while":
                    return SyntaxKind.WhileKeyword;
                case "for":
                    return SyntaxKind.ForKeyword;
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
                case SyntaxKind.LetKeyword:
                    return "let";
                case SyntaxKind.IfKeyword:
                    return "if";
                case SyntaxKind.ElseKeyword:
                    return "else";
                case SyntaxKind.WhileKeyword:
                    return "while";
                case SyntaxKind.ForKeyword:
                    return "for";
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
                case SyntaxKind.LessToken:
                    return "<";
                case SyntaxKind.LessOrEqualsToken:
                    return "<=";
                case SyntaxKind.GreaterToken:
                    return ">";
                case SyntaxKind.GreaterOrEqualsToken:
                    return ">=";
                case SyntaxKind.DoubleAmpersandToken:
                    return "&&";
                case SyntaxKind.DoublePipeToken:
                    return "||";
                case SyntaxKind.DoubleDotToken:
                    return "..";
                case SyntaxKind.NotEqualsToken:
                    return "!=";  
                default:
                    return null;
            }
        }

        public static ConsoleColor GetConsoleColor(this SyntaxToken token)
        {
            switch (token.Kind)
            {
                case SyntaxKind.IncorrectToken:
                    return ConsoleColor.Red;
                case SyntaxKind.IntegerLiteralToken:
                    return ConsoleColor.Yellow;
                case SyntaxKind.IdentifierToken:
                    return ConsoleColor.Cyan;
                case SyntaxKind.StringLiteralToken:
                    return ConsoleColor.DarkYellow;
                default:
                    if (token.Kind.ToString().EndsWith("Keyword"))
                        return ConsoleColor.DarkCyan;
                    else
                        return ConsoleColor.Gray;
            }
        }
    }
}
