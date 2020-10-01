import {
    IEnvironment,
    isGithubDotDevTLD,
    isSalesforceTLD,
    isGithubLocalTLD,
    TCodespaceEnvironment,
} from 'vso-client-core';
import { isValidGithubSubdomain } from './isValidGithubSubdomain';
import { isValidSalesforceSubdomain } from './isValidSalesforceSubdomain';

/**
 * Function that checks if the subdomain is suitable for running this Codespace
 * For GitHub the URL should be `https://:cs-friendly-name.github.dev`
 * For Salesforce the subdomain should be `https://:cs-id.builder.code.com`
 * We enable the `github.localhost` domains to hold any Codespaces for dev purposes.
 */
export const isValidCodespaceSubdomain = (envInfo: IEnvironment, env: TCodespaceEnvironment) => {
    // local GitHub Codespace can run on any URL structure
    if (env === 'local' && isGithubLocalTLD(location.href)) {
        return true;
    }
    // check the GitHub URL validity
    if (isGithubDotDevTLD(location.href)) {
        return isValidGithubSubdomain(envInfo, env);
    }
    // check the Salesforce URL validity
    if (isSalesforceTLD(location.href)) {
        return isValidSalesforceSubdomain(envInfo, env);
    }

    return false;
};
