export interface IGitCredential {
    readonly provider: string;
    readonly url: string;
    readonly username: string;
    readonly email: string;
    readonly token: string;
}
