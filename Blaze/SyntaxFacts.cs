using Blaze.Syntax_Nodes;

namespace Blaze
{
    public static class SyntaxFacts
    {
        public static SyntaxKind[] AllSyntaxKinds => (SyntaxKind[]) Enum.GetValues(typeof(SyntaxKind));
        public static SyntaxKind[] AllTokenKinds => AllSyntaxKinds.Where(k => IsToken(k)).ToArray();
            
        public static int GetBinaryOperatorPrecedence(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.StarToken:
                case SyntaxKind.SlashToken:
                    return 7;
                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                    return 6;
                case SyntaxKind.LessToken:
                case SyntaxKind.LessOrEqualsToken:
                case SyntaxKind.GreaterToken:
                case SyntaxKind.GreaterOrEqualsToken:
                    return 5;
                case SyntaxKind.DoubleEqualsToken:
                case SyntaxKind.NotEqualsToken:
                    return 4;
                case SyntaxKind.DoubleAmpersandToken:
                    return 3;
                case SyntaxKind.DoublePipeToken:
                    return 2;

                default: return 0;
            }
        }

        public static int GetUnaryOperatorPrecedence(this SyntaxKind kind)
        {
            switch (kind) 
            {
                case SyntaxKind.ExclamationSignToken:
                    return 9;
                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                    return 8;

                default: return 0;
            }
        }

