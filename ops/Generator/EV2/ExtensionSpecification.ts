/** The document representation of the Rollout Extension Specification Model. */
export interface IExtensionSpecificationSchema {
    /** The version of the schema that a document conforms to. */
    contentVersion: string;
    /** Extension definition */
    extension: IExtension;
}

export interface IExtension {
    /** Extension Namespace, e.g. 'Microsoft.Azure' */
    namespace: string;
    /** Extension Type, e.g. 'TestExtension' */
    type: string;
    /** Extension Description */
    description: string;
    /** Extension Endpoints */
    endpoints: any[];
    /** Extension state, enable or disable */
    enabled: boolean;
    /** Extension Owner Group Azure Active Directory ObjectId */
    ownerGroupObjectId: string;
    /** Extension Incident Escalation Alias */
    incidentEscalationAlias: string;
    /**  the default poll interval to query the extension result if it's not finished yet, in ISO 8601 format, ex: 'PT1M' for 1 minute */
    asyncPollInterval: string;
    /** Extension System Version, e.g. '2016-11-01' */
    systemVersion: string;
}