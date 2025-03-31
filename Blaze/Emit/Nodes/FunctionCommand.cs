using Blaze.Emit.Data;
using Blaze.Symbols;
using System.Diagnostics;

namespace Blaze.Emit.Nodes
{
    public class FunctionCommand : CommandNode
    {
        public abstract class FunctionWithClause
        {
            public abstract string Text { get; }
        }

        public class FunctionWithPathIdentifierClause : FunctionWithClause
        {
            public ObjectPathIdentifier Identifier { get; }

            public override string Text => $"with {Identifier.Text}";

            public FunctionWithPathIdentifierClause(ObjectPathIdentifier identifier)
            {
                Identifier = identifier;
            }    
        }

        public class FunctionWithArgumentsClause : FunctionWithClause
        {
            public string Arguments { get; }

            public override string Text => Arguments;

            public FunctionWithArgumentsClause(string arguments)
            {
                Arguments = arguments;
            }
        }

        public override EmittionNodeKind Kind => EmittionNodeKind.FunctionCommand;
        public override string Keyword => "function";
        public string FunctionCallName { get; }
        
        public FunctionWithClause? WithClause { get; }

        public override string Text
        {
            get
            {
                if (WithClause == null)
                    return $"{Keyword} {FunctionCallName}";
                else
                    return $"{Keyword} {FunctionCallName} {WithClause.Text}";
            }
        }

        public FunctionCommand(string function, FunctionWithClause? withClause)
        {
            FunctionCallName = function;
            WithClause = withClause;
        }
    }
}
