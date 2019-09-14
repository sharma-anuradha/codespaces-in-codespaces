import { ServiceWorkerMessage } from './service-worker-messages';

export function postServiceWorkerMessage(message: ServiceWorkerMessage): void {
    if ('serviceWorker' in window.navigator && window.navigator.serviceWorker.controller) {
        window.navigator.serviceWorker.controller.postMessage(message);
    }
}
