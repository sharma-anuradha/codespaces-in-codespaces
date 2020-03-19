// <copyright file="DataAgent.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PrivacyServices.CommandFeed.Client;
using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Common.Identity;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.PCFAgent;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PcfAgent
{
    /// <summary>
    /// Privacy Data Agent Implementation for VSO.
    /// </summary>
    [LoggingBaseName("pcf_data_agent")]
    public class DataAgent : IPrivacyDataAgent
    {
        private const string ExportFileName = "ProductAndServiceUsage_VisualStudioOnline.json";

        /// <summary>
        /// Initializes a new instance of the <see cref="DataAgent"/> class.
        /// </summary>
        /// <param name="privacyDataManager">The privacy data manager.</param>
        /// <param name="loggerFactory">The IDiagnosticsLogger.</param>
        /// <param name="defaultLogValues">Default Log Values.</param>
        /// <param name="commandFeedLogger">The command feed logger.</param>
        public DataAgent(IPrivacyDataManager privacyDataManager, IDiagnosticsLoggerFactory loggerFactory, LogValueSet defaultLogValues, CommandFeedLogger commandFeedLogger)
        {
            PrivacyDataManager = privacyDataManager;
            LoggerFactory = loggerFactory;
            DefaultLogValues = defaultLogValues;
            CommandFeedLogger = commandFeedLogger;
        }

        private IPrivacyDataManager PrivacyDataManager { get; }

        private IDiagnosticsLoggerFactory LoggerFactory { get; }

        private LogValueSet DefaultLogValues { get; }

        private CommandFeedLogger CommandFeedLogger { get; }

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

            // Only AgeOuts and AccountCloseOuts results in deletion. Manual delete commands are not supported.
            await command.CheckpointAsync(CommandStatus.Complete, 0);
        }

        /// <inheritdoc/>
        public async Task ProcessExportAsync(IExportCommand command)
        {
            var logger = GetNewLogger();
            logger.LogInfo(GetType().FormatLogMessage(nameof(ProcessExportAsync)));

            await logger.OperationScopeAsync("pcf_perform_export", async (childLogger) =>
            {
                var totalAffectedEntitiesCount = 0;
                var exportDetails = new List<ExportedFileSizeDetails>();

                try
                {
                    using (var pipeline = ExportPipelineFactory.CreateAzureExportPipeline(
                            CommandFeedLogger,
                            command.AzureBlobContainerTargetUri,
                            command.AzureBlobContainerPath))
                    {
                        var jsonExport = new JArray();
                        var userIdSets = await GetUserIdSetsAsync(command, childLogger);

                        foreach (var userIdSet in userIdSets)
                        {
                            var (affectedEntitiesCount, userData) = await PrivacyDataManager.PerformExportAsync(userIdSet, childLogger);
                            totalAffectedEntitiesCount += affectedEntitiesCount;
                            jsonExport.Add(userData);
                        }

                        childLogger.AddValue("PcfAffectedEntitiesCount", totalAffectedEntitiesCount.ToString());

                        var details = await Retry.DoAsync(async attempt =>
                        {
                            return await pipeline.ExportAsync(ExportProductId.DevelopmentVisualStudio, ExportFileName, jsonExport.ToString());
                        });

                        if (details == null)
                        {
                            throw new InvalidOperationException("Failed to upload the export.");
                        }

                        exportDetails.Add(details);
                        await command.CheckpointAsync(CommandStatus.Complete, totalAffectedEntitiesCount, exportedFileSizeDetails: exportDetails);
                    }
                }
                catch (Exception e)
                {
                    await command.CheckpointAsync(CommandStatus.Failed, totalAffectedEntitiesCount, exportedFileSizeDetails: exportDetails);
                    throw e;
                }
            });
        }

        private async Task PerformDeleteAsync(IPrivacyCommand command, IDiagnosticsLogger logger)
        {
            var affectedRowCount = 0;
            var userIdSets = await GetUserIdSetsAsync(command, logger.NewChildLogger());
            foreach (var userIdSet in userIdSets)
            {
                affectedRowCount += await Retry.DoAsync(async attempt =>
                {
                    return await PrivacyDataManager.PerformDeleteAsync(userIdSet, logger);
                });
            }

            await command.CheckpointAsync(CommandStatus.Complete, affectedRowCount);
        }

        private async Task<IEnumerable<UserIdSet>> GetUserIdSetsAsync(IPrivacyCommand command, IDiagnosticsLogger logger)
        {
            var userIdSets = ConstructUserIdSets(command.Subject);
            if (!userIdSets.Any())
            {
                logger.LogError(GetType().FormatLogErrorMessage("invalid_subject"));
                await command.CheckpointAsync(CommandStatus.UnexpectedCommand, affectedRowCount: 0);
            }

            return userIdSets;
        }

        private IEnumerable<UserIdSet> ConstructUserIdSets(IPrivacySubject subject)
        {
            string tenantId;
            string objectId;
            var userIdSets = new List<UserIdSet>();

            switch (subject)
            {
                case MsaSubject msa:
                    objectId = ConvertToGuidFormat(msa.HexCid);
                    tenantId = AuthenticationConstants.MsaPseudoTenantId;
                    var altSecId = $"1:live.com:{msa.HexPuid}";

                    // Create standard MSA UserIdSet.
                    var legacyId = IdentityUtility.MakeLegacyUserId(tenantId, objectId, altSecId);
                    var canonicalId = IdentityUtility.MakeCanonicalUserId(tenantId, objectId, msa.Puid.ToString(), altSecId);
                    userIdSets.Add(new UserIdSet(canonicalId, legacyId, legacyId));

                    // Create Passthrough UserIdSet.
                    tenantId = AuthenticationConstants.MicrosoftFirstPartyAppTenantId;
                    legacyId = IdentityUtility.MakeLegacyUserId(tenantId, null, altSecId);
                    userIdSets.Add(new UserIdSet(null, legacyId, legacyId));
                    break;
                case AadSubject aad:
                    tenantId = aad.TenantId.ToString("D");
                    objectId = aad.ObjectId.ToString("D");

                    var aadCanonical = IdentityUtility.MakeCanonicalUserId(tenantId, objectId, aad.OrgIdPUID.ToString(), null);
                    var aadLegacy = IdentityUtility.MakeLegacyUserId(tenantId, objectId, null);
                    userIdSets.Add(new UserIdSet(aadCanonical, aadLegacy, aadLegacy));
                    break;
            }

            return userIdSets;
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
