export interface IAuthStrategy {
    canHandleService(service: string, account: string): Promise<boolean>;
    getToken(service: string, account: string): Promise<string | null>;
}
