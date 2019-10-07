import { ApplicationInsights } from '@microsoft/applicationinsights-web';

import { TELEMETRY_KEY } from '../../constants';
import { createUniqueId } from '../../dependencies';
import { createTrace } from '../createTrace';
import { TelemetryEventSerializer } from './TelemetryEventSerializer';
import { ITelemetryEvent } from './types';

export interface IActionTelemetryProperties {
    action: string;
    correlationId: string;
    isInternal: boolean;
}

type TelemetryContext = {
    sessionId: string;
    pageLoadId: string;
    environmentId?: string;
};

class TelemetryService {
    private appInsights: ApplicationInsights;
    private context!: TelemetryContext;

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
            },
        });
        this.appInsights.loadAppInsights();

        this.initializeContext();
    }

    initializeContext() {
        this.context = {
            sessionId: this.getSessionId(),
            pageLoadId: this.getPageLoadId(),
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

    public track(event: ITelemetryEvent) {
        const { properties, measurements } = this.telemetryEventSerializer.serialize(event, {
            ...this.context,
            date: new Date(),
        });

        this.appInsights.trackEvent({
            name: event.name,
            properties,
            measurements,
        });
    }
}

export const telemetry = new TelemetryService();
