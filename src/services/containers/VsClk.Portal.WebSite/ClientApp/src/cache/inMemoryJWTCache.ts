import { IJWTCache } from '../typings/IJWTCache';
import { TokenType } from '../typings/TokenType';

export const inMemoryCacheFactory = ()=> {
    const inMemoryCache = new Map<string, TokenType>();

    return new InMemoryJWTCache<TokenType>(inMemoryCache);
}

const inMemoryCacheSymbol = Symbol();

export class InMemoryJWTCache<T> implements IJWTCache<TokenType> {
    private [inMemoryCacheSymbol]: Map<string, TokenType>;

    constructor(inMemoryCache: Map<string, TokenType>) {
        this[inMemoryCacheSymbol] = new Map<string, TokenType>(inMemoryCache.entries());
    }

    public cacheToken(name: string, token: TokenType): IJWTCache<TokenType> {
        this[inMemoryCacheSymbol].set(name, token);

        return this;
    }

    public getCachedToken(name: string, expirationTime: number = 60): TokenType | undefined {
        const cachedToken = this[inMemoryCacheSymbol].get(name);

        if (cachedToken) {
            const tokenExpirationTime = ((Date.now() - cachedToken.expiresOn.getTime()) / 1000);
            if (tokenExpirationTime > expirationTime) {
                return cachedToken;
            }
        }
    }

    public deleteCachedToken(name: string): IJWTCache<TokenType> {
        this[inMemoryCacheSymbol].delete(name);
        
        return this;
    }

    public getAllCachedKeys(): string[] {
        return [...this[inMemoryCacheSymbol].keys()];
    }

    public clearCache(): IJWTCache<TokenType> {
        for (let key of this.getAllCachedKeys()) {
            this.deleteCachedToken(key);
        }
        
        return this;
    }
}
