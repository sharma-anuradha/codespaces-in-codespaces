export interface Credentials {
    readonly token: string;
}

export class CredentialsManager {
    private readonly credentials = new Map<string, Credentials>();

    setCredentials(sessionId: string, credentials: Credentials) {
        this.credentials.set(sessionId.toUpperCase(), credentials);
    }

    getCredentials(sessionId: string): Credentials | undefined {
        return this.credentials.get(sessionId.toUpperCase());
    }

    deleteCredentials(sessionId: string) {
        this.credentials.delete(sessionId.toUpperCase());
    }
}
