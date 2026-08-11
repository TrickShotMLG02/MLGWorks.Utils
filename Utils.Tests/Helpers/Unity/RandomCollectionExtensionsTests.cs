using System;
using System.Collections.Generic;
using MLGWorks.Utils.Helpers.Unity;
using NUnit.Framework;

namespace MLGWorks.Utils.Tests.Helpers.Unity
{
    public sealed class RandomCollectionExtensionsTests
    {
        [Test]
        public void Shuffle_UsesFisherYatesAndPreservesAllItems()
        {
            var values = new List<int> { 1, 2, 3, 4 };
            values.Shuffle(new SequenceRandom(0, 0, 0));

            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, values);
            CollectionAssert.AreEqual(new[] { 2, 3, 4, 1 }, values);
        }

        [Test]
        public void Shuffled_DoesNotModifySource()
        {
            var source = new[] { "a", "b", "c" };
            List<string> shuffled = source.Shuffled(new SequenceRandom(0, 0));

            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, source);
            CollectionAssert.AreEquivalent(source, shuffled);
        }

        [Test]
        public void WeightedSelection_UsesRelativeWeights()
        {
            var items = new[] { "none", "common", "rare" };
            var weights = new[] { 0f, 2f, 8f };

            Assert.AreEqual("rare", items.SelectWeighted(weights, new SequenceRandom(0.75)));
            Assert.AreEqual("common", items.SelectWeighted(weights, new SequenceRandom(0.1)));
        }

        [Test]
        public void WeightedSelection_RejectsInvalidWeights()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new[] { 1 }.SelectWeighted(new[] { -1f }, new SequenceRandom(0.1)));
            Assert.Throws<ArgumentException>(() =>
                new[] { 1 }.SelectWeighted(Array.Empty<float>(), new SequenceRandom(0.1)));
        }

        [Test]
        public void WeightedSelection_ReturnsFalseForEmptyOrZeroWeights()
        {
            Assert.IsFalse(Array.Empty<int>().TrySelectWeighted(Array.Empty<float>(), new SequenceRandom(0.1), out _));
            Assert.IsFalse(new[] { 1, 2 }.TrySelectWeighted(new[] { 0f, 0f }, new SequenceRandom(0.1), out _));
            Assert.Throws<InvalidOperationException>(() =>
                new[] { 1 }.SelectWeighted(new[] { 0f }, new SequenceRandom(0.1)));
        }

        private sealed class SequenceRandom : System.Random
        {
            private readonly double[] doubles;
            private readonly int[] integers;
            private int doubleIndex;
            private int integerIndex;

            public SequenceRandom(params double[] doubles)
            {
                this.doubles = doubles;
                integers = Array.Empty<int>();
            }

            public SequenceRandom(params int[] integers)
            {
                this.integers = integers;
                doubles = Array.Empty<double>();
            }

            protected override double Sample()
            {
                return doubleIndex < doubles.Length ? doubles[doubleIndex++] : 0d;
            }

            public override int Next(int maxValue)
            {
                return integerIndex < integers.Length ? integers[integerIndex++] : 0;
            }
        }
    }
}
