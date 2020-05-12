import { ApplicationInsights } from '@microsoft/applicationinsights-web';
import { detect } from 'detect-browser';

export const TELEMETRY_KEY = 'AIF-d9b70cd4-b9f9-4d70-929b-a071c400b217';

import { ITelemetryEvent, TelemetryPropertyValue } from "../interfaces/ITelemetryEvent";

import { ITelemetryContext } from '../interfaces/ITelemetryContext';
import { randomString } from '../utils/randomString';
import { createTrace } from '../utils/createTrace';
import { TelemetryEventSerializer } from './TelemetryEventSerializer';
import { prefixPropertyNames } from './prefixPropertyNames';
import { ICustomTelemetryContextProperties } from '../interfaces/ICustomTelemetryContextProperties';

let sequenceNumber = 0;

type MatchFunction = (pathname: string) => { path: string; } | null;

export class TelemetryService {
    private appInsights: ApplicationInsights;
    private context!: ITelemetryContext;
    private logger: ReturnType<typeof createTrace>;
    private telemetryEventSerializer: TelemetryEventSerializer;
    private matchPath?: MatchFunction;

    constructor(contextProps: ICustomTelemetryContextProperties) {
        this.logger = createTrace('TelemetryService');
        this.telemetryEventSerializer = new TelemetryEventSerializer();
        this.appInsights = new ApplicationInsights({
            config: {
                instrumentationKey: TELEMETRY_KEY,
                endpointUrl: 'https://vortex.data.microsoft.com/collect/v1',
                emitLineDelimitedJson: true,
                autoTrackPageVisitTime: false,
                disableExceptionTracking: true,
                disableAjaxTracking: true,
                isCookieUseDisabled: true,
            },
        });
        this.appInsights.loadAppInsights();
        this.initializeContext(contextProps);
    }

    initializeTelemetry(matchPath: MatchFunction) {
        this.matchPath = matchPath;
    }

    initializeContext(contextProps: ICustomTelemetryContextProperties) {
        const info = detect();

        this.context = {
            // portalVersion: versionFile.version,
            browserId: this.browserId,
            sessionId: this.getSessionId(),
            pageLoadId: this.getPageLoadId(),
            host: location.host,
            browserName: (info && info.name) || '<unknown>',
            browserVersion: (info && info.version) || '<unknown>',
            browserOS: (info && info.os) || '<unknown>',
            vscodeCommit: '',
            vscodeQuality: 'stable',
            ...contextProps,
        };
    }

    getContext() {
        return {
            ...this.context,
        };
    }

    setCurrentEnvironmentId(id: string | undefined) {
        this.context.environmentId = id;
    }

    setIsInternal(isInternal: boolean) {
        this.context.isInternal = isInternal;
    }

    setVscodeConfig(commit: string, quality: string) {
        this.context.vscodeCommit = commit;
        this.context.vscodeQuality = quality;
    }

    get isInternal(): boolean {
        return !!this.context.isInternal;
    }

    resolveCommonProperties(): { [key: string]: any } {
        const vsoContextProperties = this.getContext();
        return prefixPropertyNames(vsoContextProperties, 'vso');
    }

    private get browserId(): string {
        try {
            let machineId = window.localStorage.getItem('vso_machine_id');
            if (!machineId) {
                machineId = randomString();
                window.localStorage.setItem('vso_machine_id', machineId);
            }
            return machineId;
        } catch (error) {
            this.logger.error('Failed to get session id.', {
                error,
            });
            return randomString();
        }
    }

    private getSessionId(): string {
        try {
            let sessionId = window.sessionStorage.getItem('vso_sessionId');
            if (!sessionId) {
                sessionId = randomString();
                window.sessionStorage.setItem('vso_sessionId', sessionId);
            }
            return sessionId;
        } catch (error) {
            this.logger.error('Failed to get session id.', {
                error,
            });
            return randomString();
        }
    }

    private getPageLoadId(): string {
        return randomString();
    }

    track = (userEvent: ITelemetryEvent) => {
        let defaultProperties: Record<string, TelemetryPropertyValue> = {
            ...this.context,
            date: new Date(),
            sequenceNumber: ++sequenceNumber,
            telemetryEventId: randomString(),
        };

        if (!this.matchPath) {
            throw new Error('Call `initializeTelemetry` first.');
        }

        const knownPath = this.matchPath(location.pathname);
        if (knownPath) {
            defaultProperties['path'] = knownPath.path;
        }

        defaultProperties = prefixPropertyNames(defaultProperties, 'vsonline.portal.common');
        const { properties, measurements } = this.telemetryEventSerializer.serialize(
            userEvent,
            defaultProperties
        );

        const event = {
            name: userEvent.name,
            properties,
            measurements,
        };
        this.logger.verbose('TelemetryService.track', event);

        this.appInsights.trackEvent(event);
    }

    flush() {
        this.appInsights.flush();
    }
}
