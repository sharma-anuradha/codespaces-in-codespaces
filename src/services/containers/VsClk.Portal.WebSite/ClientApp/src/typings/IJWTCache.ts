export interface IJWTCache<T> {
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
