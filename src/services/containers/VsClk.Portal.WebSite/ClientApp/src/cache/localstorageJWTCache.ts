import { Emitter } from 'vscode-jsonrpc';

import { IJWTCache, IJWTAsyncCache } from '../typings/IJWTCache';
import { inMemoryCacheFactory } from './inMemoryJWTCache';
import { TokenType } from '../typings/TokenType';
import { localStorageKeyVault } from './localStorageKeyVaultInstance';

import { IKeyVault } from '../interfaces/IKeyVault';

export const inLocalStorageJWTTokenCacheFactory = (keyPrefix?: string)=> {
    const inMemoryCache = inMemoryCacheFactory();

    return new InLocalStorageJWTCache(inMemoryCache, localStorageKeyVault, keyPrefix);
}

interface ITokenChangeEvent {
    name: string;
    token: TokenType | null;
}

export class InLocalStorageJWTCache implements IJWTAsyncCache<TokenType>  {
    private tokenChangeSignal = new Emitter<ITokenChangeEvent>();

    public onTokenChange = this.tokenChangeSignal.event;

    constructor(
        private readonly inMemoryCache: IJWTCache<TokenType>,
        private readonly keyvault: IKeyVault,
        private readonly keyPrefix?: string
    ) {}

    public async cacheToken(name: string, token: TokenType) {
        const keyName = this.getKeyName(name);

        this.inMemoryCache.cacheToken(keyName, token);

        await this.keyvault.set(keyName, JSON.stringify(token));

        this.tokenChangeSignal.fire({
            name,
            token
        });
    }

    public async getCachedToken(name: string, expirationTime: number = 60): Promise<TokenType | undefined> {
        const keyName = this.getKeyName(name);

        const cachedToken = this.inMemoryCache.getCachedToken(keyName, expirationTime);

        if (cachedToken) {
            return cachedToken;
        }

        const tokenStr = await this.keyvault.get(keyName);

        if (!tokenStr) {
            return;
        }

        let token: TokenType | undefined = undefined;
        try {
            token = JSON.parse(tokenStr);

            if (!token) {
                return;
            }
            // parse expiration string to Date object
            token.expiresOn = new Date(token.expiresOn);
        } catch (e) {
            return;
        }

        if (token && token.accessToken && token.expiresOn) {
            this.inMemoryCache.cacheToken(name, token);

            return token;
        }

        return undefined;
    }

    public async deleteCachedToken(name: string) {
        const keyName = this.getKeyName(name);

        this.inMemoryCache.deleteCachedToken(keyName);

        this.keyvault.delete(keyName);

        this.tokenChangeSignal.fire({
            name,
            token: null
        });
    }


    public async clearCache() {
        for (let key of await this.getAllCachedKeys()) {
            await this.keyvault.delete(key);
        }

        this.inMemoryCache.clearCache();
    }

    public async getAllCachedKeys() {
        return this.inMemoryCache.getAllCachedKeys();
    }

    public getKeyName(name: string) {
        if (!this.keyPrefix) {
            return name;
        }

        return `${this.keyPrefix}.${name}`;
    }
}
