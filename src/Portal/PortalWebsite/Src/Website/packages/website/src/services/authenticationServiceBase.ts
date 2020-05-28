import { SupportedGitService } from 'vso-ts-agent';

import { Disposable } from 'vscode-jsonrpc';

export interface IAuthenticationAttempt extends Disposable {
    authenticate(): Promise<string | null>;
    url: string;
    target: string;
    gitServiceType: SupportedGitService;
}