import { IJWTCache } from '../typings/IJWTCache';
import { InMemoryJWTCache } from './inMemoryJWTCache';
import { TokenType } from '../typings/TokenType';

export const inLocalStorageJWTTokenCacheFactory = (keyPrefix?: string)=> {
    const inMemoryCache = new Map<string, TokenType>();

    return new InLocalStorageJWTCache<TokenType>(inMemoryCache, keyPrefix);
}

export class InLocalStorageJWTCache<T> extends InMemoryJWTCache<TokenType> implements IJWTCache<TokenType>  {
    public cacheToken(name: string, token: TokenType) {
        const keyName = this.getKeyName(name);

        super.cacheToken(keyName, token);

        localStorage.setItem(keyName, JSON.stringify(token));

        return this;
    }

    public getCachedToken(name: string, expirationTime: number = 60): TokenType | undefined {
        const keyName = this.getKeyName(name);

        const cachedToken = super.getCachedToken(keyName, expirationTime);

        if (cachedToken) {
            return cachedToken;
        }

        const tokenStr = localStorage.getItem(keyName);

        if (!tokenStr) {
            return;
        }

        let token: TokenType | undefined = undefined;
        try {
            token = JSON.parse(tokenStr);
        } catch (e) {
            return;
        }

        if (token && token.accessToken && token.expiresOn) {
            return token;
        }

        // unknown shape, explicitelly return `undefined`
        return undefined;
    }

    public deleteCachedToken(name: string): IJWTCache<TokenType> {
        const keyName = this.getKeyName(name);

        super.deleteCachedToken(keyName);

        localStorage.removeItem(keyName);
        
        return this;
    }
}
