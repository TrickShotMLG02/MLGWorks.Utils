using MLGWorks.Utils.Patterns;
using NUnit.Framework;

namespace MLGWorks.Utils.Tests.Patterns
{
    public class EventBusTests
    {
        private int _receivedValue;
        private int _callCount;

        [SetUp]
        public void Setup()
        {
            _receivedValue = 0;
            _callCount = 0;

            // Unsubscribe any previous subscriptions to avoid test pollution
            EventBus.Unsubscribe<TestEvent>(OnTestEvent);
        }

        [Test]
        public void Subscribe_And_Publish_Event_Invokes_Callback()
        {
            EventBus.Subscribe<TestEvent>(OnTestEvent);

            var evt = new TestEvent { Value = 42 };
            EventBus.Publish(evt);

            Assert.AreEqual(42, _receivedValue);
            Assert.AreEqual(1, _callCount);

            EventBus.Unsubscribe<TestEvent>(OnTestEvent);
        }

        [Test]
        public void Unsubscribe_Event_Callback_No_Long_Invoked()
        {
            EventBus.Subscribe<TestEvent>(OnTestEvent);
            EventBus.Unsubscribe<TestEvent>(OnTestEvent);

            var evt = new TestEvent { Value = 99 };
            EventBus.Publish(evt);

            Assert.AreEqual(0, _receivedValue);
            Assert.AreEqual(0, _callCount);
        }

        [Test]
        public void Multiple_Subscribers_All_Receive_Event()
        {
            int secondSubscriberValue = 0;
            int secondSubscriberCalls = 0;

            void SecondSubscriber(TestEvent e)
            {
                secondSubscriberValue = e.Value;
                secondSubscriberCalls++;
            }

            EventBus.Subscribe<TestEvent>(OnTestEvent);
            EventBus.Subscribe<TestEvent>(SecondSubscriber);

            var evt = new TestEvent { Value = 7 };
            EventBus.Publish(evt);

            Assert.AreEqual(7, _receivedValue);
            Assert.AreEqual(1, _callCount);

            Assert.AreEqual(7, secondSubscriberValue);
            Assert.AreEqual(1, secondSubscriberCalls);

            EventBus.Unsubscribe<TestEvent>(OnTestEvent);
            EventBus.Unsubscribe<TestEvent>(SecondSubscriber);
        }

        private void OnTestEvent(TestEvent e)
        {
            _receivedValue = e.Value;
            _callCount++;
        }
    }
}
