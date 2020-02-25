// <copyright file="DataAgent.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.PrivacyServices.CommandFeed.Client;
using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Common.Identity;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.PCFAgent;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PcfAgent
{
    /// <summary>
    /// Privacy Data Agent Implementation for VSO.
    /// </summary>
    [LoggingBaseName("pcf_data_agent")]
    public class DataAgent : IPrivacyDataAgent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataAgent"/> class.
        /// </summary>
        /// <param name="privacyDataManager">The privacy data manager.</param>
        /// <param name="loggerFactory">The IDiagnosticsLogger.</param>
        /// <param name="defaultLogValues">Default Log Values.</param>
        public DataAgent(IPrivacyDataManager privacyDataManager, IDiagnosticsLoggerFactory loggerFactory, LogValueSet defaultLogValues)
        {
            PrivacyDataManager = privacyDataManager;
            LoggerFactory = loggerFactory;
            DefaultLogValues = defaultLogValues;
        }

        private IPrivacyDataManager PrivacyDataManager { get; }

        private IDiagnosticsLoggerFactory LoggerFactory { get; }

        private LogValueSet DefaultLogValues { get; }

        /// <inheritdoc />
        public async Task ProcessAccountClosedAsync(IAccountCloseCommand command)
        {
            var logger = GetNewLogger();
            logger.LogInfo(GetType().FormatLogMessage(nameof(ProcessAccountClosedAsync)));
            await PerformDeleteAsync(command, logger);
        }

        /// <inheritdoc/>
        public async Task ProcessAgeOutAsync(IAgeOutCommand command)
        {
            var logger = GetNewLogger();
            logger.LogInfo(GetType().FormatLogMessage(nameof(ProcessAgeOutAsync)));
            await PerformDeleteAsync(command, logger);
        }

        /// <inheritdoc/>
        public async Task ProcessDeleteAsync(IDeleteCommand command)
        {
            var logger = GetNewLogger();
            logger.LogInfo(GetType().FormatLogMessage(nameof(ProcessDeleteAsync)));
            await PerformDeleteAsync(command, logger);
        }

        /// <inheritdoc/>
        public Task ProcessExportAsync(IExportCommand command)
        {
            var logger = GetNewLogger();
            logger.LogInfo(GetType().FormatLogMessage(nameof(ProcessExportAsync)));
            return Task.CompletedTask;
        }

        private async Task PerformDeleteAsync(IPrivacyCommand command, IDiagnosticsLogger logger)
        {
            var userIdSet = await GetUserIdSetAsync(command, logger.NewChildLogger());
            if (userIdSet != default)
            {
                var affectedRowCount = await Retry.DoAsync(async attempt =>
                {
                    return await PrivacyDataManager.PerformDeleteAsync(userIdSet, logger);
                });
                await command.CheckpointAsync(CommandStatus.Complete, affectedRowCount);
            }
        }

        private async Task<UserIdSet> GetUserIdSetAsync(IPrivacyCommand command, IDiagnosticsLogger logger)
        {
            var userIdSet = ConstructUserIdSet(command.Subject);
            if (userIdSet == default)
            {
                logger.LogError(GetType().FormatLogErrorMessage("invalid_subject"));
                await command.CheckpointAsync(CommandStatus.UnexpectedCommand, affectedRowCount: 0);
            }

            return userIdSet;
        }

        private UserIdSet ConstructUserIdSet(IPrivacySubject subject)
        {
            string tenantId;
            string objectId;
            var userIdSet = default(UserIdSet);

            switch (subject)
            {
                case MsaSubject msa:
                    objectId = ConvertToGuidFormat(msa.HexCid);
                    tenantId = AuthenticationConstants.MsaPseudoTenantId;
                    var altSecId = $"1:live.com:{msa.HexPuid}";

                    var legacyId = IdentityUtility.MakeLegacyUserId(tenantId, objectId, altSecId);
                    var canonicalId = IdentityUtility.MakeCanonicalUserId(tenantId, objectId, msa.Puid.ToString(), altSecId);

                    userIdSet = new UserIdSet(canonicalId, legacyId, legacyId);
                    break;
                case AadSubject aad:
                    tenantId = aad.TenantId.ToString("D");
                    objectId = aad.ObjectId.ToString("D");

                    var aadCanonical = IdentityUtility.MakeCanonicalUserId(tenantId, objectId, aad.OrgIdPUID.ToString(), null);
                    var aadLegacy = IdentityUtility.MakeLegacyUserId(tenantId, objectId, null);
                    userIdSet = new UserIdSet(aadCanonical, aadLegacy, aadLegacy);
                    break;
                default:
                    return userIdSet;
            }

            return userIdSet;
        }

        private string ConstructUserIdFromTenantIdAndObjectId(string tenantId, string objectId)
        {
            return $"{tenantId}_{objectId}";
        }

        private string ConvertToGuidFormat(string id)
        {
            var paddedGraphId = id.PadLeft(32, '0');
            var guid = Guid.Parse(paddedGraphId);
            return guid.ToString("D");
        }

        private IDiagnosticsLogger GetNewLogger()
        {
            return LoggerFactory.New(DefaultLogValues);
        }
    }
}
