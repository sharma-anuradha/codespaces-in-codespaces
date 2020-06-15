export type GitCredentialsRequest = {
    /**
     * The protocol over which the credential will be used (e.g., https).
     */
    protocol?: string;

    /**
     * The credential host.
     */
    host?: string;

    /**
     * The credential path.
     */
    path?: string;

    /**
     * The credential’s username, if we already have one (e.g., from a URL, from the user, or from a previously run helper).
     */
    username?: string;

    /**
     * The credential’s password, if we are asking it to be stored.
     */
    password?: string;

    /**
     * The credential url.
     */
    url?: string;
};
