using MLGWorks.Utils.Helpers.Timing;
using NUnit.Framework;
using System;

namespace MLGWorks.Utils.Tests.Helpers.Timing
{
    public class TimingTests
    {
        [Test]
        public void Cooldown_StartsReadyAndBlocksUntilElapsed()
        {
            var cooldown = new Cooldown(2f);

            Assert.IsTrue(cooldown.IsReady);
            Assert.IsTrue(cooldown.TryConsume());
            Assert.IsFalse(cooldown.TryConsume());

            cooldown.Update(1f);
            Assert.IsFalse(cooldown.IsReady);
            cooldown.Update(1f);

            Assert.IsTrue(cooldown.IsReady);
            Assert.AreEqual(0f, cooldown.Remaining);
            Assert.IsTrue(cooldown.TryConsume());
        }

        [Test]
        public void Cooldown_UpdateClampsAndResetReturnsReady()
        {
            var cooldown = new Cooldown(1f);
            cooldown.Start();
            cooldown.Update(5f);
            cooldown.Reset();

            Assert.AreEqual(0f, cooldown.Remaining);
            Assert.IsFalse(cooldown.IsRunning);
            Assert.IsTrue(cooldown.IsReady);
        }

        [Test]
        public void Cooldown_InvalidInputsThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Cooldown(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Cooldown(float.NaN));

            var cooldown = new Cooldown(1f);
            Assert.Throws<ArgumentOutOfRangeException>(() => cooldown.Update(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => cooldown.Update(float.NaN));
        }

        [Test]
        public void RateLimiter_AllowsUpToLimitAndExpiresAtWindowBoundary()
        {
            var limiter = new RateLimiter(2, 10d);

            Assert.IsTrue(limiter.TryAcquire(0d));
            Assert.IsTrue(limiter.TryAcquire(1d));
            Assert.IsFalse(limiter.TryAcquire(2d));
            Assert.AreEqual(0, limiter.RemainingUses);

            Assert.IsTrue(limiter.TryAcquire(10d));
            Assert.AreEqual(2, limiter.UsedUses);
        }

        [Test]
        public void RateLimiter_ResetClearsWindow()
        {
            var limiter = new RateLimiter(1, 5d);
            Assert.IsTrue(limiter.TryAcquire(1d));
            limiter.Reset();

            Assert.AreEqual(1, limiter.RemainingUses);
            Assert.IsTrue(limiter.TryAcquire(0d));
        }

        [Test]
        public void RateLimiter_RejectsInvalidConfigurationAndBackwardsTime()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RateLimiter(0, 1d));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RateLimiter(1, 0d));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RateLimiter(1, double.NaN));

            var limiter = new RateLimiter(1, 1d);
            limiter.TryAcquire(2d);
            Assert.Throws<ArgumentOutOfRangeException>(() => limiter.TryAcquire(1d));
        }
    }
}
