import { CancellationToken } from 'vscode-jsonrpc';

import { Signal } from '../../utils/signal';
import { createTrace } from '../../utils/createTrace';

export const trace = createTrace('RenewTokenFactory');

export class LoginRequiredError extends Error {}

const REDIRECT_POLL_INTERVAL = 50;

export interface IRenewTokenFactoryOptions {
    paramOfInterest: string;
    mode: 'hash' | 'query';
    onCreateRenewEntity: (renewUrl: URL) => CancellationToken | void;
    getLocation: () => Location | null;
    onComplete: () => void;
}

export const renewTokenFactory = (options: IRenewTokenFactoryOptions) => {
    const { onCreateRenewEntity, getLocation, onComplete } = options;

    let intervalHandle: ReturnType<typeof setInterval> | undefined;
    let timeoutHandle: ReturnType<typeof setTimeout> | undefined;

    return (renewUrl: URL, nonce: string, timeout: number = 10000) => {
        const clearTimers = () => {
            clearInterval(intervalHandle!);
            clearTimeout(timeoutHandle!);
        };
        
        const signal = new Signal<string | null>();
        
        const complete = (data: string | null) => {
            signal.complete(data);
            clearTimers();
            onComplete();
        };

        timeoutHandle = setTimeout(() => {
            trace.error('No access token found.');
            complete(null);
        }, timeout);
        
        const cancellationToken = onCreateRenewEntity(renewUrl);

        if (cancellationToken) {
            cancellationToken.onCancellationRequested(() => {
                trace.error('Auth request was cancelled.');
                complete(null);
            });
        }

        intervalHandle = setInterval(() => {
            const location = getLocation();
            
            if (!location) {
                return;
            }

            const { href, hash, search } = location;

            const query = (options.mode === 'hash')
                ? hash
                : search;

            if (href && query) {
                const replace = (options.mode === 'hash')
                    ? '#'
                    : '?';

                const searchParams = new URLSearchParams(query.replace(replace, ''));
                
                const error = searchParams.get('error');
                if (error) {
                    const errorDescription = searchParams.get('error_description') || error || 'Redirection error.';
                    trace.error(errorDescription);
                    
                    clearTimers();
                    onComplete();

                    // in case login required, throw so outer code can adjust
                    if (error === 'login_required') {
                        signal.reject(new LoginRequiredError(errorDescription))
                    } else {
                        signal.complete(null);
                    }

                    return;
                }
                
                const state = searchParams.get('state');
                if (state !== nonce) {
                    trace.error('State params do not match.');
                    complete(null);
                    return;
                }

                const paramOfInterest = searchParams.get(options.paramOfInterest);
                if (!paramOfInterest) {
                    trace.error(`No "${options.paramOfInterest}" found.`);
                    complete(null);
                    return;
                }
                
                complete(paramOfInterest);
            }
        }, REDIRECT_POLL_INTERVAL);

        return signal.promise;
    };
};
