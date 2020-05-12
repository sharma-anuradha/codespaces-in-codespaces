type TCredentialHost = 'github.com' | 'azure.com';

export interface IGitCredential {
    readonly expiration?: number;
    readonly path?: string;
    readonly host: TCredentialHost;
    readonly token: string;
}
