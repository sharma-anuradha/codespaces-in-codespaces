import { ServiceWorkerMessage } from './service-worker-messages';

const defaultServiceWorker =
    window.navigator && window.navigator.serviceWorker && window.navigator.serviceWorker.controller;

export function postServiceWorkerMessage(
    message: ServiceWorkerMessage,
    serviceWorker = defaultServiceWorker
): void {
    if (!serviceWorker) {
        return;
    }

    serviceWorker.postMessage(message);
}
