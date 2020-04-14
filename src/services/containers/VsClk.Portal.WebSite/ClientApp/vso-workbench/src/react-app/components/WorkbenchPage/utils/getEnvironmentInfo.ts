import { IEnvironment, createTrace, getCurrentEnvironmentId } from 'vso-client-core';

import { authService } from '../../../../auth/authService';
import { vsoAPI } from '../../../../api/vsoAPI';
import { EnvironmentWorkspaceState } from '../../../../interfaces/EnvironmentWorkspaceState';
import { AuthenticationError } from '../../../../errors/AuthenticationError';

const trace = createTrace('vso-getEnvironmentInfo');

export const getEnvironmentInfo = async (
    setState: Function,
    handleAPIError: (e: Error) => any
): Promise<IEnvironment | null> => {
    const token = await authService.getCachedToken();
    if (!token) {
        throw new AuthenticationError('No token found.');
    }

    try {
        const environmentInfo = await vsoAPI.getEnvironmentInfo(getCurrentEnvironmentId(), token);
        if (!environmentInfo) {
            setState({
                value: EnvironmentWorkspaceState.Error,
                message: 'Unknown error.',
            });

            return null;
        }

        return environmentInfo;
    } catch (e) {
        trace.error(e);
        return handleAPIError(e);
    }
};
