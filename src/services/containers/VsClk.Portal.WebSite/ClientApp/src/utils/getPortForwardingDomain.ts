import { useActionContext } from '../actions/middleware/useActionContext';
import { isHostedOnGithub } from './isHostedOnGithub';
import { TEnvironment } from '../services/configurationService';

const getGithubPFDomain = (subdomain: '' | 'dev.' | 'ppe.' = '') => {
    return `https://apps.${subdomain}workspaces.githubusercontent.com`;
}

export const getPFDomain = (domain: TEnvironment) => {
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
            throw new Error('Unknown environment');
    }
}

export const getCurrentEnvironment = () => {
    const context = useActionContext();

    const { state } = context;
    const { configuration } = state;

    if (!configuration) {
        throw new Error('No configuration set.');
    }

    const { environment } = configuration;

    return environment;
}