using MLGWorks.Utils.Helpers;
using NUnit.Framework;

namespace MLGWorks.Utils.Tests.Helpers
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

        [Test]
        public void Timer_DoesNotAdvanceBeforeStartOrAfterCompletion()
        {
            var timer = new Timer(1f);

            timer.Update(1f);
            Assert.AreEqual(0f, timer.Elapsed);

            timer.Start();
            timer.Update(1f);
            timer.Update(1f);

            Assert.AreEqual(1f, timer.Elapsed);
            Assert.IsTrue(timer.IsFinished);
        }

        [Test]
        public void Timer_ZeroDuration_FinishesOnFirstUpdate()
        {
            var timer = new Timer(0f);
            int finishedCount = 0;
            timer.OnFinished += () => finishedCount++;

            timer.Start();
            timer.Update(0f);
            timer.Update(0f);

            Assert.IsTrue(timer.IsFinished);
            Assert.AreEqual(1, finishedCount);
        }

        [Test]
        public void Timer_Start_RestartsFromZero()
        {
            var timer = new Timer(2f);
            timer.Start();
            timer.Update(1f);
            timer.Start();

            Assert.AreEqual(0f, timer.Elapsed);
            Assert.IsTrue(timer.IsRunning);
            Assert.IsFalse(timer.IsPaused);
        }
    }
}
