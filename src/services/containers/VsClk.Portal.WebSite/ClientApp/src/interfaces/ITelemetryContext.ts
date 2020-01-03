export type ITelemetryContext = {
    portalVersion: string;

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
};
