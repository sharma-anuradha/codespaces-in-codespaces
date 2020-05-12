import { TelemetryPropertyValue } from "./ITelemetryEvent";

export interface ITelemetryContext {
    browserId: string;
    sessionId: string;
    pageLoadId: string;

    host: string;

    browserName: string;
    browserVersion: string;
    browserOS: string;

    environmentId?: string;
    isInternal?: boolean;

    vscodeCommit: string;
    vscodeQuality: string;
    
    [key: string]: TelemetryPropertyValue;
};
