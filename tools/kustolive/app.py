import io
import gzip
import time
import getpass
import sys
import uuid
import os
import json
from azure.kusto.data.request import KustoClient, KustoConnectionStringBuilder, ClientRequestProperties
from azure.kusto.data.exceptions import KustoServiceError
from azure.kusto.data.helpers import dataframe_from_result_table
from azure.kusto.ingest import DataFormat

KUSTO_URI = "https://vsodevkusto.westus2.kusto.windows.net:443/"
KUSTO_INGEST_URI = "https://ingest-vsodevkusto.westus2.kusto.windows.net:443/"
KUSTO_DATABASE = "VsoDevStampEventLogs"

AAD_TENANT_ID = "72f988bf-86f1-41af-91ab-2d7cd011db47"
AAD_APP_ID = "b720128c-1a02-4cbe-8aa8-004cdf393123"
AAD_APP_SECRET = ""

FORCE_UPDATE_TABLE_OR_MAPPING = False
DEV_INSTANCE_ID_KEY = 'DevInstanceId'
DEV_INSTANCE_ID = uuid.uuid4()
CURRENT_USERNAME = getpass.getuser()
DESTINATION_TABLE = CURRENT_USERNAME + 'Events'
DESTINATION_TABLE_COLUMN_MAPPING = "jsonmapping1"
LINE_SUFFIX = ',"{}":"{}"'.format(DEV_INSTANCE_ID_KEY, DEV_INSTANCE_ID) + '}'

with open(os.path.expanduser("~/CEDev/appsettings.json"), 'r') as f:
    settings_dict = json.load(f)
    AAD_APP_SECRET = settings_dict['AppSecrets']['appServicePrincipalClientSecret']

