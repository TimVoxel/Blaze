namespace Blaze.Emit
{
    public class FunctionEmittion
    {
        private int _loopCount;
        private int _ifCount;
        private int _elseCount;

        public string Name { get; }
        public string Body { get; private set; }
        public string CleanUp { get; private set; }

        public List<FunctionEmittion> Children { get; }

        public FunctionEmittion(string name)
        {
            Name = name;
            Body = string.Empty;
            CleanUp = string.Empty;
            Children = new List<FunctionEmittion>();
            _loopCount = 0;
            _ifCount = 0;
            _elseCount = 0;
        }

        public void Append(string text)
        {
            Body += text;
        }

        public void AppendLine(string line)
        {
            Body += line + Environment.NewLine;
        }

        public void AppendLine() => Body += Environment.NewLine;
        public void AppendComment(string text) => AppendLine($"#{text}");

        public void AppendCleanUp(string line)
        {
            if (!CleanUp.Contains(line))
                CleanUp += line + Environment.NewLine;
        }

        public string GetFreeSubIfName()
        {
            _ifCount++;
            return $"{Name}_sif{_ifCount}"; 
        }

        public string GetFreeSubElseName()
        {
            _elseCount++;
            return $"{Name}_sel{_elseCount}";
        }

        public string GetFreeSubLoopName()
        {
            _loopCount++;
            return $"{Name}_sw{_loopCount}";
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
