export interface Credentials {
    readonly token: string;
}

export interface ICredentialsManager {
    setCredentials(sessionId: string, credentials: Credentials): void;
    getCredentials(sessionId: string): Credentials | undefined;
    deleteCredentials(sessionId: string): void;
}

export class CredentialsManager implements ICredentialsManager {
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

export class SimpleCredentialsManager implements ICredentialsManager {
    private credentials?: Credentials;

    setCredentials(sessionId: string, credentials: Credentials) {
        this.credentials = credentials;
    }

    getCredentials(sessionId: string): Credentials | undefined {
        return this.credentials;
    }

    deleteCredentials(sessionId: string) {
        this.credentials = undefined;
    }
}
