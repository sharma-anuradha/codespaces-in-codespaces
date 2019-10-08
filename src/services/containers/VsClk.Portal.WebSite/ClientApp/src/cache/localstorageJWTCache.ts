import { IJWTCache } from '../typings/IJWTCache';
import { InMemoryJWTCache } from './inMemoryJWTCache';
import { TokenType } from '../typings/TokenType';

export const inLocalStorageJWTTokenCacheFactory = ()=> {
    const inMemoryCache = new Map<string, TokenType>();

    return new InLocalStorageJWTCache<TokenType>(inMemoryCache);
}

export class InLocalStorageJWTCache<T> extends InMemoryJWTCache<TokenType> implements IJWTCache<TokenType>  {
    public cacheToken(name: string, token: TokenType) {
        super.cacheToken(name, token);

        localStorage.setItem(name, JSON.stringify(token));

        return this;
    }

    public getCachedToken(name: string, expirationTime: number = 60): TokenType | undefined {
        const cachedToken = super.getCachedToken(name, expirationTime);

        if (cachedToken) {
            return cachedToken;
        }

        const tokenStr = localStorage.getItem(name);

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
        super.deleteCachedToken(name);

        localStorage.removeItem(name);
        
        return this;
    }
}
