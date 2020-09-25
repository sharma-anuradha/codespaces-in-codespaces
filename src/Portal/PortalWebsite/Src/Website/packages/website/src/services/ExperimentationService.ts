import { ExperimentationService, IExperimentationTelemetry, IKeyValueStorage, IExperimentationFilterProvider} from 'tas-client';

export class PortalFilterProvider implements IExperimentationFilterProvider {
    constructor(
        private extensionName: string,
        private extensionVersion: string,
        private targetPopulation: TargetPopulation,
    ) {}

    public getFilterValue(filter: string): string | null {
        switch (filter) {
            case Filters.ApplicationVersion:
                return Filters.ApplicationVersion;
            case Filters.Build:
                return Filters.Build;
            case Filters.ClientId:
                return Filters.ClientId;
            case Filters.ExtensionName:
                return this.extensionName;
            case Filters.ExtensionVersion:
                return this.extensionVersion;
            case Filters.TargetPopulation:
                return this.targetPopulation;
            default:
                return '';
        }
    }

    public getFilters(): Map<string, any> {
        let filters: Map<string, any> = new Map<string, any>();
        let filterValues = Object.values(Filters);
        for (let value of filterValues) {
            filters.set(value, this.getFilterValue(value));
        }

        return filters;
    }
}

/**
 * All available filters, can be updated.
 */
export enum Filters {
    /**
     * The market in which the extension is distributed.
     */
    Market = 'X-MSEdge-Market',

    /**
     * The corporation network.
     */
    CorpNet = 'X-FD-Corpnet',

    /**
     * Version of the application which uses experimentation service.
     */
    ApplicationVersion = 'X-VSCode-AppVersion',

    /**
     * Insiders vs Stable.
     */
    Build = 'X-VSCode-Build',

    /**
     * Client Id which is used as primary unit for the experimentation.
     */
    ClientId = 'X-MSEdge-ClientId',

    /**
     * Extension header.
     */
    ExtensionName = 'X-VSCode-ExtensionName',

    /**
     * The version of the extension.
     */
    ExtensionVersion = 'X-VSCode-ExtensionVersion',

    /**
     * The target population.
     * This is used to separate internal, early preview, GA, etc.
     */
    TargetPopulation = 'X-VSCode-TargetPopulation',
}


export enum TargetPopulation {
    Team = 'team',
    Internal = 'internal',
    Insiders = 'insider',
    Public = 'public',
}

export class DummyExperimentationTelemetry implements IExperimentationTelemetry {
    public postedEvents: { eventName: string, args: any, sharedProperties: Map<string, string> }[] = [];
    public postEvent(eventName: string, args: any): void {
        this.postedEvents.push({ eventName, args, sharedProperties: new Map(this.sharedProperties) });
    }

    public sharedProperties: Map<string, string> = new Map<string, string>();

    public setSharedProperty(name: string, value: string): void {
        this.sharedProperties.set(name, value);
    }
}

export class KeyValueStorageMock implements IKeyValueStorage {
    private storage: Map<string, any> = new Map<string, any>();

    public getValue<T>(key: string, defaultValue?: T): Promise<T | undefined> {
        if (this.storage.has(key)) {
            return Promise.resolve(this.storage.get(key));
        }

        return Promise.resolve(defaultValue ?? undefined);
    }

    public setValue<T>(key: string, value: T): void {
        this.storage.set(key, value);
    }
}

export function getExpService() {
    const dummyFilterProvider: IExperimentationFilterProvider = new PortalFilterProvider(
        "extensionName",
        "extensionVersion",
        TargetPopulation.Public
    );

    let experimentationService = new ExperimentationService({
        filterProviders: [dummyFilterProvider],
        telemetry: new DummyExperimentationTelemetry(),
        keyValueStorage: new KeyValueStorageMock(),
        endpoint: 'https://default.exp-tas.com/vscode/ab',
        featuresTelemetryPropertyName: 'VSCode.ABExp.Features',
        assignmentContextTelemetryPropertyName: 'abexp.assignmentcontext',
        telemetryEventName: 'query-expfeature',
    });
    
    return experimentationService;
}