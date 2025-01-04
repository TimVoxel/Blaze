namespace Blaze.Emit.Nodes
{
    public class TextTriviaNode : TextEmittionNode
    {
        public enum TriviaKind
        {
            Comment,
            LineBreak
        }
        
        public override bool IsSingleLine => true;
        public override string Text { get; }
        public override EmittionNodeKind Kind { get; }
        public override bool IsCleanUp => false;

        internal TextTriviaNode(EmittionNodeKind kind, string text) : base()
        {
            Text = text;
            Kind = kind;
        }

        public static TextTriviaNode Comment(string text) => new TextTriviaNode(EmittionNodeKind.CommentTrivia, $"#{text}");
        public static TextTriviaNode LineBreak() => new TextTriviaNode(EmittionNodeKind.LineBreakTrivia, string.Empty);
    }

    /*

    public class ScoreboardPlayersCommand : ScoreboardCommand
    {
        enum SubAction 
        {
            Add,
            Remove,
            Reset
            
        }
           
        internal ScoreboardPlayersCommand()
        {

        }
    }*/
}
