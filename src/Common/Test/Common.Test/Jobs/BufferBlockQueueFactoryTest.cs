using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Test
{
    public class BufferBlockQueueFactoryTest : QueueFactoryTest
    {
        public BufferBlockQueueFactoryTest()
            : base(new BufferBlockQueueFactory())
        {
        }
    }
}