TABLE_SCHEMA = "TIMESTAMP:datetime,['stream']:string,msg:string,['time']:datetime,level:string,WorkerLevelPreCount:string,WorkerLevelPostCount:string,WorkerLevelOverCapacityCount:string,WorkerLevelUnderCapacityCount:string,WorkerLevelBelowMinimumCount:string,WorkerLevelAboveMaximumCount:string,WorkerLevelRestartedCount:string,Duration:string,TaskName:string,TaskManagerId:string,Service:string,CommitId:string,ServiceEnvironment:string,ServiceInstance:string,ServiceStamp:string,ServiceLocation:string,tag:string,FluentdIngestTimestamp:datetime,Tenant:string,Role:string,RoleInstance:string,PreciseTimeStamp:datetime,ContinuationActivityLevel:string,WorkerFoundMessages:string,WorkerRunDuration:string,ContinuationWorkerId:string,ContinuationWorkerRunId:string,QueuePopCount:string,QueueVisibilityTimeout:string,QueueFoundItems:string,PumpPreCacheLevel:string,PumpCacheHit:string,PumpFoundMessage:string,PumpPostCacheLevel:string,PumpTargetLevel:string,PumpFillDidTrigger:string,PumpFoundItems:string,ProcessVirtualMemorySize:string,ProcessPagedMemorySize:string,ProcessId:string,ProcessPrivateMemorySize:string,ProcessWorkingSet:string,HttpRequestUserAgent:string,HttpRequestMethod:string,HttpRequestHost:string,HttpRequestUri:string,CorrelationId:string,HttpRequestId:string,HttpResponseStatus:string,CloudEnvironmentId:string,RequestCharge:string,OwnerId:string,Type:string,State:string,SessionId:string,ComputeId:string,StorageId:string,HttpResponseDuration:string,TaskCountResourceUnits:string,PoolImageName:string,PoolImageFamilyName:string,PoolTargetCount:string,PoolVersioDefinition:string,PoolDefinition:string,PoolResourceType:string,PoolSkuName:string,PoolLocation:string,TaskRunId:string,LeaseNotFound:string,VersionUnassignedNotVersionCount:string,RunPoolActionDuration:string,SizeCheckUnassignedCount:string,SizeCheckPoolTargetCount:string,SizeCheckPoolDeltaCount:string,PumpDequeueCount:string,PumpExpirationTime:string,PumpInsertionTime:string,PumpNextVisibleTime:string,ResourceRecordId:string,PartitionKeyPaths:string,ResourceId:string,ContinuationPayloadTrackingId:string,ContinuationActivatorId:string,RetryAfter:string,ContinuationToken:string,Status:string,AzureVmLocation:string,Name:string,ResourceGroup:string,SubscriptionId:string,HandlerOperation:string,HandlerType:string,HandlerBasePreContinuationToken:string,HandlerTriggerSource:string,HandlerOperationPreContinuationToken:string,HandlerObtainReferenceDuration:string,HandlerOperationPostContinuationToken:string,HandlerOperationPostStatus:string,HandlerOperationPostRetryAfter:string,HandlerRunOperationDuration:string,HandlerBasePostContinuationToken:string,HandlerBasePostStatus:string,HandlerBasePostRetryAfter:string,QueueVisibilityDelay:string,ContinuationPayloadHandleTarget:string,ContinuationPayloadIsInitial:string,ContinuationPayloadPreStatus:string,ContinuationPayloadCreated:string,ContinuationPayloadCreateOffSet:string,ContinuationPayloadStepCount:string,ContinuationHandler:string,ContinuationHandlerDuration:string,ContinuationHandlerFailed:string,ContinuationPayloadPostStatus:string,ContinuationPayloadPostRetryAfter:string,ContinuationPayloadIsFinal:string,ContinuationHandlerToken:string,ContinuationWasHandled:string,ContinuationFindHandleDuration:string,WorkerActivatorDuration:string,Count:string,SourceNamespace:string,SourceMoniker:string,SourceVersion:string,AzureResourceInfo:string,AzureVirtualMachineImage:string,AzureSkuName:string,AzureResourceGroup:string,AzureSubscription:string,ContinuationWorkerEndReason:string,CompletedAmount:string,AzureLocation:string,AzureStorageAccountName:string,ServiceEndpoint:string,ReadEndpoint:string,WriteEndpoint:string,ConnectionMode:string,ConnectionProtocol:string,Attempt:string,ETag:string,BillingEventId:string,Account:string,HandlerBuildOperationInputDuration:string,jwt_jti:string,jwt_sub:string,HandlerFailedToFindResource:string,ErrorException:string,ErrorMessage:string,ErrorDetail:string,ContinuationHandlerExceptionThrew:string,ContinuationHandlerExceptionMessage:string,VsoAccountId:string,StorageResourceId:string,HandlerQueueOperationDuration:string,EnvironmentVariables:string,StorageAccountName:string,AttemptsTaken:string,AzureRegion:string,DestinationStorageFilePath:string,HandlerOperationExceptionThrew:string,HandlerOperationExceptionMessage:string,HandlerFailedGetResultFromOperationException:string,HandlerFailedGetResultFromOperation:string,HandlerFailCleanupTriggered:string,HandlerFailCleanupTrigger:string,HandlerFailCleanupPostState:string,HandlerFailCleanupPostContinuationToken:string,TaskJobIteration:string,Scheme:string,Authority:string,RequestUri:string,Exception:string,CopyStatus:string,CopyStatusDescription:string,TaskResourceType:string,TaskResourceSkuName:string,TaskResourceLocation:string,VersionPoolDefinition:string,VersionPoolVersioDefinition:string,VersionPoolTargetCount:string,VersionPoolImageFamilyName:string,VersionPoolImageName:string,TaskCheckUnassignedCount:string,TaskCheckPoolTargetCount:string,TaskCheckPoolDeltaCount:string,PoolCode:string,ResourceLocation:string,ResourceSystemSkuName:string,ResourceType:string,ResourceResourceSkuName:string,PoolLookupTry:string,PoolLookupFoundItem:string,PoolLookupUpdateConflict:string,ResourceResourceAllocateFound:string,Location:string,SkuName:string,Created:string,LocationHeader:string,TaskIterationId:string,Reason:string,VersionReadyUnassignedCount:string,VersionReadyUnassignedRate:string,appSettingJsonFiles:string,appSettings:string,currentAzureLocation:string,environmentName:string,ContinuationWorkerStartReason:string,ContinuationStartLevel:string,VersionDropCount:string,VersionDropFoundCount:string,HttpResponseContentLength:string,LeaseIsFirstRun:string,LeaseObtainId:string,LeaseContainerName:string,LeaseName:string,LeaseClaimTimeSpan:string,LeaseTimeFromStartDuration:string,LeaseCurrentTime:string,PoolIsEnabled:string,PoolUnassignedCount:string,PoolUnassignedVersionCount:string,PoolUnassignedNotVersionCount:string,PoolReadyUnassignedCount:string,PoolReadyUnassignedVersionCount:string,ResourcePoolStateSnapshotRecordId:string,PoolReadyUnassignedNotVersionCount:string,PoolIsAtTargetCount:string,PoolIsReadyAtTargetCount:string,HandlerIsPoolEnabled:string,PoolLookupRunId:string,PoolLookupAttemptRunId:string,LeasetRenewCount:string,LeaseRenewIsDisposed:string,LeaseClaimLockFail:string,TotalMemory:string,ActivityId:string,serviceType:string,['location']:string,subscription:string,CapacityRecordId:string,subscriptionName:string,quota:string,['limit']:string,currentValue:string,usedPercent:string,subscriptionId:string,['enabled']:string,PoolDrainCountFound:string,resourceGroup:string,candidateSubscriptions:string,criteria:string,subscriptions:string,accountName:string,PoolVersionDefinition:string,TaskFailedStatusItem:string,TaskFailedStalledItem:string,TaskDidFailProvisioning:string,TaskDidFailStarting:string,TaskDidFailDeleting:string,DeleteAttemptCount:string,TaskFailedItemRunId:string,TaskRequestedItems:string,TaskFoundItems:string,TaskResourceResourceGroup:string,TaskResourceSubscription:string,DevInstanceId:string"

