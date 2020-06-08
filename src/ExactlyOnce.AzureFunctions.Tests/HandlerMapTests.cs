using System;
using NUnit.Framework;

namespace ExactlyOnce.AzureFunctions.Tests
{
    public class HandlerMapTests
    {
        [Test]
        public void HandlersNeedsToImplementManagesOfT()
        {
            var handlersMap = new HandlersMap();

            Assert.Throws<ArgumentException>(() => handlersMap.AddHandler<object>());
            Assert.DoesNotThrow(() => handlersMap.AddHandler<SampleHandler>());
        }

        [Test]
        public void FindsHandlerForMessageType()
        {
            var handlerMap = new HandlersMap();

            handlerMap.AddHandler<SampleHandler>();
            var handler = handlerMap.ForMessage(typeof(SampleMessage));

            Assert.AreEqual(typeof(SampleHandler), handler.HandlerType);
            Assert.AreEqual(typeof(SampleHandler.SampleData), handler.DataType);
        }

        class SampleHandler : Manages<SampleHandler.SampleData>, IHandler<SampleMessage>
        {
            public class SampleData {}

            public Guid Map(SampleMessage m)
            {
                throw new NotImplementedException();
            }

            public void Handle(HandlerContext context, SampleMessage message)
            {
                throw new NotImplementedException();
            }
        }

        class SampleMessage{}
    }
}