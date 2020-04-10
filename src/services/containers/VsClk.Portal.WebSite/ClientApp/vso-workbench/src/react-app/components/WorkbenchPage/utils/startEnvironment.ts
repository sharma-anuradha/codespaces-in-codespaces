import { EnvironmentStateInfo } from 'vso-client-core';

import { EnvironmentWorkspaceState } from '../../../../interfaces/EnvironmentWorkspaceState';
import { authService } from '../../../../auth/authService';
import { getCurrentEnvironmentId } from '../../../../utils/getCurrentEnvironmentId';
import { vsoAPI } from '../../../../api/vsoAPI';

export const startEnvironment = async (
    setState: Function,
    handleAPIError: (e: Error) => any,
    traceInfo: Function
): Promise<void> => {
    const token = await authService.getCachedToken();

    if (!token) {
        traceInfo(`No token found.`);

        setState({
            value: EnvironmentWorkspaceState.SignedOut,
        });

        return;
    }

    setState({
        value: EnvironmentStateInfo.Starting,
    });

    traceInfo(`Starting environment`);

    try {
        await vsoAPI.startEnvironment(getCurrentEnvironmentId(), token);
    } catch (e) {
        handleAPIError(e);
    }
};
