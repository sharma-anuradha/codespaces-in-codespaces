/// <reference path="Html5.d.ts" />
/// <reference path="require.d.ts" />

interface Window {
    readonly caches: CacheStorage;
    readonly crypto: Crypto;
    readonly indexedDB: IDBFactory;
    readonly origin: string;
    readonly performance: Performance;
    atob(data: string): string;
    btoa(data: string): string;
    clearInterval(handle?: number): void;
    clearTimeout(handle?: number): void;
    createImageBitmap(image: ImageBitmapSource): Promise<ImageBitmap>;
    createImageBitmap(image: ImageBitmapSource, sx: number, sy: number, sw: number, sh: number): Promise<ImageBitmap>;
    fetch(input: RequestInfo, init?: RequestInit): Promise<Response>;
    queueMicrotask(callback: Function): void;
    setInterval(handler: TimerHandler, timeout?: number, ...arguments: any[]): number;
    setTimeout(handler: TimerHandler, timeout?: number, ...arguments: any[]): number;
}

declare var window: Window & typeof globalThis;

interface Action {
    (): void;
}

interface Action1<T> {
    (arg: T): void;
}

interface Action2<T1, T2> {
    (arg1: T1, arg2: T2): void;
}

interface Action3<T1, T2, T3> {
    (arg1: T1, arg2: T2, arg3: T3): void;
}

interface Action4<T1, T2, T3, T4> {
    (arg1: T1, arg2: T2, arg3: T3, arg4: T4): void;
}

interface Func<R> {
    (): R;
}

interface Func1<T, R> {
    (arg: T): R;
}

interface Func2<T1, T2, R> {
    (arg1: T1, arg2: T2): R;
}

type Primitive = number | string | Date | boolean;

interface StringMapPrimitive extends StringMap<StringMapRecursive | Primitive | Array<Primitive | StringMapRecursive>> { }

type StringMapRecursive = StringMapPrimitive;

// Intended to break compilation
/**
 * Obsolete
 */
interface Obsolete {
    /**
     * Obsolete
     */
    Obsolete: "true";
}

/**
 * Configuration options for tracing onInputsSet calls.
 */
interface InputsSetTraceConfig {
    /**
     * Partial name of target composition or view model to log when onInputsSet gets called.
     */
    log?: StringMap<boolean>;

    /**
     * Partial name of target composition or view model to break into debugger before calling onInputsSet.
     */
    debug?: StringMap<boolean>;

    /**
     * Partial name of target compositon or view model to log verbose statements for each evaluation that
     *  checks if onInputsSet should be called.
     */
    verbose?: StringMap<boolean>;
}

interface TraceConfig {
    assert?: boolean;
    diagnostics?: boolean;
    po?: boolean;
    rpc?: boolean;
    novirt?: boolean;
    debuglog?: boolean;
    debugtests?: boolean;
    lifetime?: boolean;
    nocallstacks?: boolean;
    desktop?: boolean;
    router?: boolean;
    bladerebind?: boolean;
    extensionmanager?: boolean;
    partsettings?: StringMap<boolean>;
    lenssettings?: StringMap<boolean>;
    poarraymutation?: boolean;
    poarraymutation2?: boolean;
    potrackobservable?: boolean;

    /**
     * Configures onInputsSet traces for extension frames.
     */
    inputsset?: InputsSetTraceConfig;

    /**
     * Configures onInputsSet traces for shell's frame.
     */
    shellinputsset?: InputsSetTraceConfig;

    /**
     * Configures traces for action bar validation.
     */
    actionbar?: boolean;
}

interface FeatureConfig {

    /**
     * A value indicating whether or not the user can modify extension metadata.
     */
    canmodifyextensions?: string;

    /**
     * If true, scripts will be loaded such that the unhandled-error handler can obtain details also for scripts from another origin.
     */
    crossorigintraces?: string;

    /**
     * A value indicating whether to show monitoring group in resource menu or not.
     */
    enablemonitoringgroup?: string;

    /**
     * If true, Quotas blade will be opened from Settings blade.
     */
    showquotas?: string;

    /**
     * If true, will emit telemetry to track the time for fetchData to complete
     */
    tracefetch?: string;

