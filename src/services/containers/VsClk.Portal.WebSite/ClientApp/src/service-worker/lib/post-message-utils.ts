import { ServiceWorkerMessage } from '../../common/service-worker-messages';
import { createLogger } from './logger';

declare var self: ServiceWorkerGlobalScope;

const logger = createLogger('post-message-utils');

export async function broadcast(message: ServiceWorkerMessage) {
    try {
        const allClients = await self.clients.matchAll({ type: 'window' });
        logger.verbose('broadcast: all clients', {
            type: message.type,
            count: allClients.length,
        });

        for (const client of allClients) {
            logger.verbose('broadcast: client', {
                clientId: client.id,
                clientUrl: client.url,
                type: message.type,
            });

            client.postMessage(message);
        }
    } catch (error) {
        logger.error('broadcast failed', {
            error,
        });
    }
}
