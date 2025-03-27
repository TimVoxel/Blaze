using Blaze.Symbols;
using System.Diagnostics;

namespace Blaze.Emit.Nodes
{
    public class FunctionCommand : CommandNode
    {
        public class FunctionWithClause
        {
            public string? Storage { get; }
            public EmittionVariableSymbol? Symbol { get; }
            public string? JsonLiteral { get; }

            public FunctionWithClause(string storage, EmittionVariableSymbol symbol)
            {
                Storage = storage;
                Symbol = symbol;
            }

            public FunctionWithClause(string jsonLiteral)
            {
                JsonLiteral = jsonLiteral; 
            }

            public string Text
            {
                get
                {
                    if (JsonLiteral != null)
                    {
                        return JsonLiteral;
                    }
                    else
                    {
                        if (Storage != null)
                        {
                            Debug.Assert(Symbol != null);
                            return $"with storage {Storage} {Symbol.SaveName}";
                        }
                        else
                            throw new Exception("No storage but emittion variable symbol is there");
                    }
                }

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

        private FunctionCommand(string function, FunctionWithClause? withClause)
        {
            FunctionCallName = function;
            WithClause = withClause;
        }

        public static FunctionCommand GetCall(string function) => new FunctionCommand(function, null);

        public static FunctionCommand GetCallWith(string function, string storage, EmittionVariableSymbol symbol)
            => new FunctionCommand(function, new FunctionWithClause(storage, symbol));

        public static FunctionCommand GetCallWith(string function, string jsonLiteral)
            => new FunctionCommand(function, new FunctionWithClause(jsonLiteral));
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
