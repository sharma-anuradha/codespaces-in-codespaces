/** The document representation of the Rollout Policy. */
export interface IRolloutPolicy {
    /** The version of the schema that a document conforms to. */
    contentVersion: string;
    /** The name to identify the policy. */
    name: string;
    /** The version of the rollout policy. */
    version: string;
    /** Defines the safe rollout policies. */
    safeRollout: ISafeRollout;
    /** Defines the artifact source policy. */
    artifactSource: IArtifactSource;
    /** Defines the policy that prevents creating rollouts and pausing running rollouts for an approved period of time. */
    noRollout: INoRollout;
    /** Defines the auto restart policy. */
    autoRestart: IAutoRestart;
}

export interface ISafeRollout {
    /** Defines the region pairs for the service. */
    regionPairs: IRegionPairs[];
    /** Defines the stages that the rollout should conform to. */
    stages: IStages[];
}

export interface IArtifactSource {
    /** The disallowed artifact source types for the rollout, list of types: SmbShare, AzureStorage */
    disallowedTypes: string[];
}

export interface INoRollout {
    /** The time periods (UTC) for of No-Rollout policy */
    noRolloutZones: INoRolloutZones[];
}

export interface IAutoRestart {
    /** The maximum number of restart attempts for the rollout. */
    maxRestartAttempts: number;
    /** Flag indicating whether actions that succeeded in previous rollout should be skipped. */
    skipSucceeded: boolean;
    /** The wait duration after which the rollout would be restarted in case of failure. */
    waitDurationAfterFailure: string;
    /** Error conditons on which restart should be triggered. If not defined, rollout would be restarted on any error encountered. */
    restartOnErrorConditions: IRestartOnErrorConditions;
}

export interface IRegionPairs {
    /** The name of the primary region. */
    primary: string;
    /** The name of the secondary region. */
    secondary: string;
}

export interface IStages {
    /** The stage number. */
    number: number;
    /** A human-readable stage name. */
    name: string;
    /** The maximum number of regions that can be deployed to in parallel in this stage. */
    maxDeployableRegions: number;
    /** The regions that can be deployed to in this stage. */
    allowedRegions: string[];
    /** The regions that cannot be deployed to in this stage. */
    disallowedRegions: string[];
    /** The time to wait before starting the dependent stage in ISO8601 format. */
    waitAfterCompletion: string;
}

export interface INoRolloutZones {
    /** The event name that refers to the No-rollout period. */
    name: string;
    /** (default = now) The starting date and time (UTC) of the range of the No-rollout Zone [yyyy-mm-ddThh:mm:ss] */
    startDateTime: string;
    /** (default = infinity) The ending date and time (UTC) of the range of the No-rollout Zone [yyyy-mm-ddThh:mm:ss] */
    endDateTime: string;
}

export interface IRestartOnErrorConditions {
    /** The list of error conditions on which rollout should be restarted. */
    errorsContainAny: string;
}