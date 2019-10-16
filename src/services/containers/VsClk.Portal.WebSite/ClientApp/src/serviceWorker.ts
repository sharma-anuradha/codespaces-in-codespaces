import { createTrace } from './utils/createTrace';
import { ServiceWorkerConfiguration } from './common/service-worker-configuration';
import { postServiceWorkerMessage } from './common/post-message';
import { configureServiceWorker } from './common/service-worker-messages';
import { isDefined } from './utils/isDefined';

const logger = createTrace('service-worker-installer');
const serviceWorkerPath = '/service-worker.js';

export function register(config: ServiceWorkerConfiguration) {
    if (!('serviceWorker' in navigator)) {
        return;
    }

    registerValidSW(serviceWorkerPath, config);
}

async function registerValidSW(swUrl: string, config: ServiceWorkerConfiguration) {
    // We occasionally rename our service workers, clean up the old ones.
    await unregisterOldServiceWorkers(swUrl);

    try {
        navigator.serviceWorker.addEventListener('controllerchange', () => {
            logger.info('New service worker version is controlling this client.');
        });

        const registration = await navigator.serviceWorker.register(swUrl);

        registration.addEventListener('updatefound', () => {
            logger.verbose('Update found.');

            postServiceWorkerMessage(
                {
                    type: configureServiceWorker,
                    payload: config,
                },
                registration.installing || registration.waiting
            );
        });

        // In case service worker is active, make sure it has the right configuration.
        postServiceWorkerMessage(
            {
                type: configureServiceWorker,
                payload: config,
            },
            registration.active
        );
    } catch (err) {
        logger.error('Failed to register service worker.', err.message);
    }
}

export async function unregisterOldServiceWorkers(swUrl: string) {
    // If old service worker exists, delete and reload with new one.
    const registrationsToRemove = (await navigator.serviceWorker.getRegistrations())
        .map((registration) => {
            const worker = registration.active || registration.waiting || registration.installing;
            if (!worker) {
                return undefined;
            }
            const path = new URL(worker.scriptURL);
            const { pathname } = path;
            if (pathname != swUrl) {
                return registration;
            }
            return undefined;
        })
        .filter(isDefined);

    if (registrationsToRemove.length === 0) {
        return;
    }

    await Promise.all(registrationsToRemove.map((r) => r.unregister()));

    window.location.reload();
}

export function unregister() {
    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.ready.then((registration) => {
            registration.unregister();
        });
    }
}
