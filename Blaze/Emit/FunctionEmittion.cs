namespace Blaze.Emit
{
    public class FunctionEmittion
    {
        private int _subCount;

        public string Name { get; }
        public string Body { get; set; }
        public List<FunctionEmittion> Children { get; }

        public FunctionEmittion(string name)
        {
            Name = name;
            Body = string.Empty;
            Children = new List<FunctionEmittion>();
            _subCount = 0;
        }

        public void AppendLine(string line)
        {
            Body += line + Environment.NewLine;
        }

        public void AppendLine()
        {
            Body += Environment.NewLine;
        }

        public string GetFreeSubName()
        {
            _subCount++;
            return $"{Name}_sw{_subCount}";
        }

        /*
        public string GetCallName()
        {
            var stack = new Stack<FunctionEmittion>();
            var current = Parent;

            stack.Push(this);

            while (current != null)
            {
                stack.Push(current);
                current = Parent;
            }

            var callName = new StringBuilder("ns:");
            while (stack.Any())
            {
                var isLast = stack.Count == 1;
                var thisEmittion = stack.Pop();
                var ending = isLast ? string.Empty : "/";
                callName.Append($"{thisEmittion.Name}{ending}");
            }
            return callName.ToString();
        }
        */
    }
}
