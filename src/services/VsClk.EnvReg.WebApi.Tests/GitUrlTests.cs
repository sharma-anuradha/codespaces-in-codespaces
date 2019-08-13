using System;
using VsClk.EnvReg.Models.DataStore.Compute;
using Xunit;

namespace VsClk.EnvReg.WebApi.Tests
{
    public class GitUrlTests : Repositories.EnvironmentVariableStrategy
    {
        public GitUrlTests() : base(null)
        {
        }

        public override EnvironmentVariable GetEnvironmentVariable()
        {
            throw new NotImplementedException();
        }

        [Theory]
        [InlineData("https://github.com/microsoft/vscode", true)]
        [InlineData("https://github.com/microsoft/vs.code", true)]
        [InlineData("https://github.com/sathishMicrosoft/test.git", true)]
        [InlineData("https://github.com/sathishMicrosoft/test/tree/807d81eb112ccd2277baea1ff09aacc1fd704c72", true)]
        [InlineData("https://github.com/sathishMicrosoft/test/pull/1/commits/807d81eb112ccd2277baea1ff09aacc1fd704c72", true)]
        [InlineData("https://github.com/sathishMicrosoft/test/commit/0abd362eb99c696a74fec56a766ee1910c7598e1", true)]
        [InlineData("https://github.com/sathish-Microsoft/_test.git", true)]
        [InlineData("https://github.com/sathishMicrosoft/test/releases/tag/V10", true)]
        [InlineData("http://github.com/sathishMicrosoft/test.git", true)]
        [InlineData("https://random.visualstudio.com/DefaultCollection/randomGroup/_git/randomProjectName", true)]
        [InlineData("ssh://github.com/sathishMicrosoft/test.git", false)]
        [InlineData("C:/test/path/file.txt", false)]
        [InlineData("file:///C:/test/path/file.txt", false)]
        [InlineData("vsls-contrib/guestbook.git", false)]
        public void GitUrlTest(string url, bool result)
        {
            Assert.Equal(IsValidGitUrl(url), result);
        }
    }
}
