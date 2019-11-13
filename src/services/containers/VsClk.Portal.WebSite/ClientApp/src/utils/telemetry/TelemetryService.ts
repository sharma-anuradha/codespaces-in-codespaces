import { ApplicationInsights } from '@microsoft/applicationinsights-web';
import { detect } from 'detect-browser';

import { TELEMETRY_KEY } from '../../constants';
import { createUniqueId } from '../../dependencies';
import { createTrace } from '../createTrace';
import { TelemetryEventSerializer } from './TelemetryEventSerializer';
import { ITelemetryEvent, TelemetryPropertyValue } from './types';
import { matchPath } from '../../routes';
import versionFile from '../../version.json';

import { ITelemetryContext } from '../../interfaces/ITelemetryContext';

class TelemetryService {
    private appInsights: ApplicationInsights;
    private context!: ITelemetryContext;
    private logger: ReturnType<typeof createTrace>;
    private telemetryEventSerializer: TelemetryEventSerializer;

    constructor() {
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
        this.initializeContext();
    }

    initializeContext() {
        const info = detect();
        this.context = {
            portalVersion: versionFile.version,
            machineId: this.machineId,
            sessionId: this.getSessionId(),
            pageLoadId: this.getPageLoadId(),
            host: location.host,
            browserName: (info && info.name) || '<unknown>',
            browserVersion: (info && info.version) || '<unknown>',
            browserOS: (info && info.os) || '<unknown>',
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

    get isInternal(): boolean {
        return !!this.context.isInternal;
    }

    private get machineId(): string {
        try {
            let machineId = window.localStorage.getItem('vso_machine_id');
            if (!machineId) {
                machineId = createUniqueId();
                window.localStorage.setItem('vso_machine_id', machineId);
            }
            return machineId;
        } catch (error) {
            this.logger.error('Failed to get session id.', {
                error,
            });
            return createUniqueId();
        }
    }

    private getSessionId(): string {
        try {
            let sessionId = window.sessionStorage.getItem('vso_sessionId');
            if (!sessionId) {
                sessionId = createUniqueId();
                window.sessionStorage.setItem('vso_sessionId', sessionId);
            }
            return sessionId;
        } catch (error) {
            this.logger.error('Failed to get session id.', {
                error,
            });
            return createUniqueId();
        }
    }

    private getPageLoadId(): string {
        return createUniqueId();
    }

    public track(userEvent: ITelemetryEvent) {
        const defaultProperties: Record<string, TelemetryPropertyValue> = {
            ...this.context,
            date: new Date(),
            telemetryEventId: createUniqueId(),
        };

        const knownPath = matchPath(location.pathname);
        if (knownPath) {
            defaultProperties['path'] = knownPath.path;
        }

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
}

export const telemetryCore = new TelemetryService();
