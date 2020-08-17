import { config } from '../config/config';
import { AuthenticationError } from '../errors/AuthenticationError';
import { featureFlags, FeatureFlags } from '../config/featureFlags';
import { HttpError } from '../errors/HttpError';

export class PortForwardingManagementApi {
    public async warmupConnection(codespaceId: string, port: number, token: string) {
        const isPortForwardingServiceEnabled = await featureFlags.isEnabled(
            FeatureFlags.PortForwardingService
        );

        if (!isPortForwardingServiceEnabled) {
            return;
        }

        const response = await fetch(config.portForwardingManagementEndpoint, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                id: codespaceId,
                port,
            }),
        });

        if (!response.ok) {
            const message = 'Cannot port forwarding connection.';

            if (response.status === 401) {
                throw new AuthenticationError(message);
            }

            throw new HttpError(response.status, response.statusText);
        }
    }
}

export const portForwardingManagementApi = new PortForwardingManagementApi();
