import { AuthenticationError } from './AuthenticationError';

export class FatalPlatformRedirectionError extends AuthenticationError {
    public errorType = 'FatalPlatformRedirectionError';
}
