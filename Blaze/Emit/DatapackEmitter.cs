using Blaze.Binding;
using Blaze.Diagnostics;
using System.Collections.Immutable;

namespace Blaze.Emit
{
    internal static class DatapackEmitter
    {
        public static ImmutableArray<Diagnostic> Emit(BoundProgram program, CompilationConfiguration? configuration)
        {
            if (program.Diagnostics.Any() || configuration == null)
                return program.Diagnostics;


            return program.Diagnostics;
        }
    }
}
