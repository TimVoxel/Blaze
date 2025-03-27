using System.Diagnostics;

namespace Blaze.Emit.Nodes
{
    public class TellrawCommand : CommandNode 
    {
        public override string Keyword => "tellraw";
        public string Selector { get; }
        public string Component { get; }

        public override string Text => $"{Keyword} {Selector} {Component}";
        public override EmittionNodeKind Kind => EmittionNodeKind.TellrawCommand;

        public TellrawCommand(string selector, string component)
        {
            Selector = selector;
            Component = component;
        }
    }
}
