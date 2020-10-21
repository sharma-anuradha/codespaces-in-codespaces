import { RateLimitingError } from '../src/errors/ReteLimitingError';
import { errorToState } from '../src/react-app/components/WorkbenchPage/errorToState';
import { EnvironmentWorkspaceState } from '../src/interfaces/EnvironmentWorkspaceState';
import { AuthenticationError } from '../src/errors/AuthenticationError';
import { ConfigurationError } from '../src/errors/ConfigurationError';
import { HttpError } from '../src/errors/HttpError';

describe('workspace page `errorToState`', () => {
    it('should map `RateLimitingError`', () => {
        const result = errorToState(new RateLimitingError());

        expect(result).toMatchObject({
            value: EnvironmentWorkspaceState.Error,
            message: 'Too many requests. Try again in few minutes.',
        });
    });

    it('should map `AuthenticationError`', () => {
        const result = errorToState(new AuthenticationError());

        expect(result).toMatchObject({
            value: EnvironmentWorkspaceState.SignedOut,
        });
    });

    it('should map `ConfigurationError`', () => {
        const result = errorToState(new ConfigurationError());

        expect(result).toMatchObject({
            value: EnvironmentWorkspaceState.Error,
            message: 'Failed to fetch configuration.',
        });
    });

    it('should map `HttpError`', () => {
        const e = new HttpError(404, 'Workspace not found.');
        const result = errorToState(e);

        expect(result).toMatchObject({
            value: EnvironmentWorkspaceState.Error,
            message: `Failed to get codespace info. [${e.message}]`,
        });
    });

    it('should map `unknown error`', () => {
        const e = new Error('unknonw error test');
        const result = errorToState(e);

        expect(result).toMatchObject({
            value: EnvironmentWorkspaceState.Error,
            message: e.message || 'Unknown error when fetching codespace info.',
        });
    });
});
