using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.VsCloudKernel.SignalService.PresenceServiceHubTests
{
    using ConnectionProperties = IDictionary<string, PropertyValue>;
    using ContactDataInfo = IDictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>;

    public class BackplaneManagerTests : TestBase
    {
        private readonly ContactBackplaneManager contactBackplaneManager;
        private readonly Mock<IContactBackplaneProvider> mockContactBackplaneProvider1;
        private readonly Mock<IContactBackplaneProvider> mockContactBackplaneProvider2;

        public BackplaneManagerTests()
        {
            var backplaneServiceManagerLogger = new Mock<ILogger<ContactBackplaneManager>>();
            this.contactBackplaneManager = new ContactBackplaneManager(backplaneServiceManagerLogger.Object);
            this.mockContactBackplaneProvider1 = new Mock<IContactBackplaneProvider>();

            this.mockContactBackplaneProvider2 = new Mock<IContactBackplaneProvider>();

            this.contactBackplaneManager.RegisterProvider(this.mockContactBackplaneProvider1.Object);
            this.contactBackplaneManager.RegisterProvider(this.mockContactBackplaneProvider2.Object);
        }

        [Fact]
        public async Task UpdateContactTest()
        {
            var bUpdated1 = false;
            var bUpdated2 = false;
            SetupUpdateContactAsync(this.mockContactBackplaneProvider1, (contactDataChanged) =>
            {
                bUpdated1 = true;
                return (ContactDataInfo)null;
            });
            SetupUpdateContactAsync(this.mockContactBackplaneProvider2, (contactDataChanged) =>
            {
                bUpdated2 = true;
                return (ContactDataInfo)null;
            });

            await this.contactBackplaneManager.UpdateContactAsync(new ContactDataChanged<ConnectionProperties>(
                "change1",
                "serviceId1",
                "conn1",
                "contact1",
                ContactUpdateType.Registration,
                new Dictionary<string, PropertyValue>()
                {
                    {"propert1", new PropertyValue(100, DateTime.Now) }
                }), CancellationToken.None);

            Assert.True(bUpdated1);
            Assert.True(bUpdated2);

            SetupUpdateContactAsync(this.mockContactBackplaneProvider1, async (contactDataChanged) =>
            {
                bUpdated1 = true;
                await Task.Delay(0);
                throw new NotSupportedException();
            });
            SetupUpdateContactAsync(this.mockContactBackplaneProvider2, async (contactDataChanged) =>
            {
                bUpdated2 = true;
                await Task.Delay(0);
                throw new NotSupportedException();
            });
            Exception error1 = null;
            SetupHandleException(this.mockContactBackplaneProvider1, (methodName, error) =>
            {
                error1 = error;
                return false;
            });
            Exception error2 = null;
            SetupHandleException(this.mockContactBackplaneProvider2, (methodName, error) =>
            {
                error2 = error;
                return false;
            });

            bUpdated1 = false;
            bUpdated2 = false;

            await this.contactBackplaneManager.UpdateContactAsync(new ContactDataChanged<ConnectionProperties>(
                "change1",
                "serviceId1",
                "conn1",
                "contact1",
                ContactUpdateType.Registration,
                new Dictionary<string, PropertyValue>()
                {
                    {"propert1", new PropertyValue(100, DateTime.Now) }
                }), CancellationToken.None);

            Assert.True(bUpdated1);
            Assert.True(bUpdated2);
            Assert.NotNull(error1);
            Assert.NotNull(error2);
        }

        [Fact]
        public async Task GetContactsTest()
        {
            Func<Dictionary<string, object>, Dictionary<string, ContactDataInfo>> matchEmailCallback =
            (matchProperties) =>
            {
                object email;
                if (!matchProperties.TryGetValue("email", out email) || !"name@gmail.com".Equals(email))
                {
                    return null;
                }

                return new Dictionary<string, ContactDataInfo>()
                {
                    {
                        "contact1", new Dictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>()
                        {
                            { "serviceId1", new Dictionary<string, IDictionary<string, PropertyValue>>()
                                {
                                    { "conn1", new Dictionary<string, PropertyValue>()
                                        {
                                            {"propert1", new PropertyValue(100, DateTime.Now) }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
            };

            SetupGetContactsAsync(this.mockContactBackplaneProvider1, (matchProperties) =>
            {
                return (Dictionary<string, ContactDataInfo>)null;
            });

            SetupGetContactsAsync(this.mockContactBackplaneProvider2, matchEmailCallback);

            var result = await this.contactBackplaneManager.GetContactsDataAsync(
                CreateWithEmailsProperty("name@gmail.com"), CancellationToken.None);

            Assert.True(result[0].ContainsKey("contact1"));

            result = await this.contactBackplaneManager.GetContactsDataAsync(
                CreateWithEmailsProperty("other@gmail.com"), CancellationToken.None);
            Assert.Null(result[0]);

            SetupGetContactsAsync(this.mockContactBackplaneProvider1, async (matchProperties) =>
            {
                await Task.Delay(0);
                throw new NotSupportedException();
            });

            SetupGetContactsAsync(this.mockContactBackplaneProvider2, async (matchProperties) =>
            {
                await Task.Delay(5);
                return matchEmailCallback(matchProperties);
            });

            string methodNameException = null;
            Exception errorException = null;

            SetupHandleException(this.mockContactBackplaneProvider1, (methodName, error) =>
            {
                methodNameException = methodName;
                errorException = error;
                return false;
            });

            result = await this.contactBackplaneManager.GetContactsDataAsync(CreateWithEmailsProperty("name@gmail.com"), CancellationToken.None);

            Assert.True(result[0].ContainsKey("contact1"));
            Assert.Equal(nameof(IContactBackplaneProvider.GetContactsDataAsync), methodNameException);
            Assert.NotNull(errorException);
        }

        private static void SetupUpdateContactAsync(Mock<IContactBackplaneProvider> mockContactBackplaneProvider,
            Func<ContactDataChanged<ConnectionProperties>, Task<ContactDataInfo>> callback)
        {
            mockContactBackplaneProvider.Setup(i => i.UpdateContactAsync(
                It.IsAny<ContactDataChanged<ConnectionProperties>>(),
                It.IsAny<CancellationToken>()))
            .Returns((ContactDataChanged<ConnectionProperties> contactDataChanged, CancellationToken cancellationToken) =>
            {
                return callback(contactDataChanged);
            });
        }

        private static void SetupUpdateContactAsync(Mock<IContactBackplaneProvider> mockContactBackplaneProvider,
            Func<ContactDataChanged<ConnectionProperties>, ContactDataInfo> callback)
        {
            SetupUpdateContactAsync(mockContactBackplaneProvider, (contactDataChanges) => Task.FromResult(callback(contactDataChanges)));
        }

        private static void SetupGetContactsAsync(Mock<IContactBackplaneProvider> mockContactBackplaneProvider,
                Func<Dictionary<string, object>, Task<Dictionary<string, ContactDataInfo>>> callback)
        {
            mockContactBackplaneProvider.Setup(i => i.GetContactsDataAsync(
                It.IsAny<Dictionary<string, object>[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (Dictionary<string, object>[] matchProperties, CancellationToken cancellationToken) =>
            {
                var results = new Dictionary<string, ContactDataInfo>[matchProperties.Length];
                for (int index = 0;index < matchProperties.Length; ++index)
                {
                    results[index] = await callback(matchProperties[index]);
                }

                return results;
            });
        }

        private static void SetupGetContactsAsync(Mock<IContactBackplaneProvider> mockContactBackplaneProvider,
            Func<Dictionary<string, object>, Dictionary<string, ContactDataInfo>> callback)
        {
            SetupGetContactsAsync(mockContactBackplaneProvider, (matchProperties) => Task.FromResult(callback(matchProperties)));
        }

        private static void SetupHandleException(Mock<IContactBackplaneProvider> mockContactBackplaneProvider,
        Func<string, Exception,bool> callback)
        {
            mockContactBackplaneProvider.Setup(i => i.HandleException(
                It.IsAny<string>(),
                It.IsAny<Exception>()))
            .Returns((string methodName, Exception error) =>
            {
                return callback(methodName, error);
            });
        }
    }
}
