// <copyright file="KustoStreamWriter.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Kusto.Cloud.Platform.Utils;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Kusto.Ingest;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Developer.DevStampLogger
{
    /// <summary>
    /// Kusto stream writer.
    /// </summary>
    public class KustoStreamWriter : TextWriter
    {
        private const string DevKustoUri = "https://vsodevkusto.westus2.kusto.windows.net:443/";

        private const string KustoIngestUri = "https://ingest-vsodevkusto.westus2.kusto.windows.net:443/";

        private const string KustoDatabaseName = "VsoDevStampEventLogs";

        private const string KustoTableBaseName = "EventLogs";

        private const string KustoTableMapPrefix = "JsonTableMap";

        private readonly object lockObject = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="KustoStreamWriter"/> class.
        /// </summary>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="controlPlaneAzureResourceAccessor">Control plane resource accessor.</param>
        public KustoStreamWriter(IResourceNameBuilder resourceNameBuilder, IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor)
        {
            TableName = resourceNameBuilder.GetDeveloperStampKustoTableName(KustoTableBaseName);
            ControlPlaneAzureResourceAccessor = controlPlaneAzureResourceAccessor;
            TableMapName = $"{KustoTableMapPrefix}-{Process.GetCurrentProcess().Id}-{DateTime.Now.Ticks}";
        }

        /// <inheritdoc/>
        public override Encoding Encoding => Encoding.Default;

        private string TableName { get; set; }

        private IControlPlaneAzureResourceAccessor ControlPlaneAzureResourceAccessor { get; set; }

        private KustoIngestionProperties IngestionProperties { get; set; }

        private string TableMapName { get; set; }

        private List<ColumnMapping> ColumnMappings { get; set; } = new List<ColumnMapping>();

        /// <inheritdoc/>
        public override void WriteLine(string text)
        {
            var task = ControlPlaneAzureResourceAccessor.GetApplicationKeyAndSecretsAsync();
            (string clientId, string key, string tenant) = task.GetAwaiter().GetResult();

            if (IngestionProperties == default)
            {
                lock (lockObject)
                {
                    Initialize(clientId, key, tenant);
                }
            }

            var (updateNeeded, payloadItemKeys) = IsColumnUpdateNeeded(text);
            if (updateNeeded)
            {
                lock (lockObject)
                {
                    UpdateColumnMapping(clientId, key, tenant, payloadItemKeys);
                }
            }

            var connectionBuilder = new KustoConnectionStringBuilder(KustoIngestUri).WithAadApplicationKeyAuthentication(clientId, key, tenant);

            using (var kustoIngestClient = KustoIngestFactory.CreateQueuedIngestClient(connectionBuilder))
            using (var memoryStream = new MemoryStream())
            using (var streamWriter = new StreamWriter(memoryStream))
            {
                streamWriter.Write(text);
                streamWriter.Flush();
                memoryStream.Seek(0, SeekOrigin.Begin);

                // Post ingestion message
                kustoIngestClient.IngestFromStream(memoryStream, IngestionProperties);
            }
        }

        private static List<ColumnSchema> GetDefaultColumns()
        {
            // The list of default columns which are present in our main kusto db.
            return new List<ColumnSchema>()
            {
                new ColumnSchema("TIMESTAMP", "System.DateTime"),
                new ColumnSchema("msg", "System.String"),
                new ColumnSchema("level", "System.String"),
                new ColumnSchema("time", "System.DateTime"),
                new ColumnSchema("FluentdIngestTimestamp", "System.DateTime"),
                new ColumnSchema("PreciseTimeStamp", "System.DateTime"),
            };
        }

        private (bool, string[]) IsColumnUpdateNeeded(string text)
        {
            var payload = JObject.Parse(text);
            var payloadItemKeys = payload.ToDictionary().Select(x => x.Key).ToArray();
            var currentColumns = ColumnMappings.Select(x => x.ColumnName).ToArray();
            var deltaKeys = payloadItemKeys.Except(currentColumns).ToArray();
            return (deltaKeys.Any(), payloadItemKeys);
        }

        private void UpdateColumnMapping(string clientId, string key, string tenant, string[] payloadItemKeys)
        {
            // Recompute current columns as it could have been changed.
            var currentColumns = ColumnMappings.Select(x => x.ColumnName).ToArray();
            var deltaKeys = payloadItemKeys.Except(currentColumns).ToArray();

            foreach (var item in deltaKeys)
            {
                var properties = new Dictionary<string, string>()
                {
                    ["Path"] = $"$.{item}",
                };

                // Any new item would be of string type.
                ColumnMappings.Add(new ColumnMapping()
                {
                    ColumnName = item,
                    ColumnType = "string",
                    Properties = properties,
                });
            }

            var devKustoEngineConnectionBuilder = new KustoConnectionStringBuilder(DevKustoUri).WithAadApplicationKeyAuthentication(clientId, key, tenant);
            devKustoEngineConnectionBuilder.InitialCatalog = KustoDatabaseName;

            using (var kustoAdminClient = KustoClientFactory.CreateCslAdminProvider(devKustoEngineConnectionBuilder))
            {
                var generatedMappingCommand = CslCommandGenerator.GenerateTableMappingAlterCommand(Kusto.Data.Ingestion.IngestionMappingKind.Json, TableName, TableMapName, ColumnMappings);
                kustoAdminClient.ExecuteControlCommand(generatedMappingCommand);
            }
        }

        private void Initialize(string clientId, string key, string tenant)
        {
            var devKustoEngineConnectionBuilder = new KustoConnectionStringBuilder(DevKustoUri).WithAadApplicationKeyAuthentication(clientId, key, tenant);
            devKustoEngineConnectionBuilder.InitialCatalog = KustoDatabaseName;

            using (var kustoAdminClient = KustoClientFactory.CreateCslAdminProvider(devKustoEngineConnectionBuilder))
            {
                var ts = new TableSchema(
                   TableName,
                   GetDefaultColumns());
                var createTable = CslCommandGenerator.GenerateTableCreateCommand(ts);
                kustoAdminClient.ExecuteControlCommand(createTable);

                var showSchemaCommand = CslCommandGenerator.GenerateTableSchemaShowAsJsonCommand(TableName, KustoDatabaseName);
                var schema = kustoAdminClient.ExecuteControlCommand(showSchemaCommand);

                var tableSchema = default(string);
                schema.Read();
                var row = schema as IDataRecord;
                tableSchema = row[1] as string;

                var jsonSchema = JObject.Parse(tableSchema);
                var orderedColumns = jsonSchema["OrderedColumns"];
                foreach (var item in orderedColumns)
                {
                    var name = item["Name"].ToString();
                    var properties = new Dictionary<string, string>()
                    {
                        ["Path"] = $"$.{name}",
                    };

                    ColumnMappings.Add(new ColumnMapping()
                    {
                        ColumnName = name,
                        Properties = properties,
                    });
                }

                var generatedMappingCommand = CslCommandGenerator.GenerateTableMappingCreateCommand(Kusto.Data.Ingestion.IngestionMappingKind.Json, TableName, TableMapName, ColumnMappings);
                kustoAdminClient.ExecuteControlCommand(generatedMappingCommand);
            }

            var ingestionProperties = new KustoIngestionProperties(KustoDatabaseName, TableName)
            {
                Format = Kusto.Data.Common.DataSourceFormat.json,
            };

            ingestionProperties.IngestionMapping = new IngestionMapping()
            {
                IngestionMappingKind = Kusto.Data.Ingestion.IngestionMappingKind.Json,
                IngestionMappingReference = TableMapName,
            };

            IngestionProperties = ingestionProperties;
        }
    }
}
