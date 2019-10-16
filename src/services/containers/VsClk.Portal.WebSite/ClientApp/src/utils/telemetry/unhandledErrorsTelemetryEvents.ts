import { unhandledRejectionEventName, unhandledErrorEventName, unhandledOnErrorEventName } from './TelemetryEventNames';

import { cleanupPIIForExternal } from '../cleanupPII';

import { ITelemetryEvent } from './types';


export class UnhandledRejectionTelemetryEvent implements ITelemetryEvent {
    readonly name: string = unhandledRejectionEventName;

    private getStack(rejection: PromiseRejectionEvent): string {
        const rejectionEvent = rejection as any;
        return (rejectionEvent.detail && rejectionEvent.detail.reason && rejectionEvent.detail.reason.stack) || '';
    }

    constructor(private readonly rejection: PromiseRejectionEvent) {}

    getProperties() {
        return {
            reason: this.rejection.reason,
            detail: (this.rejection as any).detail,
            stack: cleanupPIIForExternal(this.getStack(this.rejection)),
        }
    }
}

export class UnhandledErrorTelemetryEvent implements ITelemetryEvent {
    constructor(
        private readonly error: Error,
        readonly name: string = unhandledErrorEventName
    ) {}

    getProperties() {
        const { name, message, stack } = this.error;

        return {
            stack: cleanupPIIForExternal(stack),
            message,
            name,
        }
    }
}

export class UnhandledOnErrorTelemetryEvent implements ITelemetryEvent {
    readonly name: string = unhandledOnErrorEventName;

    constructor(
        private readonly event: Event | string,
        private readonly source?: string,
        private readonly lineno?: number,
        private readonly colno?: number,
        private readonly error?: Error
    ) {}

    getProperties() {
        const { name = '', message = '', stack = '' } = this.error || {};

        return {
            errorStack: cleanupPIIForExternal(stack),
            errorMessage: message,
            errorName: name,
            source: cleanupPIIForExternal(this.source),
            lineno: this.lineno,
            colno: this.colno,
        }
    }
}
