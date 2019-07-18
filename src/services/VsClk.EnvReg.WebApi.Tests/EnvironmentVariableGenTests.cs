using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VsCloudKernel.Services.EnvReg.Models;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using System;
using VsClk.EnvReg.Repositories;

namespace VsClk.EnvReg.WebApi.Tests
{
    [TestClass]
    public class EnvironmentVariableGenTests
    {
        private EnvironmentRegistration GetGitEnvForGitRepo()
        {
            return new EnvironmentRegistration()
            {
                Seed = new SeedInfo()
                {
                    SeedType = "git",
                    SeedMoniker = "https://github.com/microsoft/vscode"
                }
            };
        }

        private EnvironmentRegistration GetGitEnvForGitPull()
        {
            return new EnvironmentRegistration()
            {
                Seed = new SeedInfo()
                {
                    SeedType = "git",
                    SeedMoniker = "https://github.com/octocat/Hello-World/pull/1347"
                }
            };
        }

        private EnvironmentRegistration GetEnvForGitConfig()
        {
            return new EnvironmentRegistration()
            {
                Seed = new SeedInfo()
                {
                    GitConfig = new GitConfig()
                    {
                        UserName = "John Smith",
                        UserEmail = "JohnSmith@contoso.com"
                    }
                }
            };
        }

        private EnvironmentRegistration GetEmptyGitConfig()
        {
            return new EnvironmentRegistration() { Seed = new SeedInfo() { GitConfig = new GitConfig() { } } };
        }

        private EnvironmentRegistration GetEmptyPersonalizationConfig()
        {
            return new EnvironmentRegistration
            {
                Personalization = new Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore.PersonalizationInfo
                {

                }
            };
        }

        [TestMethod]
        public void EnvVarGitRepoUrlTest()
        {
            var envReg = GetGitEnvForGitRepo();
            var repoUrl = new EnvVarGitRepoUrl(envReg);
            var result = repoUrl.GetEnvironmentVariable();
            Assert.AreEqual(result.Key, EnvironmentVariableConstants.GitRepoUrl);
            Assert.AreEqual(result.Value, envReg.Seed.SeedMoniker);
        }

        [TestMethod]
        public void EnvVarGitRepoUrlBadUrlTest()
        {
            var envReg = new EnvironmentRegistration()
            {
                Seed = new SeedInfo()
                {
                    SeedType = "git",
                    SeedMoniker = "badurl/microsoft/vscode"
                }
            };

            var repoUrl = new EnvVarGitRepoUrl(envReg);
            Assert.IsNull(repoUrl.GetEnvironmentVariable());
        }

        [TestMethod]
        public void EnvVarGitRepoUrlNotGitTest()
        {
            var envReg = new EnvironmentRegistration()
            {
                Seed = new SeedInfo()
                {
                    SeedType = "svn",
                    SeedMoniker = "someSVN"
                }
            };

            var repoUrl = new EnvVarGitRepoUrl(envReg);
            Assert.IsNull(repoUrl.GetEnvironmentVariable());
        }

        [TestMethod]
        public void EnvVarGitRepoUrlPullTest()
        {
            var envReg = GetGitEnvForGitPull();

            var gitPRNumber = new EnvVarGitPullPRNumber(envReg);
            var prNumber = gitPRNumber.GetEnvironmentVariable();
            Assert.AreEqual(prNumber.Key, EnvironmentVariableConstants.GitPRNumber);
            Assert.AreEqual(prNumber.Value, "1347");

            var repoUrl = new EnvVarGitRepoUrl(envReg);
            var gitRepoUrl = repoUrl.GetEnvironmentVariable();
            Assert.AreEqual(gitRepoUrl.Key, EnvironmentVariableConstants.GitRepoUrl);
            Assert.AreEqual(gitRepoUrl.Value, "https://github.com/octocat/Hello-World");
        }



        [TestMethod]
        public void EnvVarGitConfigUserNameTest()
        {
            var envReg = GetEnvForGitConfig();
            var gitConfigUserName = new EnvVarGitConfigUserName(envReg);
            var result = gitConfigUserName.GetEnvironmentVariable();
            Assert.AreEqual(result.Key, EnvironmentVariableConstants.GitConfigUsername);
            Assert.AreEqual(result.Value, envReg.Seed.GitConfig.UserName);
        }

        [TestMethod]
        public void EnvVarGitConfigUserEmailTest()
        {
            var envReg = GetEnvForGitConfig();
            var gitConfigUserEmail = new EnvVarGitConfigUserEmail(envReg);
            var result = gitConfigUserEmail.GetEnvironmentVariable();
            Assert.AreEqual(result.Key, EnvironmentVariableConstants.GitConfigUserEmail);
            Assert.AreEqual(result.Value, envReg.Seed.GitConfig.UserEmail);
        }

        [TestMethod]
        public void EnvVarGitConfigUserNameEmptyTest()
        {
            var envReg = GetEmptyGitConfig();
            var gitConfigUserName = new EnvVarGitConfigUserName(envReg);
            Assert.IsNull(gitConfigUserName.GetEnvironmentVariable());
        }

        [TestMethod]
        public void EnvVarGitConfigUserEmailEmptyTest()
        {
            var envReg = GetEmptyGitConfig();
            var gitConfigUserEmail = new EnvVarGitConfigUserEmail(envReg);
            Assert.IsNull(gitConfigUserEmail.GetEnvironmentVariable());
        }

