export interface Credentials {
    readonly token: string;
}

export class CredentialsManager {
    private readonly credentials = new Map<string, Credentials>();

    setCredentials(sessionId: string, credentials: Credentials) {
        this.credentials.set(sessionId, credentials);
    }

    getCredentials(sessionId: string): Credentials | undefined {
        return this.credentials.get(sessionId);
    }

    deleteCredentials(sessionId: string) {
        this.credentials.delete(sessionId);
    }
}
