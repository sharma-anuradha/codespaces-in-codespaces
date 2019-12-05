import { VSCodeConfig } from './../constants';
export type ITelemetryContext = {
    portalVersion: string;

    machineId: string;
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
};
