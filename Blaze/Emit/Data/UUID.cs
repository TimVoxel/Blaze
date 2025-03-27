namespace Blaze.Emit.NameTranslation
{
    internal sealed class UUID 
    {
        public int[] Values { get; }
        private readonly string _stringRepresentation;

        public string TagValue => $"[I; {Values[0]}, {Values[1]}, {Values[2]}, {Values[3]}]";

        public UUID(int uuid1, int uuid2, int uuid3, int uuid4)
        {
            Values = new int[]
            {
                uuid1, 
                uuid2,
                uuid3,
                uuid4
            };

            var unsigned1 = (uint)uuid1;
            var unsigned2 = (uint)uuid2;
            var unsigned3 = (uint)uuid3;
            var unsigned4 = (uint)uuid4;

            var value1 = unsigned1.ToString("X");
            var value2 = unsigned2.ToString("X");
            var value3 = unsigned3.ToString("X");
            var value4 = unsigned4.ToString("X");

            while (value2.Length < 8)
                value2 = value2.Insert(0, "0");

            while (value3.Length < 8)
                value3 = value3.Insert(0, "0");

            while (value4.Length < 8)
                value4 = value4.Insert(0, "0");

            _stringRepresentation = $"{value1}-{value2.Substring(0, 4)}-{value2.Substring(4, 4)}-{value3.Substring(0, 4)}-{value3.Substring(4, 4)}{value4}";
        }

        public override string ToString() => _stringRepresentation;
    }
}