import { ApplicationInsights } from '@microsoft/applicationinsights-web';
import { TELEMETRY_KEY } from '../constants';
import moment from 'moment';

export interface IActionTelemetryProperties {
    action: string,
    correlationId: string,
    isInternal: boolean,
}

export enum TelemetryEventNames {
    ActionSuccess = 'vsonline/action/success',
    ActionFailure = 'vsonline/action/failure',
}

class TelemetryService {
    private appInsights: ApplicationInsights;
    constructor() {
        this.appInsights = new ApplicationInsights({
            config: {
                instrumentationKey: TELEMETRY_KEY,
                endpointUrl: 'https://vortex.data.microsoft.com/collect/v1',
				emitLineDelimitedJson: true,
				autoTrackPageVisitTime: false,
				disableExceptionTracking: true,
				disableAjaxTracking: true
            }
        });
        this.appInsights.loadAppInsights();
    }

    private getDateProperty(): {[key: string]: any} {
        const date = moment(new Date()).utc().format('YYYY-MM-DD HH:mm:ss.SSS')
        return {
            date
        }
    }

    public trackErrorAction(eventProperties: IActionTelemetryProperties, measurements: {[key: string]: number}) {
        this.appInsights.trackEvent({
            name: TelemetryEventNames.ActionFailure,
            properties: Object.assign(eventProperties, this.getDateProperty()),
            measurements,
        });
    }

    public trackSuccessAction(eventProperties: IActionTelemetryProperties, measurements: {[key: string]: number}) {
        this.appInsights.trackEvent({
            name: TelemetryEventNames.ActionSuccess,
            properties: Object.assign(eventProperties, this.getDateProperty()),
            measurements,
        });
    }
        
}

export const telemetry = new TelemetryService();