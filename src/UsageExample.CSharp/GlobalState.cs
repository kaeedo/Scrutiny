namespace UsageExample.CSharp
{
    public class GlobalState
    {
        public bool IsSignedIn { get; set; }
        public string Username { get; set; }
        public int Number { get; } = 42;
    }
}
