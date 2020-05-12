import { createTrace, isDefined } from 'vso-client-core';

import { Event, Emitter, Disposable } from 'vscode-jsonrpc';

import { ServiceWorkerMessage, isServiceWorkerMessage, tryConfigureMessageType, configureServiceWorker } from './service-worker-messages';
import { ServiceWorkerConfiguration } from './service-worker-configuration';
import { postServiceWorkerMessage } from './post-message';

const logger = createTrace('service-worker-installer');
const serviceWorkerPath = '/service-worker.js';

const onMessageEmitter: Emitter<ServiceWorkerMessage> = new Emitter();
const onMessageEvent: Event<ServiceWorkerMessage> = onMessageEmitter.event;

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

        navigator.serviceWorker.addEventListener('message', (event) => {
            if (!isServiceWorkerMessage(event.data)) {
                return;
            }

            if (event.data.type === tryConfigureMessageType) {
                postServiceWorkerMessage({
                    type: configureServiceWorker,
                    payload: config,
                });

                return;
            }

            onMessageEmitter.fire(event.data);
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
            // We are not interested in the waiting service worker yet.
            registration.active || registration.installing
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

export function onMessage(listener: (message: ServiceWorkerMessage) => void): Disposable {
    return onMessageEvent(listener);
}
