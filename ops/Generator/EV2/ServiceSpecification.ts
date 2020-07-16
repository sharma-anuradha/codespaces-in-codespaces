/** The document representation of the Service Specification of a Service. */
export interface IServiceSpecification {
    /** The version of the schema that a document conforms to. */
    contentVersion: string;
    /** The unique identifier for the service registered with Azure Deployment Manager. */
    identifier: string;
    /** A brief description for the service. */
    description: string;
    /** The owner security group object identifier. */
    ownerGroupObjectId: string;
    /** The display name for the owner group object identifier. */
    ownerGroupDisplayName: string;
    /** The email contact alias for the owner group. */
    ownerGroupContactEmail: string;
    /** The value indicating if all rollouts for this service should use a policy. */
    policyCheckEnabled: boolean;
}