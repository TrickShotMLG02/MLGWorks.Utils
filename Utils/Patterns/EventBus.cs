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
        private static readonly Dictionary<Delegate, List<Action<IEvent>>> _delegateMap = new();

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
        public static IDisposable Subscribe<T>(Action<T> callback) where T : IEvent
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list))
            {
                _subscribers[type] = list = new List<Action<IEvent>>();
            }

            Action<IEvent> wrapper = e => callback((T)e);
            if (!_delegateMap.TryGetValue(callback, out var wrappers))
            {
                _delegateMap[callback] = wrappers = new List<Action<IEvent>>();
            }

            wrappers.Add(wrapper);
            list.Add(wrapper);
            return new EventSubscription(() => RemoveSubscription(type, callback, wrapper));
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
            if (callback == null) return;

            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out var list) &&
                _delegateMap.TryGetValue(callback, out var wrappers) &&
                wrappers.Count > 0)
            {
                RemoveSubscription(type, callback, wrappers[0]);
            }
        }

        private static void RemoveSubscription<T>(Type type, Action<T> callback, Action<IEvent> wrapper)
            where T : IEvent
        {
            if (_subscribers.TryGetValue(type, out var list))
            {
                list.Remove(wrapper);
                if (list.Count == 0)
                {
                    _subscribers.Remove(type);
                }
            }

            if (_delegateMap.TryGetValue(callback, out var wrappers))
            {
                wrappers.Remove(wrapper);
                if (wrappers.Count == 0)
                {
                    _delegateMap.Remove(callback);
                }
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
                // Use a snapshot so a subscriber can safely dispose its subscription
                // or subscribe another callback while the event is being delivered.
                foreach (var action in list.ToArray())
                {
                    action.Invoke(evt);
                }
            }
        }
    }
}
