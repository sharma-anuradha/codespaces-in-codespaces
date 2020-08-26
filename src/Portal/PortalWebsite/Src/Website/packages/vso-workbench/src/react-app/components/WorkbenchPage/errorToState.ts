import { AuthenticationError } from '../../../errors/AuthenticationError';
import { RateLimitingError } from '../../../errors/ReteLimitingError';
import { HttpError } from '../../../errors/HttpError';
import { EnvironmentWorkspaceState } from '../../../interfaces/EnvironmentWorkspaceState';
import { IWorkbenchStateObject } from './IWorkbenchStateObject';
import { ConfigurationError } from '../../../errors/ConfigurationError';

export const errorToState = (e: Error): IWorkbenchStateObject => {
    if (e instanceof RateLimitingError) {
        return {
            value: EnvironmentWorkspaceState.Error,
            message: 'Too many requests. Try again in few minutes.',
        };
    }

    if (e instanceof AuthenticationError) {
        return {
            value: EnvironmentWorkspaceState.SignedOut,
        };
    }

    if (e instanceof ConfigurationError) {
        return {
            value: EnvironmentWorkspaceState.Error,
            message: 'Failed to fetch configuration.',
        };
    }

    if (e instanceof HttpError) {
        return {
            value: EnvironmentWorkspaceState.Error,
            message: `Failed to get codespace info. [${e.message}]`,
        };
    }

    return {
        value: EnvironmentWorkspaceState.Error,
        message: e.message || 'Unknown error when fetching codespace info.',
    };
};
