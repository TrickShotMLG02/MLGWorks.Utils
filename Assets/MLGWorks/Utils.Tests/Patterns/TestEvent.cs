namespace MLGWorks.Utils.Patterns.Tests
{
    public class TestEvent : IEvent
    {
        public string Name => nameof(TestEvent);
        public int Value { get; set; }
    }
}
