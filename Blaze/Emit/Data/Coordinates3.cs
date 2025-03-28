namespace Blaze.Emit.Data
{
    public class Coordinates3 
    {
        public string X { get; }
        public string Y { get; }
        public string Z { get; }

        public string Text => $"{X} {Y} {Z}";

        public Coordinates3(string x, string y, string z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
