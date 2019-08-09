using Microsoft.VsCloudKernel.Services.EnvReg.Models;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using System;
using System.Collections.Generic;
using VsClk.EnvReg.Repositories;
using Xunit;

namespace VsClk.EnvReg.WebApi.Tests
{
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

        [Fact]
        public void EnvVarGitRepoUrlTest()
        {
            var envReg = GetGitEnvForGitRepo();
            var repoUrl = new EnvVarGitRepoUrl(envReg);
            var result = repoUrl.GetEnvironmentVariable();
            Assert.Equal(result.Key, EnvironmentVariableConstants.GitRepoUrl);
            Assert.Equal(result.Value, envReg.Seed.SeedMoniker);
        }

        [Fact]
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
            Assert.Null(repoUrl.GetEnvironmentVariable());
        }

        [Fact]
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
            Assert.Null(repoUrl.GetEnvironmentVariable());
        }

        [Fact]
        public void EnvVarGitRepoUrlPullTest()
        {
            var envReg = GetGitEnvForGitPull();

            var gitPRNumber = new EnvVarGitPullPRNumber(envReg);
            var prNumber = gitPRNumber.GetEnvironmentVariable();
            Assert.Equal(prNumber.Key, EnvironmentVariableConstants.GitPRNumber);
            Assert.Equal("1347", prNumber.Value);

            var repoUrl = new EnvVarGitRepoUrl(envReg);
            var gitRepoUrl = repoUrl.GetEnvironmentVariable();
            Assert.Equal(gitRepoUrl.Key, EnvironmentVariableConstants.GitRepoUrl);
            Assert.Equal("https://github.com/octocat/Hello-World", gitRepoUrl.Value);
        }



        [Fact]
        public void EnvVarGitConfigUserNameTest()
        {
            var envReg = GetEnvForGitConfig();
            var gitConfigUserName = new EnvVarGitConfigUserName(envReg);
            var result = gitConfigUserName.GetEnvironmentVariable();
            Assert.Equal(result.Key, EnvironmentVariableConstants.GitConfigUsername);
            Assert.Equal(result.Value, envReg.Seed.GitConfig.UserName);
        }

        [Fact]
        public void EnvVarGitConfigUserEmailTest()
        {
            var envReg = GetEnvForGitConfig();
            var gitConfigUserEmail = new EnvVarGitConfigUserEmail(envReg);
            var result = gitConfigUserEmail.GetEnvironmentVariable();
            Assert.Equal(result.Key, EnvironmentVariableConstants.GitConfigUserEmail);
            Assert.Equal(result.Value, envReg.Seed.GitConfig.UserEmail);
        }

        [Fact]
        public void EnvVarGitConfigUserNameEmptyTest()
        {
            var envReg = GetEmptyGitConfig();
            var gitConfigUserName = new EnvVarGitConfigUserName(envReg);
            Assert.Null(gitConfigUserName.GetEnvironmentVariable());
        }

        [Fact]
        public void EnvVarGitConfigUserEmailEmptyTest()
        {
            var envReg = GetEmptyGitConfig();
            var gitConfigUserEmail = new EnvVarGitConfigUserEmail(envReg);
            Assert.Null(gitConfigUserEmail.GetEnvironmentVariable());
        }

        [Fact]
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
            Assert.Equal(result.Key, EnvironmentVariableConstants.SessionCallback);
            Assert.Equal(result.Value, $"https://myhost/myPath/registration/{envReg.Id}/_callback");
        }

        [Fact]
        public void EnvVarSessionTokenTest()
        {
            var accessToken = Guid.NewGuid().ToString();
            var sessionToken = new EnvVarSessionToken(accessToken);
            var result = sessionToken.GetEnvironmentVariable();
            Assert.Equal(result.Key, EnvironmentVariableConstants.SessionToken);
            Assert.Equal(result.Value, accessToken);
        }

        [Fact]
        public void EnvVarSessionIdTest()
        {
            var sessionId = Guid.NewGuid().ToString();
            var target = new EnvVarSessionId(sessionId);
            var result = target.GetEnvironmentVariable();
            Assert.Equal(result.Key, EnvironmentVariableConstants.SessionId);
            Assert.Equal(sessionId, result.Value);
        }

        [Fact]
        public void EnvDotfilesRepoUrlEmptyTest()
        {
            var registration = GetEmptyPersonalizationConfig();

            var validator = new EnvDotfilesRepoUrl(registration);
            var envVar = validator.GetEnvironmentVariable();

            Assert.Null(envVar);
        }

        [Fact]
        public void EnvDotfilesRepoUrlTest()
        {
            var registration = GetEmptyPersonalizationConfig();
            var repository = "https://github.com/microsoft/vscode.git";
            registration.Personalization.DotfilesRepository = repository;

            var validator = new EnvDotfilesRepoUrl(registration);
            var envVar = validator.GetEnvironmentVariable();

            Assert.Equal(envVar.Key, EnvironmentVariableConstants.DotfilesRepository);
            Assert.Equal(envVar.Value, repository);
        }

        [Fact]
        public void EnvDotfilesRepoUrlInvalidRepositoryTest()
        {
            var registration = GetEmptyPersonalizationConfig();
            var repository = "badurl/microsoft/vscode";
            registration.Personalization.DotfilesRepository = repository;

            var validator = new EnvDotfilesRepoUrl(registration);
            var envVar = validator.GetEnvironmentVariable();

            Assert.Null(envVar);
        }

        [Fact]
        public void EnvDotfilesTargetPathEmptyTest()
        {
            var registration = GetEmptyPersonalizationConfig();

            var validator = new EnvDotfilesTargetPath(registration);
            var envVar = validator.GetEnvironmentVariable();

            Assert.Null(envVar);
        }

        [Fact]
        public void EnvDotfilesTargetPathTest()
        {
            var registration = GetEmptyPersonalizationConfig();
            var path = "~/dotfiles";
            registration.Personalization.DotfilesTargetPath = path;

            var validator = new EnvDotfilesTargetPath(registration);
            var envVar = validator.GetEnvironmentVariable();

            Assert.Equal(envVar.Key, EnvironmentVariableConstants.DotfilesTargetPath);
            Assert.Equal(envVar.Value, path);
        }

        [Fact]
        public void EnvDotfilesInstallCommandEmptyTest()
        {
            var registration = GetEmptyPersonalizationConfig();

            var validator = new EnvDotfilesInstallCommand(registration);
            var envVar = validator.GetEnvironmentVariable();

            Assert.Null(envVar);
        }

        [Fact]
        public void EnvDotfilesInstallCommandTest()
        {
            var registration = GetEmptyPersonalizationConfig();
            var script = "echo Hello!";
            registration.Personalization.DotfilesInstallCommand = script;

            var validator = new EnvDotfilesInstallCommand(registration);
            var envVar = validator.GetEnvironmentVariable();

            Assert.Equal(envVar.Key, EnvironmentVariableConstants.DotfilesInstallCommand);
            Assert.Equal(envVar.Value, script);
        }

        [Fact]
        public void EnvDefaultShellEmptyTest()
        {
            var registration = GetEmptyPersonalizationConfig();

            var validator = new EnvDefaultShell(registration);
            var envVar = validator.GetEnvironmentVariable();

            Assert.Null(envVar);
        }

        [Theory]
        [InlineData(true, new string[] { "/bin/sh", "hello" })]
        [InlineData(true, new string[] { "sh" })]
        [InlineData(true, null)]
        [InlineData(true, new string[] { })]
        [InlineData(true, new string[] { "", "hello" })]
        [InlineData(false, null)]
        [InlineData(false, new string[] { })]
        [InlineData(true, new string[] { "" })]
        public void EnvDefaultShellInvalidShellTest(bool isKitchenSinkImage, string[] shells)
        {
            var registration = GetEmptyPersonalizationConfig();
            registration.ContainerImage = isKitchenSinkImage ? "kitchensink" : null;
            registration.Personalization.PreferredShells = shells;

            var validator = new EnvDefaultShell(registration);
            var envVar = validator.GetEnvironmentVariable();

            Assert.Null(envVar);
        }

        [Theory]
        [MemberData(nameof(DefaultShell_Valid_MemberData))]
        public void EnvDefaultShellTest(bool isKitchenSinkImage, string[] shells, string path)
        {
            var registration = GetEmptyPersonalizationConfig();
            registration.ContainerImage = isKitchenSinkImage ? "kitchensink" : null;
            registration.Personalization.PreferredShells = shells;

            var validator = new EnvDefaultShell(registration);
            var envVar = validator.GetEnvironmentVariable();

            Assert.Equal(envVar.Key, isKitchenSinkImage ?
                EnvironmentVariableConstants.KitchenSinkDefaultShell :
                EnvironmentVariableConstants.CustomImageDefaultShell);
            Assert.Equal(envVar.Value, path);
        }

        public static IEnumerable<object[]> DefaultShell_Valid_MemberData()
        {
            yield return new object[] { false, new string[] { "cmd.exe" }, "cmd.exe" };
            yield return new object[] { false, new string[] { "powershell.exe" }, "powershell.exe" };
            yield return new object[] { false, new string[] { "cmd.exe", "powershell.exe", "hello", "/bin/sh" }, "cmd.exe,powershell.exe,hello,/bin/sh" };
            yield return new object[] { false, new string[] { "", "hello" }, "hello" };
            yield return new object[] { false, new string[] { "", "bash", "", "hello", null }, "bash,hello" };
            yield return new object[] { true, new string[] { "zsh" }, "/usr/bin/zsh" };
            yield return new object[] { true, new string[] { "bash" }, "/bin/bash" };
            yield return new object[] { true, new string[] { "fish" }, "/usr/bin/fish" };
            yield return new object[] { true, new string[] { null, "fish", "bash", "zsh" }, "/usr/bin/fish" };
            yield return new object[] { true, new string[] { "", "sh", "/bin/bash", "zsh" }, "/usr/bin/zsh" };
        }
    }
}
