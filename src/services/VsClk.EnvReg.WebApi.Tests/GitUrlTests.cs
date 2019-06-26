using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VsClk.EnvReg.Models.DataStore.Compute;

namespace VsClk.EnvReg.WebApi.Tests
{
    [TestClass]
    public class GitUrlTests : Repositories.EnvironmentVariableStrategy
    {
        public GitUrlTests() : base(null)
        {
        }

        public override EnvironmentVariable GetEnvironmentVariable()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void GitUrlTest()
        {
            var gitURL = "https://github.com/microsoft/vscode";
            var dotGitURL = "https://github.com/sathishMicrosoft/test.git";
            var treeGitURL = "https://github.com/sathishMicrosoft/test/tree/807d81eb112ccd2277baea1ff09aacc1fd704c72";
            var pullGitURL = "https://github.com/sathishMicrosoft/test/pull/1/commits/807d81eb112ccd2277baea1ff09aacc1fd704c72";
            var commitGitURL = "https://github.com/sathishMicrosoft/test/commit/0abd362eb99c696a74fec56a766ee1910c7598e1";
            var releaseGitURL = "https://github.com/sathishMicrosoft/test/releases/tag/V10";
            var httpGitURL = "http://github.com/sathishMicrosoft/test.git";
            var sshURL = "ssh://github.com/sathishMicrosoft/test.git";
            var badDomainURL = "https://git.com/sathishMicrosoft/test.git";
            var validSymbolsInGitURL = "https://github.com/sathish-Microsoft/_test.git";
            var invalidSymbolsInGitURL = "https://github.com/sathish@Microsoft/%test.git";
            Assert.IsTrue(IsValidGitUrl(gitURL));
            Assert.IsTrue(IsValidGitUrl(dotGitURL));
            Assert.IsTrue(IsValidGitUrl(treeGitURL));
            Assert.IsTrue(IsValidGitUrl(pullGitURL));
            Assert.IsTrue(IsValidGitUrl(commitGitURL));
            Assert.IsTrue(IsValidGitUrl(validSymbolsInGitURL));
            Assert.IsTrue(IsValidGitUrl(releaseGitURL));
            Assert.IsFalse(IsValidGitUrl(httpGitURL));
            Assert.IsFalse(IsValidGitUrl(sshURL));
            Assert.IsFalse(IsValidGitUrl(badDomainURL));
            Assert.IsFalse(IsValidGitUrl(invalidSymbolsInGitURL));
        }
    }
}
