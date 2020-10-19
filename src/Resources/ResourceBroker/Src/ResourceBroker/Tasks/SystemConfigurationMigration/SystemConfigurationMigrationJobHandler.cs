// <copyright file="SystemConfigurationMigrationJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Linq;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks.SystemConfigurationMigration
{
    public class SystemConfigurationMigrationJobHandler : JobHandlerPayloadBase<SystemConfigurationMigrationJobProducer.SystemConfigurationMigrationPayload>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemConfigurationMigrationJobHandler"/> class.
        /// <param name="regionalSystemConfigurationMigrationRepository">Regional system configuration migration repository.</param>
        /// <param name="globalSystemConfigurationRepository">Global system configuration repository.</param>
        /// </summary>
        public SystemConfigurationMigrationJobHandler(
            IRegionalSystemConfigurationMigrationRepository regionalSystemConfigurationMigrationRepository,
            IGlobalSystemConfigurationRepository globalSystemConfigurationRepository)
        {
            RegionalRepository = regionalSystemConfigurationMigrationRepository;
            GlobalRepository = globalSystemConfigurationRepository;
        }

        private IRegionalSystemConfigurationMigrationRepository RegionalRepository { get; }

        private IGlobalSystemConfigurationRepository GlobalRepository { get; }

        private string LogBaseName => ResourceLoggingConstants.SystemConfigurationMigrationJobHandler;

        /// <inheritdoc/>
        protected override Task HandleJobAsync(SystemConfigurationMigrationJobProducer.SystemConfigurationMigrationPayload payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                async (childLogger) =>
                {
                    // get all records
                    var allRegionalRecords = await GetAllRegionalRecordsAsync(childLogger);

                    // migrate the records to global db
                    var recordsToMigrate = GetRecordsToMigrate(allRegionalRecords, childLogger);
                    await MigrateRecordsToGlobalDBAsync(recordsToMigrate, childLogger);

                    // validate already migrated records
                    var migratedRecords = GetMigratedRegionalRecords(allRegionalRecords, childLogger);
                    await ValidateMigratedRecordsAsync(migratedRecords, childLogger);
                },
                swallowException: true);
        }

        private async Task MigrateRecordsToGlobalDBAsync(IEnumerable<SystemConfigurationMigrationRecord> records, IDiagnosticsLogger logger)
        {
            var conflictingRecordsCount = 0;

            foreach (var record in records)
            {
                var conflictRecord = await CoreRunUnitMigrateAsync(record, logger.NewChildLogger());
                conflictingRecordsCount += conflictRecord;
            }

            logger.FluentAddValue("SizeOfConflictingExistingRecords", conflictingRecordsCount);            
        }

        private async Task<int> CoreRunUnitMigrateAsync(SystemConfigurationMigrationRecord record, IDiagnosticsLogger logger)
        {
            var conflictingRecordsCount = 0;
            var globalRecord = await GlobalRepository.GetAsync(record.Id, logger.NewChildLogger());

            // if the record with said ID doesn't exist then add it to the global db
            if (globalRecord == default)
            {
                var recordToAdd = new SystemConfigurationRecord()
                {
                    Id = record.Id,
                    Value = record.Value,
                    Comment = record.Comment,
                };

                // add to global DB
                await GlobalRepository.CreateAsync(recordToAdd, logger.NewChildLogger());

                // update the regional record to show migrate
                record.Migrated = true;
                await RegionalRepository.UpdateAsync(record, logger.NewChildLogger());
            }
            else
            {
                // if they have same values, then just update the regional record to show migration
                if (globalRecord.Value == record.Value)
                {
                    // update the regional record to show migrated
                    record.Migrated = true;
                    await RegionalRepository.UpdateAsync(record, logger.NewChildLogger());
                }
                else
                {
                    // we have a conflict in database values
                    ++conflictingRecordsCount;

                    // log the error
                    logger.NewChildLogger()
                        .FluentAddValue("RecordKey", record.Id)
                        .FluentAddValue("ValueInGlobalDB", globalRecord.Value)
                        .FluentAddValue("ValueInRegionalDB", record.Value)
                        .LogError($"{LogBaseName}_migrate_error");                   
                }
            }

            return conflictingRecordsCount;
        }

        private async Task ValidateMigratedRecordsAsync(IEnumerable<SystemConfigurationMigrationRecord> migratedRecords, IDiagnosticsLogger logger)
        {
            var missingRecordsCount = 0;
            var conflictingRecordsCount = 0;

            foreach (var record in migratedRecords)
            {
                var localCount = await CoreRunUnitValidateAsync(record, logger.NewChildLogger());
                missingRecordsCount += localCount.missingRecordsCount;
                conflictingRecordsCount += localCount.conflictingRecordsCount;
            }

            logger.FluentAddValue("SizeOfMissingMigratedRecords", missingRecordsCount);
            logger.FluentAddValue("SizeOfConflictingMigratedRecords", conflictingRecordsCount);
        }

        private async Task<(int missingRecordsCount, int conflictingRecordsCount)> CoreRunUnitValidateAsync(SystemConfigurationMigrationRecord record, IDiagnosticsLogger logger)
        {
            var missingRecordsCount = 0;
            var conflictingRecordsCount = 0;
            var globalRecord = await GlobalRepository.GetAsync(record.Id, logger.NewChildLogger());

            // if it is not present then we have a missing record that has been marked as migrated and we should log it
            if (globalRecord == default)
            {
                ++missingRecordsCount;

                // log the error
                logger.NewChildLogger()
                    .FluentAddValue("RecordKey", record.Id)
                    .FluentAddValue("ValueInRegionalDB", record.Value)
                    .LogError($"{LogBaseName}_validate_missing_error");  
            }
            else
            {
                // if the record exists, it's an expected outcome.
                // We should then check values to make sure that they are exact same
                if (globalRecord.Value != record.Value)
                {
                    // we have a conflict in database values
                    ++conflictingRecordsCount;

                    // log the error
                    logger.NewChildLogger()
                        .FluentAddValue("RecordKey", record.Id)
                        .FluentAddValue("ValueInGlobalDB", globalRecord.Value)
                        .FluentAddValue("ValueInRegionalDB", record.Value)
                        .LogError($"{LogBaseName}_validate_conflicting_error");
                }
            }

            return (missingRecordsCount, conflictingRecordsCount);
        }

        private async Task<IEnumerable<SystemConfigurationMigrationRecord>> GetAllRegionalRecordsAsync(IDiagnosticsLogger logger)
        {
            var query = new SqlQuerySpec(@"SELECT * FROM c");
            var records = await RegionalRepository.QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<SystemConfigurationMigrationRecord>(uri, query, feedOptions).AsDocumentQuery(), logger.NewChildLogger());
            logger.FluentAddBaseValue("SizeOfConfigurationRecords", records.Count());
            return records;
        }

        private IEnumerable<SystemConfigurationMigrationRecord> GetMigratedRegionalRecords(IEnumerable<SystemConfigurationMigrationRecord> allRegionalRecords, IDiagnosticsLogger logger)
        {
            var migratedRecords = allRegionalRecords.Where(record => record.Migrated == true);
            logger.FluentAddBaseValue("SizeOfMigratedRecords", migratedRecords.Count());
            return migratedRecords;
        }

        private IEnumerable<SystemConfigurationMigrationRecord> GetRecordsToMigrate(IEnumerable<SystemConfigurationMigrationRecord> allRegionalRecords, IDiagnosticsLogger logger)
        {
            var recordsToMigrate = allRegionalRecords.Where(record => record.Migrated == false);
            logger.FluentAddBaseValue("SizeOfRecordsToMigrate", recordsToMigrate.Count());
            return recordsToMigrate;
        }
    }
}
