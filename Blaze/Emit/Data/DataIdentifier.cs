using Blaze.Symbols;

namespace Blaze.Emit.Data
{
    public abstract class DataIdentifier
    {
        public abstract string Text { get; }
        public abstract DataLocation Location { get; }
    }
}