        public static int GetAssignmentOperatorPrecedence(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.DoublePlusToken:
                case SyntaxKind.DoubleMinusToken:
                    return 10;
                case SyntaxKind.PlusEqualsToken:
                case SyntaxKind.MinusEqualsToken:
                case SyntaxKind.StarEqualsToken:
                case SyntaxKind.SlashEqualsToken:
                case SyntaxKind.EqualsToken:
                    return 1;

                default:
                    return 0;
            }
        }

        public static bool IsAllowedInAssignmentLeftHand(this ExpressionSyntax syntax)
        {
            switch (syntax.Kind)
            {
                case SyntaxKind.SimpleNameExpression:
                case SyntaxKind.MemberAccessExpression:
                case SyntaxKind.ParenthesizedExpression:
                    return true;
                default:
                    return false;
            }
        }

        public static bool CanBeAccessed(this ExpressionSyntax syntax)
        {
            //All the primary ones except literals

            switch (syntax.Kind)
            {
                case SyntaxKind.SimpleNameExpression:
                case SyntaxKind.MemberAccessExpression:
                case SyntaxKind.ParenthesizedExpression:
                case SyntaxKind.CallExpression:
                    return true;
                default:
                    return false;
            }
        }

        public static bool CanBeCallIdentifier(this ExpressionSyntax syntax)
        {
            switch (syntax.Kind)
            {
                case SyntaxKind.SimpleNameExpression:
                case SyntaxKind.MemberAccessExpression:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsKeyword(SyntaxKind token)
        {
            return token.ToString().EndsWith("Keyword");
        }

        public static bool IsToken(SyntaxKind kind)
        {
            return !IsTrivia(kind) && (IsKeyword(kind) || kind.ToString().EndsWith("Token"));
        }

        public static bool IsTrivia(SyntaxKind kind)
        {
            return kind switch
            {
                SyntaxKind.WhitespaceTrivia         => true,
                SyntaxKind.SingleLineCommentTrivia  => true,
                SyntaxKind.MultiLineCommentTrivia   => true,
                SyntaxKind.SkippedTextTrivia        => true,
                SyntaxKind.LineBreakTrivia          => true,
                _                                   => false
            };
        }

        public static bool IsComment(SyntaxKind kind) 
            => kind == SyntaxKind.SingleLineCommentTrivia || kind == SyntaxKind.MultiLineCommentTrivia;

        public static bool IsFunctionModifier(SyntaxKind kind)
        {
            return kind switch
            {
                SyntaxKind.TickKeyword => true,
                SyntaxKind.LoadKeyword => true,
                _ => false
            };
        }

        public static SyntaxKind GetKeywordKind(string text)
        {
            return text switch
            {
                "true"      => SyntaxKind.TrueKeyword,
                "false"     => SyntaxKind.FalseKeyword,
                "var"       => SyntaxKind.VarKeyword,
                "if"        => SyntaxKind.IfKeyword,
                "else"      => SyntaxKind.ElseKeyword,
                "while"     => SyntaxKind.WhileKeyword,
                "do"        => SyntaxKind.DoKeyword,
                "for"       => SyntaxKind.ForKeyword,
                "break"     => SyntaxKind.BreakKeyword,
                "continue"  => SyntaxKind.ContinueKeyword,
                "function"  => SyntaxKind.FunctionKeyword,
                "return"    => SyntaxKind.ReturnKeyword,
                "load"      => SyntaxKind.LoadKeyword,
                "tick"      => SyntaxKind.TickKeyword,
                "namespace" => SyntaxKind.NamespaceKeyword,
                "using"     => SyntaxKind.UsingKeyword,
                "new"       => SyntaxKind.NewKeyword,
                _           => SyntaxKind.IdentifierToken,
            };;
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

        public static SyntaxKind GetCorrespondingBinaryOperatorKind(SyntaxKind assignmentOperator)
        {
            return assignmentOperator switch
            {
                SyntaxKind.PlusEqualsToken => SyntaxKind.PlusToken,
                SyntaxKind.MinusEqualsToken => SyntaxKind.MinusToken,
                SyntaxKind.StarEqualsToken => SyntaxKind.StarToken,
                SyntaxKind.SlashEqualsToken => SyntaxKind.SlashToken,
                SyntaxKind.DoublePlusToken => SyntaxKind.PlusToken,
                SyntaxKind.DoubleMinusToken => SyntaxKind.MinusToken,
                _ => assignmentOperator
            };
        }

        public static string? GetText(SyntaxKind kind)
        {
            return kind switch
            {
                SyntaxKind.FalseKeyword => "false",
                SyntaxKind.TrueKeyword => "true",
                SyntaxKind.VarKeyword => "var",
                SyntaxKind.IfKeyword => "if",
                SyntaxKind.ElseKeyword => "else",
                SyntaxKind.WhileKeyword => "while",
                SyntaxKind.DoKeyword => "do",
                SyntaxKind.ForKeyword => "for",
                SyntaxKind.BreakKeyword => "break",
                SyntaxKind.ContinueKeyword => "continue",
                SyntaxKind.FunctionKeyword => "function",
                SyntaxKind.ReturnKeyword => "return",
                SyntaxKind.LoadKeyword => "load",
                SyntaxKind.TickKeyword => "tick",
                SyntaxKind.NamespaceKeyword => "namespace",
                SyntaxKind.UsingKeyword => "using",
                SyntaxKind.NewKeyword => "new",
                SyntaxKind.SemicolonToken => ";",
                SyntaxKind.ColonToken => ":",
                SyntaxKind.CommaToken => ",",
                SyntaxKind.DotToken => ".",
                SyntaxKind.OpenBraceToken => "{",
                SyntaxKind.CloseBraceToken => "}",
                SyntaxKind.PlusToken => "+",
                SyntaxKind.MinusToken => "-",
                SyntaxKind.StarToken => "*",
                SyntaxKind.SlashToken => "/",
                SyntaxKind.OpenParenToken => "(",
                SyntaxKind.CloseParenToken => ")",
                SyntaxKind.ExclamationSignToken => "!",
                SyntaxKind.EqualsToken => "=",
                SyntaxKind.DoubleEqualsToken => "==",
                SyntaxKind.LessToken => "<",
                SyntaxKind.LessOrEqualsToken => "<=",
                SyntaxKind.GreaterToken => ">",
                SyntaxKind.GreaterOrEqualsToken => ">=",
                SyntaxKind.DoubleAmpersandToken => "&&",
                SyntaxKind.DoublePipeToken => "||",
                SyntaxKind.DoubleDotToken => "..",
                SyntaxKind.NotEqualsToken => "!=",
                _ => null,
            };
        }
    }
}
