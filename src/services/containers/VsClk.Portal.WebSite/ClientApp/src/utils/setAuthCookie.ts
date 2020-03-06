import { isHostedOnGithub } from './isHostedOnGithub';
import { CrossDomainPFAuthenticator } from './CrossDomainPFAuthenticator';
import { useActionContext } from '../actions/middleware/useActionContext';
import { TEnvironment } from '../services/configurationService';

const getGithubPFDomain = (subdomain: '' | 'dev.' | 'ppe.' = '') => {
    return `https://apps.${subdomain}workspaces.githubusercontent.com`;
}

const getPFDomain = (domain: TEnvironment) => {
    if (!isHostedOnGithub()) {
        return location.origin;
    }

    switch (domain) {
        case 'production': {
            return getGithubPFDomain();
        }

        case 'development':
        case 'local': {
            return getGithubPFDomain('dev.');
        }

        case 'staging': {
            return getGithubPFDomain('ppe.');
        }

        default:
            throw new Error('Unknow environment');
    }
}

const getCurrentEnvironment = () => {
    const context = useActionContext();

    const { state } = context;
    const { configuration } = state;

    if (!configuration) {
        throw new Error('No configuration set.');
    }

    const { environment } = configuration;

    return environment;
}

export async function setAuthCookie(accessToken: string) {
    const environment = getCurrentEnvironment();
    const pfDomain = getPFDomain(environment);

    const crossDomainAuth = new CrossDomainPFAuthenticator(pfDomain);
    const tokenName = isHostedOnGithub()
        ? 'cascadeToken'
        : 'token';

    await crossDomainAuth.setPFCookie(accessToken, tokenName);
    crossDomainAuth.dispose();
}

export async function deleteAuthCookie() {
    const environment = getCurrentEnvironment();
    const pfDomain = getPFDomain(environment);

    const crossDomainAuth = new CrossDomainPFAuthenticator(pfDomain);
    await crossDomainAuth.removePFCookieWithCascadeToken();
    crossDomainAuth.dispose();
}
