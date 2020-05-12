export interface IJWTCache<T> {
    /**
     * Calculate key name.
     */
    getKeyName(name: string): string;
    
    /**
     * Method to cache a JWT token.
     * @param name Token name.
     * @param expiration Min valid expiration time in seconds.
     */
    cacheToken(name: string, token: string | T): IJWTCache<T>;

    /**
     * Method to retrieve cached JWT token.
     * @param name Token name.
     * @param expiration Min valid expiration time in seconds.
     */
    getCachedToken(name: string, expiration: number): T | undefined;

    /**
     * Method to delete the cached token.
     * @param name Token name.
     */
    deleteCachedToken(name: string): IJWTCache<T>;

    /**
     * Method to clear the cache.
     */
    clearCache(): IJWTCache<T>;

    /**
     * Method to get all cached keys.
     */
    getAllCachedKeys(): string[];
}


export interface IJWTAsyncCache<T> {
    /**
     * Method to cache a JWT token.
     * @param name Token name.
     * @param expiration Min valid expiration time in seconds.
     */
    cacheToken(name: string, token: string | T): Promise<void>;

    /**
     * Method to retrieve cached JWT token.
     * @param name Token name.
     * @param expiration Min valid expiration time in seconds.
     */
    getCachedToken(name: string, expiration: number): Promise<T | undefined>;

    /**
     * Method to delete the cached token.
     * @param name Token name.
     */
    deleteCachedToken(name: string): Promise<void>;

    /**
     * Method to clear the cache.
     */
    clearCache(): Promise<void>;

    /**
     * Method to get all cached keys.
     */
    getAllCachedKeys(): Promise<string[]>;
}