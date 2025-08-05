using System;
using System.Collections.Generic;

namespace MLGWorks.Utils.Patterns
{
    #region Interfaces

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

    #endregion Interfaces

    /// <summary>
    /// A static event bus for subscribing to, unsubscribing from, and publishing events of various types.
    /// Allows decoupled communication between event publishers and subscribers.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Action<IEvent>>> _subscribers = new();
        private static readonly Dictionary<Delegate, Action<IEvent>> _delegateMap = new();

        /// <summary>
        /// Subscribes to a specific event type.
        /// </summary>
        /// <typeparam name="T">The type of the event to subscribe to.</typeparam>
        /// <param name="callback">The callback to invoke when the event is published.</param>
        /// <example>
        /// Here's how to subscribe to a <c>PlayerDiedEvent</c>:
        /// <code>
        /// void OnPlayerDied(PlayerDiedEvent e)
        /// {
        ///     Debug.Log($"\{e.PlayerName\} has died.");
        /// }
        ///
        /// EventBus.Subscribe&lt;PlayerDiedEvent&gt;(OnPlayerDied);
        /// </code>
        /// </example>
        public static void Subscribe<T>(Action<T> callback) where T : IEvent
        {
            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list))
            {
                _subscribers[type] = list = new List<Action<IEvent>>();
            }

            Action<IEvent> wrapper = e => callback((T)e);
            _delegateMap[callback] = wrapper;
            list.Add(wrapper);
        }

        /// <summary>
        /// Unsubscribes a previously registered callback from the specified event type.
        /// </summary>
        /// <typeparam name="T">The type of the event to unsubscribe from. Must implement <see cref="IEvent"/>.</typeparam>
        /// <param name="callback">The callback method to remove from the event's subscriber list.</param>
        /// <remarks>
        /// If the callback was not previously subscribed or has already been removed, this method has no effect.
        /// </remarks>
        /// <example>
        /// Example of unsubscribing from a <c>PlayerDiedEvent</c>:
        /// <code>
        /// void OnPlayerDied(PlayerDiedEvent e)
        /// {
        ///     Debug.Log($"{e.PlayerName} has died.");
        /// }
        ///
        /// // Subscribe to the event
        /// EventBus.Subscribe&lt;PlayerDiedEvent&gt;(OnPlayerDied);
        ///
        /// // Unsubscribe from the event
        /// EventBus.Unsubscribe&lt;PlayerDiedEvent&gt;(OnPlayerDied);
        /// </code>
        /// </example>

        public static void Unsubscribe<T>(Action<T> callback) where T : IEvent
        {
            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out var list) &&
                _delegateMap.TryGetValue(callback, out var wrapper))
            {
                list.Remove(wrapper);
                _delegateMap.Remove(callback);
            }
        }

        /// <summary>
        /// Publishes an event to all subscribers of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the event to publish. Must implement <see cref="IEvent"/>.</typeparam>
        /// <param name="evt">The event instance to publish.</param>
        /// <example>
        /// Example of publishing a <c>PlayerDiedEvent</c>:
        /// <code>
        /// var evt = new PlayerDiedEvent("Alice");
        /// EventBus.Publish(evt);
        /// </code>
        /// </example>

        public static void Publish<T>(T evt) where T : IEvent
        {
            Type type = typeof(T);
            if (_subscribers.TryGetValue(type, out var list))
            {
                foreach (var action in list)
                {
                    action.Invoke(evt);
                }
            }
        }
    }
}
