namespace ReplExperience
{
    internal static class Program
    {
        private static void Main()
        {
            Repl repl = new BlazeRepl();
            repl.Run();
        }
    }
}