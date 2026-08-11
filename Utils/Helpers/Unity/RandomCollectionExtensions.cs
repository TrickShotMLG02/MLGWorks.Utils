using System;
using System.Collections.Generic;

namespace MLGWorks.Utils.Helpers.Unity
{
    /// <summary>Provides random selection and Fisher-Yates shuffle operations for collections.</summary>
    public static class RandomCollectionExtensions
    {
        private static readonly object RandomLock = new object();
        private static readonly Random DefaultRandom = new Random();

        /// <summary>Shuffles a list in place using the Fisher-Yates algorithm.</summary>
        /// <typeparam name="T">The list item type.</typeparam>
        /// <param name="list">The list to shuffle.</param>
        /// <param name="random">The random source used to choose swap positions.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="list"/> or <paramref name="random"/> is null.</exception>
        public static void Shuffle<T>(this IList<T> list, Random random)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (random == null) throw new ArgumentNullException(nameof(random));
            for (int index = list.Count - 1; index > 0; index--)
            {
                int swapIndex = random.Next(index + 1);
                if (swapIndex == index) continue;
                (list[index], list[swapIndex]) = (list[swapIndex], list[index]);
            }
        }

        /// <summary>Shuffles a list in place using a process-local random source.</summary>
        /// <typeparam name="T">The list item type.</typeparam>
        /// <param name="list">The list to shuffle.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="list"/> is null.</exception>
        public static void Shuffle<T>(this IList<T> list)
        {
            lock (RandomLock) Shuffle(list, DefaultRandom);
        }

        /// <summary>Creates a shuffled copy without modifying the source sequence.</summary>
        /// <typeparam name="T">The sequence item type.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="random">The random source used to choose swap positions.</param>
        /// <returns>A new list containing the source items in randomized order.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="random"/> is null.</exception>
        public static List<T> Shuffled<T>(this IEnumerable<T> source, Random random)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (random == null) throw new ArgumentNullException(nameof(random));
            var result = new List<T>(source);
            result.Shuffle(random);
            return result;
        }

        /// <summary>Creates a shuffled copy using a process-local random source.</summary>
        /// <typeparam name="T">The sequence item type.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <returns>A new list containing the source items in randomized order.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public static List<T> Shuffled<T>(this IEnumerable<T> source)
        {
            lock (RandomLock) return Shuffled(source, DefaultRandom);
        }

        /// <summary>Attempts to select an item according to parallel non-negative weights.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="items">The items to choose from.</param>
        /// <param name="weights">The weight for each item.</param>
        /// <param name="random">The random source used for selection.</param>
        /// <param name="selected">The selected item, or the default value when selection is not possible.</param>
        /// <returns><see langword="true"/> when an item was selected; otherwise, <see langword="false"/> for an empty collection or zero total weight.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown when item and weight counts differ.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when a weight is negative, NaN, or infinite.</exception>
        public static bool TrySelectWeighted<T>(this IReadOnlyList<T> items, IReadOnlyList<float> weights, Random random, out T selected)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (weights == null) throw new ArgumentNullException(nameof(weights));
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (items.Count != weights.Count) throw new ArgumentException("Items and weights must have the same count.", nameof(weights));
            double totalWeight = 0d;
            for (int index = 0; index < weights.Count; index++)
            {
                float weight = weights[index];
                if (float.IsNaN(weight) || float.IsInfinity(weight) || weight < 0f)
                    throw new ArgumentOutOfRangeException(nameof(weights), "Weights must be finite and non-negative.");
                totalWeight += weight;
            }
            if (items.Count == 0 || totalWeight <= 0d)
            {
                selected = default;
                return false;
            }
            double target = random.NextDouble() * totalWeight;
            double cumulativeWeight = 0d;
            for (int index = 0; index < items.Count; index++)
            {
                cumulativeWeight += weights[index];
                if (target < cumulativeWeight)
                {
                    selected = items[index];
                    return true;
                }
            }
            selected = items[items.Count - 1];
            return true;
        }

        /// <summary>Attempts to select an item using a process-local random source.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="items">The items to choose from.</param>
        /// <param name="weights">The weight for each item.</param>
        /// <param name="selected">The selected item, or the default value when selection is not possible.</param>
        /// <returns><see langword="true"/> when an item was selected; otherwise, <see langword="false"/>.</returns>
        public static bool TrySelectWeighted<T>(this IReadOnlyList<T> items, IReadOnlyList<float> weights, out T selected)
        {
            lock (RandomLock) return TrySelectWeighted(items, weights, DefaultRandom, out selected);
        }

        /// <summary>Selects an item according to parallel non-negative weights.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="items">The items to choose from.</param>
        /// <param name="weights">The weight for each item.</param>
        /// <param name="random">The random source used for selection.</param>
        /// <returns>The selected item.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the collection is empty or all weights are zero.</exception>
        public static T SelectWeighted<T>(this IReadOnlyList<T> items, IReadOnlyList<float> weights, Random random)
        {
            if (!TrySelectWeighted(items, weights, random, out T selected))
                throw new InvalidOperationException("At least one item with a positive weight is required.");
            return selected;
        }

        /// <summary>Selects an item using a process-local random source.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="items">The items to choose from.</param>
        /// <param name="weights">The weight for each item.</param>
        /// <returns>The selected item.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the collection is empty or all weights are zero.</exception>
        public static T SelectWeighted<T>(this IReadOnlyList<T> items, IReadOnlyList<float> weights)
        {
            lock (RandomLock) return SelectWeighted(items, weights, DefaultRandom);
        }
    }
}
