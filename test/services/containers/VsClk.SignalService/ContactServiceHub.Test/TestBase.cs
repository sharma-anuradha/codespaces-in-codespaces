using System;
using System.Linq;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.VsCloudKernel.SignalService.PresenceServiceHubTests
{
    public class TestBase
    {
        protected static ContactReference AsContactRef(string connectionId, string id ) => new ContactReference(id, connectionId);
        protected static void AssertContactRef(string connectionId, string id, ContactReference contactReference)
        {
            Assert.Equal(AsContactRef(connectionId, id), contactReference);
        }

        protected static void AssertContactRef(string connectionId, string id, object contactReference)
        {
            Assert.IsType<ContactReference>(contactReference);
            AssertContactRef(connectionId, id, (ContactReference)contactReference);
        }

        protected static string CreateChangeId() => Guid.NewGuid().ToString();

        protected static Dictionary<string, object> CreateWithEmailProperty(string email)
        {
            return new Dictionary<string, object>()
            {
                { "email", email },
            };
        }

        protected static Dictionary<string, object>[] CreateWithEmailsProperty(params string[] emails)
        {
            return emails.Select(email => CreateWithEmailProperty(email)).ToArray();
        }
    }
}
