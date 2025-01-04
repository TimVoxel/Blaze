using Blaze.Diagnostics;
using System.Collections.Immutable;

namespace Blaze.Emit.Nodes
{
    public sealed class Datapack : StructureEmittionNode
    {
        public CompilationConfiguration Configuration { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }

        public override EmittionNodeKind Kind => EmittionNodeKind.Datapack;

        public ImmutableArray<NamespaceEmittionNode> Namespaces { get; }

        public MinecraftFunction InitFunction { get; } 
        public MinecraftFunction TickFunction { get; }


        public Datapack(ImmutableArray<NamespaceEmittionNode> namespaces, CompilationConfiguration configuration, ImmutableArray<Diagnostic> diagnostics, MinecraftFunction initFunction, MinecraftFunction tickFunction) : base(null, configuration.Name)
        {
            Namespaces = namespaces;
            Configuration = configuration;
            Diagnostics = diagnostics;
            InitFunction = initFunction;
            TickFunction = tickFunction;
        }
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
