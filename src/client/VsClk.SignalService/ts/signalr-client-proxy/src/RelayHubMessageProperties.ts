
export class RelayHubMessageProperties {
    
    public static propertySequenceId = 'sequenceId';

    public static propertyAuditPrefixId = 'audit-';

    public static propertyAuditPerfTypeId = 'perfType';

    public static createMessageSequence(sequence: number): { [key: string]: any; } {
        return {
            sequenceId: sequence,
        };
    }

    public static getMessageSequence(properties: { [key: string]: any; }): number | undefined  {
        if (properties && properties.hasOwnProperty(RelayHubMessageProperties.propertySequenceId)) {
            return Number(properties[RelayHubMessageProperties.propertySequenceId]);
        }
    }
}