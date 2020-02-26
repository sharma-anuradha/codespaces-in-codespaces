declare module 'vscode-web' {
    import * as vscodeTypes from 'vscode-jsonrpc';

    export interface IWebSocket {
        readonly onData: vscodeTypes.Event<ArrayBuffer>;
        readonly onOpen: vscodeTypes.Event<void>;
        readonly onClose: vscodeTypes.Event<void>;
        readonly onError: vscodeTypes.Event<any>;

        send(data: ArrayBuffer | ArrayBufferView): void;
        close(): void;
    }

    export interface IWebSocketFactory {
        create(url: string): IWebSocket;
    }

    export interface IUpdate {
        version: string;
    }

    export interface IUpdateProvider {
        /**
         * Should return with the `IUpdate` object if an update is
         * available or `null` otherwise to signal that there are
         * no updates.
         */
        checkForUpdate(): Promise<IUpdate | null>;
    }

    export enum FileType {
        Unknown = 0,
        File = 1,
        Directory = 2,
        SymbolicLink = 64,
    }

    export interface IStat {
        type: FileType;
        mtime: number;
        ctime: number;
        size: number;
    }

    export interface FileOverwriteOptions {
        overwrite: boolean;
    }

    export interface FileWriteOptions {
        overwrite: boolean;
        create: boolean;
    }

    export interface FileOpenOptions {
        create: boolean;
    }

    export interface FileDeleteOptions {
        recursive: boolean;
        useTrash: boolean;
    }

    export interface IWatchOptions {
        recursive: boolean;
        excludes: string[];
    }

    export const enum FileSystemProviderCapabilities {
        FileReadWrite = 1 << 1,
        FileOpenReadWriteClose = 1 << 2,
        FileFolderCopy = 1 << 3,

        PathCaseSensitive = 1 << 10,
        Readonly = 1 << 11,

        Trash = 1 << 12,
    }

    /**
     * Identifies a single change in a file.
     */
    export interface IFileChange {
        /**
         * The type of change that occurred to the file.
         */
        type: FileChangeType;

        /**
         * The unified resource identifier of the file that changed.
         */
        resource: URI;
    }

    export interface IFileSystemProvider {
        readonly capabilities: FileSystemProviderCapabilities;
        readonly onDidChangeCapabilities: vscodeTypes.Event<void>;

        readonly onDidErrorOccur?: vscodeTypes.Event<string>; // TODO@ben remove once file watchers are solid

        readonly onDidChangeFile: vscodeTypes.Event<IFileChange[]>;
        watch(resource: URI, opts: IWatchOptions): vscodeTypes.Disposable;

        stat(resource: URI): Promise<IStat>;
        mkdir(resource: URI): Promise<void>;
        readdir(resource: URI): Promise<[string, FileType][]>;
        delete(resource: URI, opts: FileDeleteOptions): Promise<void>;

        rename(from: URI, to: URI, opts: FileOverwriteOptions): Promise<void>;
        copy?(from: URI, to: URI, opts: FileOverwriteOptions): Promise<void>;

        readFile?(resource: URI): Promise<Uint8Array>;
        writeFile?(resource: URI, content: Uint8Array, opts: FileWriteOptions): Promise<void>;

        open?(resource: URI, opts: FileOpenOptions): Promise<number>;
        close?(fd: number): Promise<void>;
        read?(
            fd: number,
            pos: number,
            data: Uint8Array,
            offset: number,
            length: number
        ): Promise<number>;
        write?(
            fd: number,
            pos: number,
            data: Uint8Array,
            offset: number,
            length: number
        ): Promise<number>;
    }

    export interface ICommand {
        command: string;
        title: string;
        category?: string;
    }

    export interface IConfigurationProperty {
        description: string;
        type: string | string[];
        default?: any;
    }

    export interface IConfiguration {
        properties: { [key: string]: IConfigurationProperty };
    }

    export interface IDebugger {
        label?: string;
        type: string;
        runtime?: string;
    }

    export interface IGrammar {
        language: string;
    }

    export interface IJSONValidation {
        fileMatch: string;
        url: string;
    }

    export interface IKeyBinding {
        command: string;
        key: string;
        when?: string;
        mac?: string;
        linux?: string;
        win?: string;
    }

    export interface ILanguage {
        id: string;
        extensions: string[];
        aliases: string[];
    }

    export interface IMenu {
        command: string;
        alt?: string;
        when?: string;
        group?: string;
    }

    export interface ISnippet {
        language: string;
    }

    export interface ITheme {
        label: string;
    }

    export interface IViewContainer {
        id: string;
        title: string;
    }

    export interface IView {
        id: string;
        name: string;
    }

    export interface IColor {
        id: string;
        description: string;
        defaults: { light: string; dark: string; highContrast: string };
    }

    export interface ITranslation {
        id: string;
        path: string;
    }

    export interface ILocalization {
        languageId: string;
        languageName?: string;
        localizedLanguageName?: string;
        translations: ITranslation[];
        minimalTranslations?: { [key: string]: string };
    }

    export interface IExtensionContributions {
        commands?: ICommand[];
        configuration?: IConfiguration | IConfiguration[];
        debuggers?: IDebugger[];
        grammars?: IGrammar[];
        jsonValidation?: IJSONValidation[];
        keybindings?: IKeyBinding[];
        languages?: ILanguage[];
        menus?: { [context: string]: IMenu[] };
        snippets?: ISnippet[];
        themes?: ITheme[];
        iconThemes?: ITheme[];
        viewsContainers?: { [location: string]: IViewContainer[] };
        views?: { [location: string]: IView[] };
        colors?: IColor[];
        localizations?: ILocalization[];
    }

    export type ExtensionKind = 'ui' | 'workspace' | 'web';

    export interface IExtensionManifest {
        readonly name: string;
        readonly displayName?: string;
        readonly publisher: string;
        readonly version: string;
        readonly engines: { vscode: string };
        readonly description?: string;
        readonly main?: string;
        readonly icon?: string;
        readonly categories?: string[];
        readonly keywords?: string[];
        readonly activationEvents?: string[];
        readonly extensionDependencies?: string[];
        readonly extensionPack?: string[];
        readonly extensionKind?: ExtensionKind;
        readonly contributes?: IExtensionContributions;
        readonly repository?: { url: string };
        readonly bugs?: { url: string };
        readonly enableProposedApi?: boolean;
        readonly api?: string;
        readonly scripts?: { [key: string]: string };
    }

    export interface ICredentialsProvider {
        getPassword(service: string, account: string): Promise<string | null>;
        setPassword(service: string, account: string, password: string): Promise<void>;
        deletePassword(service: string, account: string): Promise<boolean>;
        findPassword(service: string): Promise<string | null>;
        findCredentials(service: string): Promise<{ account: string; password: string }[]>;
    }

    export interface IURLCallbackProvider {
        /**
         * Indicates that a Uri has been opened outside of VSCode. The Uri
         * will be forwarded to all installed Uri handlers in the system.
         */
        readonly onCallback: vscodeTypes.Event<URI>;

        /**
         * Creates a Uri that - if opened in a browser - must result in
         * the `onCallback` to fire.
         *
         * The optional `Partial<URI>` must be properly restored for
         * the Uri passed to the `onCallback` handler.
         *
         * For example: if a Uri is to be created with `scheme:"vscode"`,
         * `authority:"foo"` and `path:"bar"` the `onCallback` should fire
         * with a Uri `vscode://foo/bar`.
         *
         * If there are additional `query` values in the Uri, they should
         * be added to the list of provided `query` arguments from the
         * `Partial<URI>`.
         */
        create(options?: Partial<URI>): URI;
    }

    export enum LogLevel {
        Trace,
        Debug,
        Info,
        Warning,
        Error,
        Critical,
        Off,
    }

    export interface UriComponents {
        scheme: string;
        authority: string;
        path: string;
        query: string;
        fragment: string;
    }

    export interface URIInstance {
        /**
         * scheme is the 'http' part of 'http://www.msft.com/some/path?query#fragment'.
         * The part before the first colon.
         */
        readonly scheme: string;

        /**
         * authority is the 'www.msft.com' part of 'http://www.msft.com/some/path?query#fragment'.
         * The part between the first double slashes and the next slash.
         */
        readonly authority: string;

        /**
         * path is the '/some/path' part of 'http://www.msft.com/some/path?query#fragment'.
         */
        readonly path: string;

        /**
         * query is the 'query' part of 'http://www.msft.com/some/path?query#fragment'.
         */
        readonly query: string;

        /**
         * fragment is the 'fragment' part of 'http://www.msft.com/some/path?query#fragment'.
         */
        readonly fragment: string;

        // ---- modify to new -------------------------

        with(change: {
            scheme?: string;
            authority?: string | null;
            path?: string | null;
            query?: string | null;
            fragment?: string | null;
        }): URI;

        toJSON(): UriComponents;

        revive(data: UriComponents | URI): URI;
        revive(data: UriComponents | URI | undefined): URI | undefined;
        revive(data: UriComponents | URI | null): URI | null;
        revive(data: UriComponents | URI | undefined | null): URI | undefined | null;
        revive(data: UriComponents | URI | undefined | null): URI | undefined | null;

        // ---- filesystem path -----------------------

        /**
         * Returns a string representing the corresponding file system path of this URI.
         * Will handle UNC paths, normalizes windows drive letters to lower-case, and uses the
         * platform specific path separator.
         *
         * * Will *not* validate the path for invalid characters and semantics.
         * * Will *not* look at the scheme of this URI.
         * * The result shall *not* be used for display purposes but for accessing a file on disk.
         *
         *
         * The *difference* to `URI#path` is the use of the platform specific separator and the handling
         * of UNC paths. See the below sample of a file-uri with an authority (UNC path).
         *
         * ```ts
            const u = URI.parse('file://server/c$/folder/file.txt')
            u.authority === 'server'
            u.path === '/shares/c$/file.txt'
            u.fsPath === '\\server\c$\folder\file.txt'
        ```
         *
         * Using `URI#path` to read a file (using fs-apis) would not be enough because parts of the path,
         * namely the server name, would be missing. Therefore `URI#fsPath` exists - it's sugar to ease working
         * with URIs that represent files on disk (`file` scheme).
         */
        fsPath(): string;

        // ---- printing/externalize ---------------------------

        /**
         * Creates a string representation for this URI. It's guaranteed that calling
         * `URI.parse` with the result of this function creates an URI which is equal
         * to this URI.
         *
         * * The result shall *not* be used for display purposes but for externalization or transport.
         * * The result will be encoded using the percentage encoding and encoding happens mostly
         * ignore the scheme-specific encoding rules.
         *
         * @param skipEncoding Do not encode the result, default is `false`
         */
        toString(skipEncoding?: boolean): string;
    }

    export interface URI extends UriComponents {
        /**
         * @internal
         */
        new(
            scheme: string,
            authority?: string,
            path?: string,
            query?: string,
            fragment?: string,
            _strict?: boolean
        ): IURI;

        /**
         * @internal
         */
        new(components: UriComponents): IURI;

        /**
         * @internal
         */
        new(
            schemeOrData: string | UriComponents,
            authority?: string,
            path?: string,
            query?: string,
            fragment?: string,
            _strict?: boolean
        ): IURI;

        static isUri(thing: any): thing is URI;

        // ---- parse & validate ------------------------

        /**
         * Creates a new URI from a string, e.g. `http://www.msft.com/some/path`,
         * `file:///usr/home`, or `scheme:with/path`.
         *
         * @param value A string which represents an URI (see `URI#toString`).
         */
        static parse(value: string, _strict?: boolean): URI;

        /**
         * Creates a new URI from a file system path, e.g. `c:\my\files`,
         * `/usr/home`, or `\\server\share\some\path`.
         *
         * The *difference* between `URI#parse` and `URI#file` is that the latter treats the argument
         * as path, not as stringified-uri. E.g. `URI.file(path)` is **not the same as**
         * `URI.parse('file://' + path)` because the path might contain characters that are
         * interpreted (# and ?). See the following sample:
         * ```ts
        const good = URI.file('/coding/c#/project1');
        good.scheme === 'file';
        good.path === '/coding/c#/project1';
        good.fragment === '';
        const bad = URI.parse('file://' + '/coding/c#/project1');
        bad.scheme === 'file';
        bad.path === '/coding/c'; // path is now broken
        bad.fragment === '/project1';
        ```
         *
         * @param path A file system path (see `URI#fsPath`)
         */
        static file(path: string): URI;

        static from(components: {
            scheme: string;
            authority?: string;
            path?: string;
            query?: string;
            fragment?: string;
        }): URI;
    }

    /**
     * A workspace to open in the workbench can either be:
     * - a workspace file with 0-N folders (via `workspaceUri`)
     * - a single folder (via `folderUri`)
     * - empty (via `undefined`)
     */
    export type IWorkspace = { workspaceUri: URI } | { folderUri: URI } | undefined;

    export interface IWorkspaceProvider {
        /**
         * The initial workspace to open.
         */
        readonly workspace: IWorkspace;

        /**
         * Asks to open a workspace in the current or a new window.
         *
         * @param workspace the workspace to open.
         * @param options wether to open inside the current window or a new window.
         */
        open(workspace: IWorkspace, options?: { reuse?: boolean }): Promise<void>;
    }

    interface IApplicationLink {

        /**
         * A link that is opened in the OS. If you want to open VSCode it must
         * follow our expected structure of links:
         *
         * <vscode|vscode-insiders>://<file|vscode-remote>/<remote-authority>/<path>
         *
         * For example:
         *
         * vscode://vscode-remote/vsonline+2005711d/home/vsonline/workspace for
         * a remote folder in VSO or vscode://file/home/workspace for a local folder.
         */
        uri: URI;

        /**
         * A label for the application link to display.
         */
        label: string;
    }

    interface IHostCommand {

        /**
         * An identifier for the command. Commands can be executed from extensions
         * using the `vscode.commands.executeCommand` API using that command ID.
         */
        id: string,

        /**
         * A function that is being executed with any arguments passed over.
         */
        handler: (...args: any[]) => void;
    }

    export interface IWorkbenchConstructionOptions {
        /**
         * Experimental: the remote authority is the IP:PORT from where the workbench is served
         * from. It is for example being used for the websocket connections as address.
         */
        remoteAuthority?: string;

        /**
         * The connection token to send to the server.
         */
        connectionToken?: string;

        /**
         * Experimental: An endpoint to serve iframe content ("webview") from. This is required
         * to provide full security isolation from the workbench host.
         */
        webviewEndpoint?: string;

        /**
         * Experimental: a handler for opening workspaces and providing the initial workspace.
         */
        workspaceProvider?: IWorkspaceProvider;

        /**
         * Experimental: An optional folder that is set as workspace context for the workbench.
         */
        folderUri?: URI;

        /**
         * Experimental: An optional workspace that is set as workspace context for the workbench.
         */
        workspaceUri?: URI;

        /**
         * Experimental: The userDataProvider is used to handle user specific application
         * state like settings, keybindings, UI state (e.g. opened editors) and snippets.
         */
        userDataProvider?: IFileSystemProvider;

        /**
         * A factory for web sockets.
         */
        webSocketFactory?: IWebSocketFactory;

        /**
         * A provider for resource URIs.
         */
        resourceUriProvider?: (uri: URI) => URI;

        /**
         * Experimental: Whether to enable the smoke test driver.
         */
        driver?: boolean;

        /**
         * Experimental: The credentials provider to store and retrieve secrets.
         */
        credentialsProvider?: ICredentialsProvider;

        /**
         * Experimental: Add static extensions that cannot be uninstalled but only be disabled.
         */
        staticExtensions?: { packageJSON: IExtensionManifest; extensionLocation: URI }[];

        /**
         * Experimental: Support for URL callbacks.
         */
        urlCallbackProvider?: IURLCallbackProvider;

        /**
         * Current logging level. Default is `LogLevel.Info`.
         */
        logLevel?: LogLevel;

        /**
         * Experimental: Support for update reporting.
         */
        updateProvider?: IUpdateProvider;

        /**
         * Experimental: Support adding additional properties to telemetry.
         */
        resolveCommonTelemetryProperties?: () => { [key: string]: any };

        /**
         * Experimental: Resolves an external uri before it is opened.
         */
        readonly resolveExternalUri?: (uri: URI) => Promise<URI>;

        /**
         * Provide entries for the "Open in Desktop" feature.
         *
         * Depending on the returned elements the behaviour is:
         * - no elements: there will not be a "Open in Desktop" affordance
         * - 1 element: there will be a "Open in Desktop" affordance that opens on click
         *   and it will use the label provided by the link
         * - N elements: there will be a "Open in Desktop" affordance that opens
         *   a picker on click to select which application to open.
         */
        readonly applicationLinks?: readonly IApplicationLink[];

        /**
         * A set of optional commands that should be registered with the commands
         * registry.
         *
         * Note: commands can be called from extensions if the identifier is known!
         */
        readonly commands?: readonly IHostCommand[];
    }

    export interface IWorkbench {
        URI: URI;
        Disposable: vscodeTypes.Disposable;
        Event: vscodeTypes.Event<any>;
        LogLevel: LogLevel;
        FileType: FileType;
        create: (el: HTMLElement, options: IWorkbenchConstructionOptions) => void;
    }
}
