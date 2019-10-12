import { Signal } from '../../utils/signal';
import { parseJWTToken } from '../../utils/parseJWTToken';
import { IToken } from '../../typings/IToken';
import { createTrace } from '../../utils/createTrace';
import { CancellationToken } from 'vscode-jsonrpc';

export const trace = createTrace('RenewTokenFactory');

export class LoginRequiredError extends Error {}

const REDIRECT_POLL_INTERVAL = 50;

interface IRenewTokenFactoryOptions {
    onCreateRenewEntity: (renewUrl: URL) => CancellationToken | void;
    getLocation: () => Location | null;
    onComplete: () => void;
}

export const renewTokenFactory = (options: IRenewTokenFactoryOptions) => {
    const { onCreateRenewEntity, getLocation, onComplete } = options;
    return (renewUrl: URL, nonce: string, timeout: number = 10000) => {
        const clearTimers = () => {
            clearInterval(intervalHandle);
            clearTimeout(timeoutHandle);
        };
        
        const signal = new Signal<IToken | null>();
        
        const complete = (data: IToken | null) => {
            signal.complete(data);
            clearTimers();
            onComplete();
        };

        const timeoutHandle = setTimeout(() => {
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

        const intervalHandle = setInterval(() => {
            const location = getLocation();
            
            if (!location) {
                return;
            }

            let { href, hash } = location;
            if (href && hash) {
                const searchParams = new URLSearchParams(hash.replace('#', ''));
                
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

                const accessToken = searchParams.get('access_token');
                if (!accessToken) {
                    trace.error('No access token found.');
                    complete(null);
                    return;
                }
                
                const token = parseJWTToken(accessToken);

                complete(token);
            }
        }, REDIRECT_POLL_INTERVAL);

        return signal.promise;
    };
};
