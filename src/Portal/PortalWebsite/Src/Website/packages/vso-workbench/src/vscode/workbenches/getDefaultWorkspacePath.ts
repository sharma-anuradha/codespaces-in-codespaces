import { getCurrentEnvironmentId, authService } from 'vso-client-core';

export const getDefaultWorkspacePath = async (): Promise<string | undefined> => {
    const info = await authService.getCachedPartnerInfo(getCurrentEnvironmentId());
    if (!info) {
        return;
    }

    const defaultWorkspacePath = 'defaultWorkspacePath' in info ? info.defaultWorkspacePath : undefined;

    return defaultWorkspacePath;
};
