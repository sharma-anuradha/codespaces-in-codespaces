import { ITelemetryEvent, cleanupPIIForExternal } from 'vso-client-core';

import { unhandledRejectionEventName, unhandledErrorEventName, unhandledOnErrorEventName } from './TelemetryEventNames';
import { useActionContext } from '../../actions/middleware/useActionContext';


export class UnhandledRejectionTelemetryEvent implements ITelemetryEvent {
    readonly name: string = unhandledRejectionEventName;

    private getStack(rejection: PromiseRejectionEvent): string {
        const rejectionEvent = rejection as any;
        return (rejectionEvent.detail && rejectionEvent.detail.reason && rejectionEvent.detail.reason.stack) || '';
    }

    constructor(private readonly rejection: PromiseRejectionEvent) {}

    get properties() {
        const { state } = useActionContext();
        const { authentication } = state;

        return {
            reason: this.rejection.reason,
            detail: (this.rejection as any).detail,
            stack: cleanupPIIForExternal(authentication.isInternal, this.getStack(this.rejection)),
        }
    }
}

export class UnhandledErrorTelemetryEvent implements ITelemetryEvent {
    constructor(
        private readonly error: Error,
        readonly name: string = unhandledErrorEventName
    ) {}

    get properties() {
        const { name, message, stack } = this.error;
        const { state } = useActionContext();
        const { authentication } = state;

        return {
            stack: cleanupPIIForExternal(authentication.isInternal, stack),
            message,
            name,
        }
    }
}

export class UnhandledOnErrorTelemetryEvent implements ITelemetryEvent {
    readonly name: string = unhandledOnErrorEventName;

    constructor(
        // tslint:disable-next-line
        private readonly event: Event | string,
        private readonly source?: string,
        private readonly lineno?: number,
        private readonly colno?: number,
        private readonly error?: Error
    ) {}

    get properties() {
        const { name = '', message = '', stack = '' } = this.error || {};
        const { state } = useActionContext();
        const { authentication } = state;
        const { isInternal } = authentication;

        return {
            errorStack: cleanupPIIForExternal(isInternal, stack),
            errorMessage: message,
            errorName: name,
            source: cleanupPIIForExternal(isInternal, this.source),
            lineno: this.lineno,
            colno: this.colno,
        }
    }
}
