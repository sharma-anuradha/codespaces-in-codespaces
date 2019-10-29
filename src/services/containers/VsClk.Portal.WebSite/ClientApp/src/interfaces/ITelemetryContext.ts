export type ITelemetryContext = {
    sessionId: string;
    pageLoadId: string;

    host: string;
    path: string;

    browserName: string;
    browserVersion: string;
    browserOS: string;

    environmentId?: string;
    isInternal?: boolean;
};