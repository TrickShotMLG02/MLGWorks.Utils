namespace MLGWorks.Utils.Patterns
{
    /// <summary>
    /// Represents an event in the event bus system.
    /// </summary>
    public interface IEvent
    {
        /// <summary>
        /// Gets the name of the event.
        /// </summary>
        string Name { get; }
    }
}
