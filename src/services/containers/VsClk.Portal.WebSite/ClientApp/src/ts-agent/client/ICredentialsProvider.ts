export interface ICredentialsProvider {
    getToken(sessionId: string): string;
}
