import { SupportedGitService } from '../interfaces/SupportedGitService';
import { getSupportedGitServiceByHost } from './getSupportedGitServiceByHost';

export function getSupportedGitService(url: string): SupportedGitService {
    const parsedUrl = new URL(url);
    return getSupportedGitServiceByHost(parsedUrl.host);
}