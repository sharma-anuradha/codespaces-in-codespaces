export interface IKeychain {
    get(key: string): Promise<string | undefined | null>;
    set(key: string, value: string): Promise<void>;
    delete(key: string): void;
}
