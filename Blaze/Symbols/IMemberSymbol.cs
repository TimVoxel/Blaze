using System.Text;

namespace Blaze.Symbols
{
    public interface IMemberSymbol
    {
        public string Name { get; }
        public IMemberSymbol? Parent { get; }
        public bool IsRoot => Parent == null;

        public string GetFullName();
    }
}
