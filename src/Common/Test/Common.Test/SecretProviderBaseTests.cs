using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Moq;
using System.Collections;
using System.Runtime.InteropServices.ComTypes;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Test
{
    public class SecretProviderBaseTests
    {

        [Fact]
        public void ConstructorsOK()
        {
            _ = new TestSecretProvider();
            _ = new TestSecretProvider(null);
            _ = new TestSecretProvider(new Dictionary<string, string>());
        }

        [Fact]
        public void SetAndGetSecretValue()
        {
            const string secretName = "xxxx";
            const string secretValue = "yyyy";
            var testSecretProvider = new TestSecretProvider();
            testSecretProvider.SetSecret(secretName, secretValue);
            var result = testSecretProvider.TryGetSecret(secretName, out var value);
            Assert.True(result);
            Assert.Equal(secretValue, value);
        }

        [Fact]
        public void GetUnknownSecretValue()
        {
            const string secretName = "xxxx";
            var testSecretProvider = new TestSecretProvider();
            var result = testSecretProvider.TryGetSecret(secretName, out var value);
            Assert.False(result);
            Assert.Null(value);
        }


        [Fact]
        public async Task SetAndGetSecretValueAsync()
        {
            const string secretName = "xxxx";
            const string secretValue = "yyyy";
            var testSecretProvider = new TestSecretProvider();
            testSecretProvider.SetSecret(secretName, secretValue);
            var value = await testSecretProvider.GetSecretAsync(secretName);
            Assert.Equal(secretValue, value);
        }

        [Fact]
        public async Task GetUnknownSecretValueAsync()
        {
            const string secretName = "xxxx";
            var testSecretProvider = new TestSecretProvider();

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>  _ = await testSecretProvider.GetSecretAsync(secretName));
        }

        [Fact]
        public async Task GetSecretsFromConstructorValuesAsync()
        {
            var initialValues = new Dictionary<string, string>
            {
                { "one", "1" },
                { "two", "2" },
            };
            var testSecretProvider = new TestSecretProvider(initialValues);

            var value = await testSecretProvider.GetSecretAsync("one");
            Assert.Equal("1", value);

            value = await testSecretProvider.GetSecretAsync("two");
            Assert.Equal("2", value);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>  _ = await testSecretProvider.GetSecretAsync("three"));
        }

        private class TestSecretProvider : SecretProviderBase
        {
            public TestSecretProvider() : 
                this(null)
            {
            }

            public TestSecretProvider(IDictionary<string, string> values)
                : base(values)
            {
            }

            public new void SetSecret(string secretName, string secretValue)
            {
                base.SetSecret(secretName, secretValue);
            }

            public new bool TryGetSecret(string secretName, out string secretValue)
            {
                return base.TryGetSecret(secretName, out secretValue);
            }
        }
    }
}

