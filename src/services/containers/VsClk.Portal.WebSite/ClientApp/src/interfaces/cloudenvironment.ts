
export interface ICloudEnvironment {
    id: string;
    ownerId: string;
    friendlyName: string;
    created: Date;
    updated: Date;
    active: Date;
    connection: {
        sessionId: string;
        sessionPath: string;
    }
}
