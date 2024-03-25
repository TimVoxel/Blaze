using System.Collections.Immutable;

namespace Blaze.Emit
{
    public class FunctionEmittion
    {
        public string Name { get; }
        public string Body { get; }
        public ImmutableArray<FunctionEmittion> SubFunctions { get; }

        public FunctionEmittion(string name, string body, ImmutableArray<FunctionEmittion> subFunctions)
        {
            Name = name;
            Body = body;
            SubFunctions = subFunctions;
        }
    }
}
