using NUnit.Framework;
using MLGWorks.Utils.Patterns;

namespace MLGWorks.Utils.Tests.Patterns
{
    public class TimerTests
    {
        [Test]
        public void Timer_Starts_And_Completes()
        {
            Timer timer = new Timer(2f);
            bool finished = false;
            timer.OnFinished += () => finished = true;

            timer.Start();
            timer.Update(1f);
            Assert.IsFalse(finished);
            Assert.IsFalse(timer.IsFinished);

            timer.Update(1f);
            Assert.IsTrue(finished);
            Assert.IsTrue(timer.IsFinished);
        }

        [Test]
        public void Timer_Can_Be_Paused_And_Resumed()
        {
            Timer timer = new Timer(2f);
            timer.Start();
            timer.Update(1f);

            timer.Pause();
            timer.Update(1f); // Should not advance time

            Assert.IsFalse(timer.IsFinished);
            Assert.Less(timer.Elapsed, 2f);

            timer.Resume();
            timer.Update(1f);
            Assert.IsTrue(timer.IsFinished);
        }

        [Test]
        public void Timer_Can_Be_Reset()
        {
            Timer timer = new Timer(1f);
            timer.Start();
            timer.Update(1f);
            timer.Reset();

            Assert.AreEqual(0f, timer.Elapsed);
            Assert.IsFalse(timer.IsRunning);
            Assert.IsFalse(timer.IsPaused);
        }
    }
}
