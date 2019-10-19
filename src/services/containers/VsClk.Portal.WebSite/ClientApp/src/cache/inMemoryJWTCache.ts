import { IJWTCache } from '../typings/IJWTCache';
import { TokenType } from '../typings/TokenType';

export const inMemoryCacheFactory = ()=> {
    const inMemoryCache = new Map<string, TokenType>();

    return new InMemoryJWTCache<TokenType>(inMemoryCache);
}

const inMemoryCacheSymbol = Symbol();

const inMemoryPrefixSymbol = Symbol();

export class InMemoryJWTCache<T> implements IJWTCache<TokenType> {
    private [inMemoryCacheSymbol]: Map<string, TokenType>;
    private [inMemoryPrefixSymbol]: string | undefined;

    constructor(inMemoryCache: Map<string, TokenType>, keyPrefix?: string) {
        this[inMemoryCacheSymbol] = new Map<string, TokenType>(inMemoryCache.entries());
        this[inMemoryPrefixSymbol] = keyPrefix;
    }

    protected getKeyName(name: string) {
        if (!this[inMemoryPrefixSymbol]) {
            return name;
        }

        return `${this[inMemoryPrefixSymbol]}_${name}`;
    }

    public cacheToken(name: string, token: TokenType): IJWTCache<TokenType> {
        this[inMemoryCacheSymbol].set(this.getKeyName(name), token);

        return this;
    }

    public getCachedToken(name: string, expirationTime: number = 60): TokenType | undefined {
        const cachedToken = this[inMemoryCacheSymbol].get(this.getKeyName(name));

        if (cachedToken) {
            const tokenExpirationTime = ((Date.now() - cachedToken.expiresOn.getTime()) / 1000);
            if (tokenExpirationTime > expirationTime) {
                return cachedToken;
            }
        }
    }

    public deleteCachedToken(name: string): IJWTCache<TokenType> {
        this[inMemoryCacheSymbol].delete(this.getKeyName(name));
        
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
