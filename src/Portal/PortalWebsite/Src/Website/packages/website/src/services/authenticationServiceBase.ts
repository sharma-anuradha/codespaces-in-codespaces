import { Disposable } from 'vscode-jsonrpc';
import { SupportedGitService } from '../utils/gitUrlNormalization';

export interface IAuthenticationAttempt extends Disposable {
    authenticate(): Promise<string | null>;
    url: string;
    target: string;
    gitServiceType: SupportedGitService;
}