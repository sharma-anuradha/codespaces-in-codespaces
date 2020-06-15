// <copyright file="RandomMockCommandFeedClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#pragma warning disable

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PCFAgent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.CommandFeed.Client.Commands;

    /// <summary>
    /// Contains an implementation of ICommandFeed client that returns random dummy data.
    /// This code is copied from PCF Samples.
    /// </summary>
    public sealed class RandomMockCommandFeedClient : ICommandFeedClient
    {
        /// <summary>
        /// Indicates if the mock client should return export commands.
        /// </summary>
        public bool SimulateExportCommands { get; set; } = true;

        /// <summary>
        /// Indicates if the mock client should return delete commands.
        /// </summary>
        public bool SimulateDeleteCommands { get; set; } = true;

        /// <summary>
        /// Indicates if the mock client should return account closed commands.
        /// </summary>
        public bool SimulateAccountClosedCommands { get; set; } = true;

        /// <summary>
        /// Indicates if the mock client should return age out commands.
        /// </summary>
        public bool SimulateAgeOutCommands { get; set; } = true;

        /// <summary>
        /// Indicates if the mock client should return MSA subjects.
        /// </summary>
        public bool SimulateMsaSubject { get; set; } = true;

        /// <summary>
        /// Indicates if the mock client should return AAD subjects.
        /// </summary>
        public bool SimulateAadSubject { get; set; } = true;

        /// <summary>
        /// Indicates if the mock client should return Device subjects.
        /// </summary>
        public bool SimulateDeviceSubject { get; set; } = true;

        /// <summary>
        /// Indicates if the mock client should return Alternate Demographic subjects.
        /// Demographic subjects should not be confounded with Demographic data types.
        /// </summary>
        public bool SimulateAlternateDemographicSubject { get; set; } = true;

        /// <summary>
        /// Indicates if the mock client should return Alternate Microsoft employee subjects.
        /// If the Alternate MicrosoftEmployee property is set, then all other Alternate subject properties should be empty.
        /// </summary>
        public bool SimulateAlternateMicrosoftEmployeeSubject { get; set; } = true;

        public List<KeyDiscoveryConfiguration> SovereignCloudConfigurations
        {
            get => null;
            set { }
        }

        /// <inheritdoc />
        public TimeSpan? RequestedLeaseDuration { get; set; }

        private List<IPrivacyCommand> commandList;

        /// <summary>
        /// Gets the next batch of random commands.
        /// </summary>
        public async Task<List<IPrivacyCommand>> GetCommandsAsync(CancellationToken cancellationToken)
        {
            Random random = new Random();

            // Simulate HTTP long-poll.
            await Task.Delay(random.Next(2, 25) * 1000, cancellationToken).ConfigureAwait(false);
            this.commandList = new List<IPrivacyCommand>();

            // 20 commands at most sounds reasonable for now. This is a functionality test, not a stress test.
            int commandsToCreate = random.Next(1, 20);
            for (int i = 0; i < commandsToCreate; ++i)
            {
                int next = random.Next() % 4;

                if (this.SimulateDeleteCommands && next == 0)
                {
                    this.commandList.Add(this.CreateDeleteCommand(random));
                }
                else if (this.SimulateExportCommands && next == 1)
                {
                    this.commandList.Add(this.CreateExportCommand(random));
                }
                else if (this.SimulateAccountClosedCommands && !this.SimulateAlternateDemographicSubject && next == 2)
                {
                    this.commandList.Add(this.CreateAccountCloseCommand(random));
                }
                else if (this.SimulateAgeOutCommands)
                {
                    this.commandList.Add(this.CreateAgeOutCommand(random));
                }
            }

            return this.commandList;
        }

        private IDeleteCommand CreateDeleteCommand(Random random)
        {
            DataTypeId privacyDataType = TakeElement(random, Policies.Current.DataTypes.Set.ToList()).Id;
            Type subjectType = this.GetSubjectType(random, typeof(ExportCommand));

            return SampleCommandFactory.GetDeleteCustomerContentSampleCommand(
                assetGroupQualifier: null,
                subjectType: subjectType,
                client: this,
                predicateTypeId: privacyDataType,
                random: random,
                createMicrosoftEmployeeAlternateSubject: this.SimulateAlternateMicrosoftEmployeeSubject
            );
        }

        private IExportCommand CreateExportCommand(Random random)
        {
            Type subjectType = this.GetSubjectType(random, typeof(ExportCommand));

            return SampleCommandFactory.GetExportSampleCommand(
                assetGroupQualifier: Guid.NewGuid().ToString(),
                client: this,
                subjectType: subjectType,
                dataTypeIds: Policies.Current.DataTypes.Set.Select(x => x.Id).ToList(),
                azureBlobUri: new Uri("https://" + Guid.NewGuid().ToString("n")),
                cloudInstance: Policies.Current.CloudInstances.Ids.Public.Value,
                random: random,
                createMicrosoftEmployeeAlternateSubject: this.SimulateAlternateMicrosoftEmployeeSubject);
        }

        private IAccountCloseCommand CreateAccountCloseCommand(Random random)
        {
            Type subjectType = this.GetSubjectType(random, typeof(AccountCloseCommand));
            return SampleCommandFactory.GetAccountCloseSampleCommand(
                assetGroupQualifier: Guid.NewGuid().ToString(),
                client: this,
                subjectType: subjectType,
                cloudInstance: Policies.Current.CloudInstances.Ids.Public.Value,
                random: random);
        }

        private IAgeOutCommand CreateAgeOutCommand(Random random)
        {
            return SampleCommandFactory.GetMsaAgeOutSampleCommand(
                assetGroupQualifier: Guid.NewGuid().ToString(),
                client: this,
                lastActive: DateTimeOffset.UtcNow.AddYears(-6),
                random: random);
        }

        private Type GetSubjectType(Random random, Type commandType)
        {
            // Supported subjects
            List<Type> supportedSubjects = new List<Type>();
            if (this.SimulateAadSubject)
            {
                supportedSubjects.Add(typeof(AadSubject));
            }

            if (this.SimulateMsaSubject)
            {
                supportedSubjects.Add(typeof(MsaSubject));
            }

            if (this.SimulateDeviceSubject)
            {
                supportedSubjects.Add(typeof(DeviceSubject));
            }

            if ((this.SimulateAlternateDemographicSubject || this.SimulateAlternateMicrosoftEmployeeSubject) && commandType != typeof(AccountCloseCommand))
            {
                supportedSubjects.Add(typeof(DemographicSubject));
            }

            // pick a subject
            Type subjectType = TakeElement(random, supportedSubjects);

            return subjectType;
        }

        private static T TakeElement<T>(Random r, IList<T> items)
        {
            return items[r.Next() % items.Count];
        }

        /// <summary>
        /// Stub for Checkpoint.
        /// </summary>
        public Task<string> CheckpointAsync(string commandId, string agentState, CommandStatus commandStatus, int affectedRowCount, string leaseReceipt, TimeSpan? leaseExtension = null, IEnumerable<string> variantIds = null, IEnumerable<string> nonTransientFailures = null, IEnumerable<ExportedFileSizeDetails> exportedFileSizeDetails = null)
        {
            return Task.FromResult("new lease receipt");
        }

        public Task<IPrivacyCommand> QueryCommandAsync(string leaseReceipt, CancellationToken cancellationToken)
        {
            if (this.commandList != null && this.commandList.Any(command => command.LeaseReceipt == leaseReceipt))
            {
                return Task.FromResult(this.commandList.First(command => command.LeaseReceipt == leaseReceipt));
            }

            return Task.FromResult<IPrivacyCommand>(null);
        }

        public Task BatchCheckpointCompleteAsync(IEnumerable<ProcessedCommand> processedCommands)
        {
            throw new NotImplementedException();
        }

        public Task<List<Microsoft.PrivacyServices.CommandFeed.Client.QueueStats>> GetQueueStatsAsync(string assetGroupQualifier = null, string commandType = null)
        {
            throw new NotImplementedException();
        }

        public Task ReplayCommandsByIdAsync(IEnumerable<string> commandIds, IEnumerable<string> assetGroupQualifiers = null)
        {
            throw new NotImplementedException();
        }

        public Task ReplayCommandsByDatesAsync(DateTimeOffset replayFromDate, DateTimeOffset replayToDate, IEnumerable<string> assetGroupQualifiers = null)
        {
            throw new NotImplementedException();
        }
    }
}

