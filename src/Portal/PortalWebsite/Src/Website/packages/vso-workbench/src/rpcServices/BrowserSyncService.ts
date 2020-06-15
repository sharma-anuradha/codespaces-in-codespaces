import { vsls, createTrace, getCurrentEnvironmentId } from 'vso-client-core';

import {
    BrowserSyncService as BrowserSyncServiceBase,
    BrowserConnectorMessages,
} from 'vso-ts-agent';
import { authService } from '../auth/authService';
import { AuthenticationError } from '../errors/AuthenticationError';

export const trace = createTrace('BrowserSyncService');

const goToManagementPortal = async (type: BrowserConnectorMessages) => {
    const info = await authService.getPartnerInfo();
    if (!info) {
        throw new AuthenticationError('No platform info found.');
    }

    const url = new URL(info.managementPortalUrl);
    if (type === BrowserConnectorMessages.SignOut) {
        url.searchParams.append('codespaceId', getCurrentEnvironmentId());
        await authService.signOut();
    }

    location.href = url.toString();
};

export class BrowserSyncService extends BrowserSyncServiceBase {
    public async onSourceEvent(e: vsls.SourceEventArgs) {
        trace.info('event received', e);

        switch (e.sourceId) {
            case BrowserConnectorMessages.ConnectToEnvironment: {
                const payload = JSON.parse(e.jsonContent);

                trace.info('Connect to environment requested.', payload);
                return;
            }

            case BrowserConnectorMessages.DisconnectFromEnvironment:
            case BrowserConnectorMessages.SignOut: {
                await goToManagementPortal(e.sourceId);
                return;
            }
        }
    }
}
