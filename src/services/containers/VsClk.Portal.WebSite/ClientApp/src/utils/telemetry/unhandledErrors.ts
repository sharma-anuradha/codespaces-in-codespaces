import { telemetry } from '.';
import { unhandledUnexpectedEventName } from './TelemetryEventNames';
import {
    UnhandledErrorTelemetryEvent,
    UnhandledOnErrorTelemetryEvent,
    UnhandledRejectionTelemetryEvent
} from './unhandledErrorsTelemetryEvents';
import { createTrace } from '../createTrace';

const trace = createTrace('UnhandledExceptions');

export const trackUnhandled = () => {
    const handleUnexpected = (err: Error) => {
        const errorTelemetryEvent = new UnhandledErrorTelemetryEvent(err, unhandledUnexpectedEventName);
        telemetry.track(errorTelemetryEvent);
        trace.error(errorTelemetryEvent);
    }

    window.onerror = (event: string | Event, ...args) => {
        try {
            const unhandledOnErrorTelemetryEvent = new UnhandledOnErrorTelemetryEvent(event, ...args);
            telemetry.track(unhandledOnErrorTelemetryEvent);
            trace.error(unhandledOnErrorTelemetryEvent);
        } catch (e) {
            handleUnexpected(e);
        }
        
        return false;
    };

    window.addEventListener('error', (e) => {
        try {
            const errorTelemetryEvent = new UnhandledErrorTelemetryEvent(e.error);
            telemetry.track(errorTelemetryEvent);
            trace.error(errorTelemetryEvent);
        } catch (e) {
            handleUnexpected(e);
        }
        
        return false;
    });

    window.addEventListener('unhandledrejection', (err) => {
        try {
            const rejectionTelemetryEvent = new UnhandledRejectionTelemetryEvent(err);
            telemetry.track(rejectionTelemetryEvent);
            trace.error(rejectionTelemetryEvent);
        } catch (e) {
            handleUnexpected(e);
        }

        return false;
    });
}
