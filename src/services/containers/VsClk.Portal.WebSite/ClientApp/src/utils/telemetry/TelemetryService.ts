import { match } from 'react-router-dom';

import { ApplicationInsights } from '@microsoft/applicationinsights-web';
import { detect } from 'detect-browser';

import { TELEMETRY_KEY, getVSCodeVersion } from '../../constants';
import { createUniqueId } from '../../dependencies';
import { createTrace } from '../createTrace';
import { TelemetryEventSerializer } from './TelemetryEventSerializer';
import { ITelemetryEvent, TelemetryPropertyValue } from './types';
import { prefixPropertyNames } from './prefixPropertyNames';
import versionFile from '../../version.json';

import { ITelemetryContext } from '../../interfaces/ITelemetryContext';

let sequenceNumber = 0;

type MatchFunction = (pathname: string) => match<{}> | null;

class TelemetryService {
    private appInsights: ApplicationInsights;
    private context!: ITelemetryContext;
    private logger: ReturnType<typeof createTrace>;
    private telemetryEventSerializer: TelemetryEventSerializer;
    private matchPath?: MatchFunction;

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

    initializeTelemetry(matchPath: MatchFunction) {
        this.matchPath = matchPath;
    }

    initializeContext() {
        const info = detect();

        this.context = {
            portalVersion: versionFile.version,
            browserId: this.browserId,
            sessionId: this.getSessionId(),
            pageLoadId: this.getPageLoadId(),
            host: location.host,
            browserName: (info && info.name) || '<unknown>',
            browserVersion: (info && info.version) || '<unknown>',
            browserOS: (info && info.os) || '<unknown>',
            vscodeCommit: '',
            vscodeQuality: 'stable',
        };
        this.setVscodeConfig();
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

    setVscodeConfig() {
        const vscodeConfig = getVSCodeVersion(
            window.localStorage.getItem('vso-featureset') === 'insider' ? 'insider' : 'stable'
        );
        this.context.vscodeCommit = vscodeConfig.commit;
        this.context.vscodeQuality = vscodeConfig.quality;
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

    track = (userEvent: ITelemetryEvent) => {
        let defaultProperties: Record<string, TelemetryPropertyValue> = {
            ...this.context,
            date: new Date(),
            sequenceNumber: ++sequenceNumber,
            telemetryEventId: createUniqueId(),
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

export const telemetry = new TelemetryService();
