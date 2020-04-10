import { createTrace } from 'vso-client-core';
import { ServiceWorkerMessage } from './service-worker-messages';

const trace = createTrace('post-message');

function getDefaultServiceWorker() {
    return (
        window.navigator &&
        window.navigator.serviceWorker &&
        window.navigator.serviceWorker.controller
    );
}

export function postServiceWorkerMessage(
    message: ServiceWorkerMessage,
    serviceWorker = getDefaultServiceWorker(),
    persist = true
): void {
    if (!navigator.serviceWorker) {
        trace.warn('Service worker not supported');
        return;
    }

    if (!serviceWorker) {
        trace.warn('Cannot send message to unavailable worker', { message: message.type });

        if (persist && messageBuffer.length === 0) {
            navigator.serviceWorker.ready.then(() => {
                for (const message of messageBuffer) {
                    postServiceWorkerMessage(message, getDefaultServiceWorker(), false);
                }

                messageBuffer = [];
            });
        }

        if (persist) {
            messageBuffer.push(message);
        }

        return;
    }

    trace.verbose('Sending message to service worker.', { message: message.type });
    serviceWorker.postMessage(message);
}

let messageBuffer: ServiceWorkerMessage[] = [];
