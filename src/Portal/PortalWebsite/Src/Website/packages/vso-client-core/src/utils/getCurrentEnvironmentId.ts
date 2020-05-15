import { isGitHubHostname } from './isGitHubHostname';
import { KNOWN_VSO_HOSTNAMES } from '../constants';
import { isGithubTLD } from './isGithubTLD';

const UUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

/**
 * https://online.dev.core.vsengsaas.visualstudio.com/workspace/{id}
 * https://online.dev.core.vsengsaas.visualstudio.com/environment/{id}
 * https://{:id}.workspaces-dev.github.com/environment/{:id}
 * https://{:id}.workspaces-dev.github.com
 */
export const getCurrentEnvironmentId = () => {
    const { pathname, hostname, href } = location;

    const isKnownExactOrigin = KNOWN_VSO_HOSTNAMES.includes(hostname);
    if (!isKnownExactOrigin && !isGitHubHostname(href)) {
        throw new Error(`Unknown origin "${hostname}".`);
    }

    const firstSubdomain = hostname.split('.')[0];
    if (UUID_REGEX.test(firstSubdomain) && isGithubTLD(href)) {
        return firstSubdomain;
    }

    const split = pathname.split('/').slice(1);
    const [workspacePath, id] = split;

    if (!['workspace', 'environment'].includes(workspacePath)) {
        throw new Error(`Unexpected path "${workspacePath}".`);
    }

    if (typeof id !== 'string') {
        throw new Error(`Unexpected id "${id}".`);
    }

    const trimmedId = id.trim();
    const isValidId = /^([a-f0-9]{8})\-([a-f0-9]{4})\-([a-f0-9]{4})\-([a-f0-9]{4})\-([a-f0-9]{12})$/i.test(
        trimmedId
    );
    if (!isValidId) {
        throw new Error(`The workspace id [${trimmedId}] is not valid.`);
    }

    return trimmedId;
};