    /**
     * A value including which verbose diagnostics information to include separated by commas or 'all' for all the verbose diagnostics information
     */
    verbosediagnostics?: string;

    /**
     * A value indicating whether monitor chart control is available to use or not. This flag is targeted for extensions to consume.
     */
    enablemonitorchartcontrol?: string;

    /**
     * A value indicating whether monitor chart part to use app insights blade or not.
     */
    enableappinsightsmetricsblade?: string;

    [key: string]: string;
}

type UnsafeCastForTypeScriptMigration = any;

/**
 * Values here are generated in CommonEnvironmentProvider.cs, these are values generated per extension
 * These values are shipped to partners.
 * Anything here should be accessed by MsPortalFx.getEnvironmentValue("keyname"). The value returned will be typed correctly.
 */
interface FxEnvironment {
    applicationPath: string;
    extensionName: string;
    pageVersion: string;
    sdkVersion: string;
    trustedParentOrigin: string;
    useFxArmEndpoint: boolean;
    version: string;
    trustedDomains: string[];
}

interface BootstrapTelemetry {
    ossFxScriptsBegin: number;
    ossFxScriptsEnd: number;
    fxScriptsBegin: number;
    fxScriptsEnd: number;
    manifestScriptDownloadEnd: number;
    initScriptDownloadBegin: number;
    initScriptDownloadEnd: number;
    navigationTime: number;
    appCacheLoad: number;
    domainLookup: number;
    requestTime: number;
    ttfb: number;
    pageLoad: number;
    unloadTime: number;
    performanceTelemetry: PerformanceTelemetry;
    timing: string;
}

interface PerformanceTelemetry {
    readonly totalBytesTransferred: number;
    readonly totalBytesTransferredByGroup: StringMap<number>;
    readonly uncachedCountByGroup: StringMap<number>;
    readonly cachedCountByGroup: StringMap<number>;
    readonly homepageTransferBytes: number;
    readonly connection: any;
    readonly hardwareConcurrency: number;
    readonly totalTransferMs: number;
    readonly totalTransferMsByGroup: StringMap<number>;
}

interface FxStatic {
    bootstrapTelemetry: BootstrapTelemetry;
    injectCss(module: { id: string }, content: string): void;
    getTimestamp(): number;
}

type DeepReadonly<T> = {
    readonly [P in keyof T]: DeepReadonly<T[P]>;
}

declare const enum ExtensionNames {
    HubsExtension = "HubsExtension",
    Microsoft_Azure_AD = "Microsoft_Azure_AD",
    Microsoft_Azure_Billing = "Microsoft_Azure_Billing",
    Microsoft_Azure_Insights = "Microsoft_Azure_Insights",
    Microsoft_Azure_Marketplace = "Microsoft_Azure_Marketplace",
    Microsoft_Azure_Monitoring = "Microsoft_Azure_Monitoring",
    Microsoft_Azure_Resources = "Microsoft_Azure_Resources",
    Microsoft_Azure_Support = "Microsoft_Azure_Support",
    Microsoft_Health_Admin = "Microsoft_Health_Admin",
    WebsitesExtension = "WebsitesExtension",
}

interface Window {
    fx: FxStatic;
}

interface DependencyInjectionScope {
    "viewModel": undefined;
}

interface ModuleMap {
}

////////////////////////////////////////
/// BEGIN: IE11 ECMAScript Extensions
////////////////////////////////////////

interface CollatorOptions extends Intl.CollatorOptions {
}

interface MapBase<K, V> {
    delete(key: K): boolean;
    get(key: K): V;
    has(key: K): boolean;
    set(key: K, value: V): this;
    clear(): void;
    forEach(callbackfn: (value: V, key: K, map: MapBase<K, V>) => void, thisArg?: any): void;
}

interface Map<K, V> extends MapBase<K, V> {
    readonly size: number;
}

interface WeakMap<K extends object, V> {
    delete(key: K): boolean;
    get(key: K): V;
    has(key: K): boolean;
    set(key: K, value: V): void;
}

////////////////////////////////////////
/// END: IE11 ECMAScript Extensions
////////////////////////////////////////