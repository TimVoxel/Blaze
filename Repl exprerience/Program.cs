namespace ReplExperience
{
    internal static class Program
    {
        private static void Main()
        {
            Repl repl = new DPPRepl();
            repl.Run();
        }
    }
}