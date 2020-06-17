import { IProductConfiguration } from 'vscode-web';

import { getCurrentEnvironmentId, authService } from 'vso-client-core';

export const getProductConfiguration = async (): Promise<IProductConfiguration | undefined> => {
    const info = await authService.getCachedPartnerInfo(getCurrentEnvironmentId());
    if (!info || !('vscodeSettings' in info)) {
        return;
    }

    // productConfig is not part of official API yet,
    // so types won't have the property yet
    if ('productConfiguration' in info.vscodeSettings) {
        return (info.vscodeSettings as any).productConfiguration;
    }
};
