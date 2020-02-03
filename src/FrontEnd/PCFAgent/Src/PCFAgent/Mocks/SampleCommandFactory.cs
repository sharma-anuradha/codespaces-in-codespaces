// <copyright file="SampleCommandFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

#pragma warning disable

using System;
using System.Collections.Generic;

using Microsoft.PrivacyServices.CommandFeed.Client;
using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
using Microsoft.PrivacyServices.Policy;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PCFAgent
{
    /// <summary>
    /// Generates sample commands according to the desired requirements.
    /// This code is copied from PCF Samples.
    /// </summary>
    public class SampleCommandFactory
    {
        // AAD subject TenantId example
        private static readonly Guid tenantId = Guid.Parse("72f988bf-86f1-41af-91ab-2d7cd011db47");

        // AAD subject ObjectId example
        private static readonly Guid objectId = Guid.Parse("9a4c7299-32e8-43ec-8d0b-20f37d7e7ac6");

        // AAD subject OrgId PUID example
        private static readonly long orgIdPuid = 123456789123456789;

        // MSA subject Anid example
        private static readonly string anid = "928D5FE23E2DD1F3F5AEAD19FFFFFFFF";

        // MSA subject Cid example
        private static readonly long cid = 3219959886081161043;

        // MSA subject Opid example
        private static readonly string opid = "vdPQVboQwcJSkQF2RgRnxA2";

        // MSA subject Puid example
        private static readonly long puid = 0x000300000A80842A;

        // MSA subject Xuid example
        private static readonly string xuid = "2535439314266772";

        // When using Insert Command to inject synthetic commands, we recommend using an empty GUID as the AssetGroupId.
        // The service may overwrite this value before storing it; don't expect it to be the same when you retrieve the command later.
        private static readonly string assetGroupId = Guid.Empty.ToString();

        // Verifier example
        private static readonly string verifier = string.Empty;

        // Lease receipt example
        private static readonly string leaseReceipt = Guid.NewGuid().ToString();

        // Your agent state example
        private static readonly string agentState = "ExampleAgentState";

        // Sample correlation vector
        private static readonly string cv = "abc123.1.2.3";

        /// <summary>
        /// A sample AAD subject with static values defined above.
        /// </summary>
        private static readonly AadSubject sampleAadSubject = new AadSubject
        {
            TenantId = tenantId,
            ObjectId = objectId,
            OrgIdPUID = orgIdPuid
        };

        /// <summary>
        /// A sample MSA subject with static values defined above.
        /// </summary>
        private static readonly MsaSubject sampleMsaSubject = new MsaSubject
        {
            Puid = puid,
            Anid = anid,
            Cid = cid,
            Opid = opid,
            Xuid = xuid
        };

        /// <summary>
        /// A sample Alternate subject representing a Microsoft employee.
        /// </summary>
        private static readonly DemographicSubject sampleMicrosoftEmployeeSubject = new DemographicSubject
        {
            MicrosoftEmployee = new MicrosoftEmployee
            {
                Emails = new[] { "alias", @"DOMAIN\alias" },
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = DateTime.Today,
                EmployeeId = "msid1234"
            }
        };

        /// <summary>
        /// A sample Alternate subject.
        /// </summary>
        private static readonly DemographicSubject sampleAlternateSubject = new DemographicSubject
        {
            Names = new[] { "John", "Doe", "John Doe" },
            EmailAddresses = new[] { "billg@microsoft.com" },
            PhoneNumbers = new[] { "14258828080", "(425) 882-8080", "+1 (425) 882-8080" },
            Address = new AddressQueryParams
            {
                StreetNumbers = new[] { "39", "1" },
                Streets = new[] { "quai du Président", "Microsoft Way" },
                UnitNumbers = new[] { "room 1234", "84/1234" },
                Cities = new[] { "Roosevelt", "Redmond" },
                States = new[] { "Issy-les-Moulineaux", "Washington", "WA" },
                PostalCodes = new[] { "92130", "98052" }
            }
        };

        /// <summary>
        /// Gets a sample AAD Account Close command.
        /// </summary>
        /// <param name="assetGroupQualifier">The asset group qualifier</param>
        /// <param name="client">The Command Feed client</param>
        /// <returns>Returns a privacy command of type <see cref="IAccountCloseCommand"/></returns>
        public static IAccountCloseCommand GetAadAccountCloseSampleCommand(string assetGroupQualifier, ICommandFeedClient client, string cloudInstance = "Public", Random random = null)
        {
            return GetAccountCloseSampleCommand(assetGroupQualifier, client, typeof(AadSubject), cloudInstance);
        }

        /// <summary>
        /// Gets a sample MSA Account Close command.
        /// </summary>
        /// <param name="assetGroupQualifier">The asset group qualifier</param>
        /// <param name="client">The Command Feed client</param>
        /// <returns>Returns a privacy command of type <see cref="IAccountCloseCommand"/></returns>
        public static IAccountCloseCommand GetMsaAccountCloseSampleCommand(string assetGroupQualifier, ICommandFeedClient client)
        {
            return GetAccountCloseSampleCommand(assetGroupQualifier, client, typeof(MsaSubject), Policies.Current.CloudInstances.Ids.Public.Value);
        }

        /// <summary>
        /// Gets a sample Account Close command.
        /// </summary>
        /// <param name="assetGroupQualifier">The asset group qualifier</param>
        /// <param name="client">The Command Feed client</param>
        /// <param name="subjectType">The subject type</param>
        /// <param name="cloudInstance">The <see cref="CloudInstance"/></param>
        /// <param name="random">A random number generator to create different subject data</param>
        /// <returns>Returns a privacy command of type <see cref="IAccountCloseCommand"/></returns>
        public static IAccountCloseCommand GetAccountCloseSampleCommand(string assetGroupQualifier, ICommandFeedClient client, Type subjectType, string cloudInstance, Random random = null)
        {
            return new AccountCloseCommand(
                commandId: Guid.NewGuid().ToString(),
                assetGroupId: assetGroupId,
                assetGroupQualifier: assetGroupQualifier,
                verifier: verifier,
                correlationVector: cv,
                leaseReceipt: leaseReceipt,
                approximateLeaseExpiration: DateTimeOffset.UtcNow.AddMinutes(1),
                createdTime: DateTimeOffset.UtcNow,
                subject: CreateSubject(subjectType, random),
                agentState: agentState,
                cloudInstance: cloudInstance,
                commandFeedClient: client);
        }

        /// <summary>
        /// Gets a sample MSA Delete command.
        /// </summary>
        /// <param name="assetGroupQualifier">The asset group qualifier</param>
        /// <param name="client">The Command Feed client</param>
        /// <param name="predicateTypeId">The predicate type identifier. This parameter can be null.</param>
        /// <returns>Returns a privacy command of type <see cref="IDeleteCommand"/></returns>
        public static IDeleteCommand GetMsaDeleteCustomerContentSampleCommand(string assetGroupQualifier, ICommandFeedClient client, DataTypeId predicateTypeId, Random randrom = null)
        {
            return GetDeleteCustomerContentSampleCommand(
                assetGroupQualifier: assetGroupQualifier,
                subjectType: typeof(MsaSubject),
                client: client,
                predicateTypeId: predicateTypeId,
                random: randrom
            );
        }

        /// <summary>
        /// Gets a sample Delete command.
        /// </summary>
        /// <param name="assetGroupQualifier">The asset group qualifier</param>
        /// <param name="client">The Command Feed client</param>
        /// <param name="predicateTypeId">The predicate type identifier. This parameter can be null.</param>
        /// <param name="random">A random number generator to create different subject data</param>
        /// <param name="createMicrosoftEmployeeAlternateSubject">
        /// Optional bool to create an alternate subject of Microsoft employee type.
        /// </param>
        /// <returns>Returns a privacy command of type <see cref="IDeleteCommand"/></returns>
        public static IDeleteCommand GetDeleteCustomerContentSampleCommand(
            string assetGroupQualifier,
            Type subjectType,
            ICommandFeedClient client,
            DataTypeId predicateTypeId,
            Random random = null,
            bool createMicrosoftEmployeeAlternateSubject = false)
        {
            IPrivacyPredicate dataTypePredicate = null;
            if (predicateTypeId != null)
            {
                dataTypePredicate = CreateDeleteDataTypePredicate(predicateTypeId);
            }

            return new DeleteCommand(
                commandId: Guid.NewGuid().ToString(),
                assetGroupId: assetGroupId,
                assetGroupQualifier: assetGroupQualifier,
                verifier: verifier,
                correlationVector: cv,
                leaseReceipt: leaseReceipt,
                approximateLeaseExpiration: DateTimeOffset.UtcNow.AddMinutes(1),
                createdTime: DateTimeOffset.UtcNow,
                subject: CreateSubject(subjectType, random, createMicrosoftEmployeeAlternateSubject),
                agentState: agentState,
                dataTypePredicate: dataTypePredicate,
                dataType: predicateTypeId,
                timeRangePredicate: new TimeRangePredicate
                {
                    StartTime = DateTimeOffset.UtcNow.AddMinutes(-30),
                    EndTime = DateTimeOffset.UtcNow.AddMinutes(30)
                },
                cloudInstance: null, // This is the same as CloudInstance.Public
                commandFeedClient: client);
        }

        /// <summary>
        /// Gets a sample MSA Export command.
        /// </summary>
        /// <param name="assetGroupQualifier">The asset group qualifier</param>
        /// <param name="client">The Command Feed client</param>
        /// <param name="dataTypeIds">The data type ids to be exported</param>
        /// <param name="azureBlobUri">The azure blob uri to which the data will be exported</param>
        /// <returns>Returns a privacy command of type <see cref="IExportCommand"/></returns>
        public static IExportCommand GetMsaExportSampleCommand(string assetGroupQualifier, ICommandFeedClient client, IEnumerable<DataTypeId> dataTypeIds, Uri azureBlobUri)
        {
            // A null cloud instance is interpreted as CloudInstance.Public
            return GetExportSampleCommand(assetGroupQualifier, client, typeof(MsaSubject), dataTypeIds, azureBlobUri, null);
        }

        /// <summary>
        /// Gets a sample Export command.
        /// </summary>
        /// <param name="assetGroupQualifier">The asset group qualifier</param>
        /// <param name="client">The Command Feed client</param>
        /// <param name="subjectType">The subject type</param>
        /// <param name="dataTypeIds">The data type ids to be exported</param>
        /// <param name="azureBlobUri">The azure blob uri to which the data will be exported</param>
        /// <param name="cloudInstance">The <see cref="CloudInstance"/></param>
        /// <param name="random">A random number generator to create different subject data</param>
        /// <param name="createMicrosoftEmployeeAlternateSubject">
        /// Optional bool to create an alternate subject of Microsoft employee type.
        /// </param>
        /// <returns>Returns a privacy command of type <see cref="IExportCommand"/></returns>
        public static IExportCommand GetExportSampleCommand(
            string assetGroupQualifier,
            ICommandFeedClient client,
            Type subjectType,
            IEnumerable<DataTypeId> dataTypeIds,
            Uri azureBlobUri,
            string cloudInstance,
            Random random = null,
            bool createMicrosoftEmployeeAlternateSubject = false)
        {
            return new ExportCommand(
                commandId: Guid.NewGuid().ToString(),
                assetGroupId: assetGroupId,
                assetGroupQualifier: assetGroupQualifier,
                verifier: verifier,
                correlationVector: cv,
                leaseReceipt: leaseReceipt,
                approximateLeaseExpiration: DateTimeOffset.UtcNow.AddMinutes(1),
                createdTime: DateTimeOffset.UtcNow,
                subject: CreateSubject(subjectType, random, createMicrosoftEmployeeAlternateSubject),
                agentState: agentState,
                dataTypes: dataTypeIds,
                azureBlobUri: azureBlobUri,
                cloudInstance: cloudInstance,
                commandFeedClient: client);
        }

        /// <summary>
        /// Gets a sample MSA AgeOut command.
        /// </summary>
        /// <param name="assetGroupQualifier">The asset group qualifier</param>
        /// <param name="client">The Command Feed client</param>
        /// <param name="lastActive">The last active time for this MSA account</param>
        /// <param name="random">A random number generator to create different subject data</param>
        /// <returns>Returns a privacy command of type <see cref="IAgeOutCommand"/></returns>
        public static IAgeOutCommand GetMsaAgeOutSampleCommand(string assetGroupQualifier, ICommandFeedClient client, DateTimeOffset lastActive, Random random = null)
        {
            return new AgeOutCommand(
                commandId: Guid.NewGuid().ToString(),
                assetGroupId: assetGroupId,
                assetGroupQualifier: assetGroupQualifier,
                verifier: verifier,
                correlationVector: cv,
                leaseReceipt: leaseReceipt,
                approximateLeaseExpiration: DateTimeOffset.UtcNow.AddMinutes(1),
                createdTime: DateTimeOffset.UtcNow,
                subject: CreateSubject(typeof(MsaSubject), random),
                agentState: agentState,
                commandFeedClient: client,
                lastActive: lastActive,
                false,
                cloudInstance: Policies.Current.CloudInstances.Ids.Public.Value);
        }

        /// <summary>
        /// Creates a subject of a desired type.
        /// </summary>
        /// <param name="subjectType">The subject type</param>
        /// <param name="random">
        /// Optional random number generator used to create random subjects instead of hardcoded subjects.
        /// </param>
        /// <param name="createMicrosoftEmployeeAlternateSubject">
        /// Optional bool to create an alternate subject of Microsoft employee type.
        /// </param>
        /// <returns>Returns a <see cref="IPrivacySubject"/></returns>
        public static IPrivacySubject CreateSubject(Type subjectType, Random random = null, bool createMicrosoftEmployeeAlternateSubject = false)
        {
            if (subjectType == typeof(AadSubject))
            {
                if (random == null)
                {
                    return sampleAadSubject;
                }
                else
                {
                    return new AadSubject
                    {
                        TenantId = Guid.NewGuid(),
                        ObjectId = Guid.NewGuid(),
                        OrgIdPUID = random.Next()
                    };
                }
            }

            if (subjectType == typeof(MsaSubject))
            {
                if (random == null)
                {
                    return sampleMsaSubject;
                }
                else
                {
                    return new MsaSubject
                    {
                        Anid = "anid" + Guid.NewGuid(),
                        Cid = random.Next(),
                        Opid = "opid" + Guid.NewGuid(),
                        Puid = random.Next(),
                        Xuid = "uid" + Guid.NewGuid()
                    };
                }
            }

            if (subjectType == typeof(DeviceSubject))
            {
                return new DeviceSubject
                {
                    GlobalDeviceId = random.Next(),
                    XboxConsoleId = random.Next()
                };
            }

            if (createMicrosoftEmployeeAlternateSubject)
            {
                return sampleMicrosoftEmployeeSubject;
            }
            else
            {
                return sampleAlternateSubject;
            }
        }

        private static IPrivacyPredicate CreateDeleteDataTypePredicate(DataTypeId privacyDataType)
        {
            var ids = Policies.Current.DataTypes.Ids;

            if (privacyDataType == ids.BrowsingHistory)
            {
                return new BrowsingHistoryPredicate { UriHash = Guid.NewGuid().ToString() };
            }
            else if (privacyDataType == ids.SearchRequestsAndQuery)
            {
                return new SearchRequestsAndQueryPredicate { ImpressionGuid = Guid.NewGuid().ToString() };
            }
            else if (privacyDataType == ids.ProductAndServiceUsage)
            {
                return new ProductAndServiceUsagePredicate
                {
                    AppId = Guid.NewGuid().ToString(),
                    PropertyBag = new Dictionary<string, List<string>>
                    {
                        { "key1", new List<string> { "value1", "value2", "value3" } },
                        { "key2", new List<string> { "value4", "value5", "value6" } }
                    }
                };
            }
            else if (privacyDataType == ids.ContentConsumption)
            {
                return new ContentConsumptionPredicate { ContentId = Guid.NewGuid().ToString() };
            }
            else if (privacyDataType == ids.InkingTypingAndSpeechUtterance)
            {
                return new InkingTypingAndSpeechUtterancePredicate { ImpressionGuid = Guid.NewGuid().ToString() };
            }

            return null;
        }
    }
}