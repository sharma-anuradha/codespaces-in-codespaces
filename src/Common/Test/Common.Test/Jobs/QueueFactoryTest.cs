using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Test
{
    public abstract class QueueFactoryTest : QueueTestBase
    {
        protected QueueFactoryTest(IQueueFactory queueFactory)
            : base(queueFactory)
        {
        }

        [Fact]
        public void Create()
        {
            Assert.Throws<ArgumentNullException>(() => QueueFactory.GetOrCreate(null));
            Assert.Throws<ArgumentException>(() => QueueFactory.GetOrCreate(String.Empty));
            var queue = QueueFactory.GetOrCreate("queue1");
            var otherQueue = QueueFactory.GetOrCreate("queue1");
            Assert.Same(queue, otherQueue);
        }

        [Fact]
        public Task TestAsync()
        {
            return RunQueueTest(async (queue) =>
            {
                var messages = await queue.GetMessagesAsync(10, null, TimeSpan.FromMilliseconds(5), default);
                Assert.Empty(messages);

                var message1Content = CreateMessageContent("message#1");
                var message1 = await queue.AddMessageAsync(message1Content, null, default);
                var message2 = await queue.AddMessageAsync(CreateMessageContent("message#2"), null, default);

                messages = await queue.GetMessagesAsync(10, null, TimeSpan.Zero, default);
                Assert.NotEmpty(messages);
                Assert.Equal(2, messages.Count());
                AssertEqualMessage(messages.First(), message1);
                AssertEqualMessage(messages.Skip(1).First(), message2);

                var visibleTimeout = TimeSpan.FromMilliseconds(500);
                await queue.AddMessageAsync(message1Content, null, default);
                messages = await queue.GetMessagesAsync(10, visibleTimeout, TimeSpan.Zero, default);
                Assert.Single(messages);
                messages = await queue.GetMessagesAsync(10, null, TimeSpan.Zero, default);
                Assert.Empty(messages);

                await Task.Delay(1500);
                messages = await queue.GetMessagesAsync(10, visibleTimeout, TimeSpan.Zero, default);
                Assert.Single(messages);

                await queue.DeleteMessageAsync(messages.First(), default);
            });
        }

        private static byte[] CreateMessageContent(string content)
        {
            return Encoding.UTF8.GetBytes(content);
        }

        private static void AssertEqualMessage(QueueMessage queueMessage1, QueueMessage queueMessage2)
        {
            Assert.Equal(queueMessage1.Id, queueMessage2.Id);
            Assert.Equal(queueMessage1.Content, queueMessage2.Content);
        }
    }
}