def process_schema_part(process_schema_part):
    lhs, rhs = process_schema_part.split(':')
    if lhs.startswith("['") and lhs.endswith("']"):
        lhs = lhs[2:-2]
    return f'{{"column":"{lhs}","path":"$.{lhs}"}}'

# See https://docs.microsoft.com/en-us/azure/kusto/management/mappings#json-mapping
mapping_parts = [process_schema_part(s) for s in TABLE_SCHEMA.split(',')]
JSON_MAPPING = f'[{",".join(mapping_parts)}]'

KCSB_INGEST = KustoConnectionStringBuilder.with_aad_application_key_authentication( KUSTO_INGEST_URI, AAD_APP_ID, AAD_APP_SECRET, AAD_TENANT_ID)
KCSB_DATA = KustoConnectionStringBuilder.with_aad_application_key_authentication( KUSTO_URI, AAD_APP_ID, AAD_APP_SECRET, AAD_TENANT_ID)
KUSTO_CLIENT = KustoClient(KCSB_DATA)

def table_exists():
    SHOW_TABLE_COMMAND = f".show tables ({DESTINATION_TABLE})"
    RESPONSE = KUSTO_CLIENT.execute_mgmt(KUSTO_DATABASE, SHOW_TABLE_COMMAND)
    show_table_response = dataframe_from_result_table(RESPONSE.primary_results[0])
    return False if show_table_response.empty else True

def create_mapping():
    # Create json mapping. If mapping exists, this updates the mapping if the mapping has changed - https://docs.microsoft.com/en-us/azure/kusto/management/tables#create-ingestion-mapping
    CREATE_MAPPING_COMMAND = (
        f""".create-or-alter table {DESTINATION_TABLE} """
        f"""ingestion json mapping '{DESTINATION_TABLE_COLUMN_MAPPING}' """
        f"""'{JSON_MAPPING}'""")
    RESPONSE = KUSTO_CLIENT.execute_mgmt(KUSTO_DATABASE, CREATE_MAPPING_COMMAND)
    dataframe_from_result_table(RESPONSE.primary_results[0])
    # Waiting 30 seconds for the table to be set up
    time.sleep(30)

def create_table():
    # Create table. If table exists, this is basically a no-op - https://docs.microsoft.com/en-us/azure/kusto/management/tables#create-table
    print(f'Creating or updating table {DESTINATION_TABLE}')
    CREATE_TABLE_COMMAND = f".create table {DESTINATION_TABLE} ({TABLE_SCHEMA})"
    RESPONSE = KUSTO_CLIENT.execute_mgmt(KUSTO_DATABASE, CREATE_TABLE_COMMAND)
    dataframe_from_result_table(RESPONSE.primary_results[0])
    # Waiting 30 seconds for the table to be set up
    time.sleep(30)
    print('Table update done.')

def send_out(o):
    KUSTO_CLIENT.execute_streaming_ingest(KUSTO_DATABASE, DESTINATION_TABLE, o, 'json', mapping_name=DESTINATION_TABLE_COLUMN_MAPPING)


def process_chunk():
    chunk_start_time = time.time()
    out = io.BytesIO()
    with gzip.GzipFile(fileobj=out, mode="w") as f:
        for line in sys.stdin:
            line = line.rstrip()
            if line.startswith('{') and line.endswith('}'):
                line = line[:-1] + LINE_SUFFIX
                f.write(str.encode(line))
                f.write(b'\n')
                cur_time = time.time()
                if cur_time - chunk_start_time > 10: # 10 seconds
                    break;
    return out

try:
    if (not table_exists() or FORCE_UPDATE_TABLE_OR_MAPPING):
        create_table()
        create_mapping()
    else:
        print(f'Using table: {DESTINATION_TABLE}')
    print(f"Sending json output to Kusto. Uri={KUSTO_URI} Table={DESTINATION_TABLE}")
    print(f'{DESTINATION_TABLE} | where {DEV_INSTANCE_ID_KEY} == "{DEV_INSTANCE_ID}" | order by [\'time\'] desc')
    while True:
        chuck_output = process_chunk()
        send_out(chuck_output.getvalue())
except KeyboardInterrupt:
    pass