        [TestMethod]
        public void EnvVarSessionCallbackTest()
        {
            var envReg = new EnvironmentRegistration()
            {
                Id = "someId"
            };

            var appSettings = new AppSettings()
            {
                PreferredSchema = "https",
                DefaultHost = "myhost/",
                DefaultPath = "myPath"
            };

            var sessionCallback = new EnvVarSessionCallback(envReg, appSettings);
            var result = sessionCallback.GetEnvironmentVariable();
            Assert.AreEqual(result.Key, EnvironmentVariableConstants.SessionCallback);
            Assert.AreEqual(result.Value, $"https://myhost/myPath/registration/{envReg.Id}/_callback");
        }

        [TestMethod]
        public void EnvVarSessionTokenTest()
        {
            var accessToken = Guid.NewGuid().ToString();
            var sessionToken = new EnvVarSessionToken(accessToken);
            var result = sessionToken.GetEnvironmentVariable();
            Assert.AreEqual(result.Key, EnvironmentVariableConstants.SessionToken);
            Assert.AreEqual(result.Value, accessToken);
        }

        [TestMethod]
        public void EnvVarSessionIdTest()
        {
            var sessionId = Guid.NewGuid().ToString();
            var target = new EnvVarSessionId(sessionId);
            var result = target.GetEnvironmentVariable();
            Assert.AreEqual(result.Key, EnvironmentVariableConstants.SessionId);
            Assert.AreEqual(sessionId, result.Value);
        }

        [TestMethod]
        public void EnvDotfilesRepoUrlEmptyTest()
        {
            var registration = GetEmptyPersonalizationConfig();

            var validator = new EnvDotfilesRepoUrl(registration);
            var envVar = validator.GetEnvironmentVariable();

            Assert.IsNull(envVar);
        }

        [TestMethod]
        public void EnvDotfilesRepoUrlTest()
        {
            var registration = GetEmptyPersonalizationConfig();
            var repository = "https://github.com/microsoft/vscode.git";
            registration.Personalization.DotfilesRepository = repository;

            var validator = new EnvDotfilesRepoUrl(registration);
            var envVar = validator.GetEnvironmentVariable();

            Assert.AreEqual(envVar.Key, EnvironmentVariableConstants.DotfilesRepository);
            Assert.AreEqual(envVar.Value, repository);
        }

        [TestMethod]
        public void EnvDotfilesRepoUrlInvalidRepositoryTest()
        {
            var registration = GetEmptyPersonalizationConfig();
            var repository = "badurl/microsoft/vscode";
            registration.Personalization.DotfilesRepository = repository;

            var validator = new EnvDotfilesRepoUrl(registration);
            var envVar = validator.GetEnvironmentVariable();

            Assert.IsNull(envVar);
        }

        [TestMethod]
        public void EnvDotfilesTargetPathEmptyTest()
        {
            var registration = GetEmptyPersonalizationConfig();

            var validator = new EnvDotfilesTargetPath(registration);
            var envVar = validator.GetEnvironmentVariable();

            Assert.IsNull(envVar);
        }

        [TestMethod]
        public void EnvDotfilesTargetPathTest()
        {
            var registration = GetEmptyPersonalizationConfig();
            var path = "~/dotfiles";
            registration.Personalization.DotfilesTargetPath = path;

            var validator = new EnvDotfilesTargetPath(registration);
            var envVar = validator.GetEnvironmentVariable();

            Assert.AreEqual(envVar.Key, EnvironmentVariableConstants.DotfilesTargetPath);
            Assert.AreEqual(envVar.Value, path);
        }

        [TestMethod]
        public void EnvDotfilesInstallCommandEmptyTest()
        {
            var registration = GetEmptyPersonalizationConfig();

            var validator = new EnvDotfilesInstallCommand(registration);
            var envVar = validator.GetEnvironmentVariable();

            Assert.IsNull(envVar);
        }

        [TestMethod]
        public void EnvDotfilesInstallCommandTest()
        {
            var registration = GetEmptyPersonalizationConfig();
            var script = "echo Hello!";
            registration.Personalization.DotfilesInstallCommand = script;

            var validator = new EnvDotfilesInstallCommand(registration);
            var envVar = validator.GetEnvironmentVariable();

            Assert.AreEqual(envVar.Key, EnvironmentVariableConstants.DotfilesInstallCommand);
            Assert.AreEqual(envVar.Value, script);
        }

        [TestMethod]
        public void EnvDefaultShellEmptyTest()
        {
            var registration = GetEmptyPersonalizationConfig();

            var validator = new EnvDefaultShell(registration);
            var envVar = validator.GetEnvironmentVariable();

            Assert.IsNull(envVar);
        }

        [TestMethod]
        public void EnvDefaultShellInvalidShellTest()
        {
            var registration = GetEmptyPersonalizationConfig();
            var shell = "/bin/sh";
            registration.Personalization.DotfilesInstallCommand = shell;

            var validator = new EnvDefaultShell(registration);
            var envVar = validator.GetEnvironmentVariable();

            Assert.IsNull(envVar);
        }

        [TestMethod]
        public void EnvDefaultShellTest()
        {
            var registration = GetEmptyPersonalizationConfig();
            var shell = "/usr/bin/zsh";
            registration.Personalization.DefaultShell = shell;

            var validator = new EnvDefaultShell(registration);
            var envVar = validator.GetEnvironmentVariable();

            Assert.AreEqual(envVar.Key, EnvironmentVariableConstants.DefaultShell);
            Assert.AreEqual(envVar.Value, shell);
        }
    }
}
