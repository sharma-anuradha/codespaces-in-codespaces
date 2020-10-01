import { TCodespaceEnvironment } from 'vso-client-core';

import { isValidCodespaceSubdomain } from '../../src/utils/isValidCodespaceSubdomain';
import { VALID_GITHUB_URLS_BY_ENVIRONMENT, INVALID_GITHUB_URLS_BY_ENVIRONMENT } from '../test-helpers/validGithubUrlsTestHelper';
import { INVALID_SALESFORCE_URLS_BY_ENVIRONMENT, VALID_SALESFORCE_URLS_BY_ENVIRONMENT } from '../test-helpers/validSalesforceUrlsTestHelper';
import { getEnvironmentInfo } from '../test-helpers/getEnvironmentInfoTestHelper';

let currentUrl = 'localhost';
Object.defineProperty(window, 'location', {
    value: {
      get href() {
          return currentUrl;
      },
      get hostname() {
        const url = new URL(currentUrl);
        return url.hostname;
    },
      enumerable: true,
      configurable: true,
      writable: true,
    }
});

const setLocationHref = (urlString: string) => {
    const url = new URL(urlString);
    const result = url.toString();

    if (result.indexOf('..') > -1) {
        throw new Error('Misconstructed URL.');
    }

    currentUrl = result;
}

const envInfo = getEnvironmentInfo();
const environments: TCodespaceEnvironment[] = [ 'production', 'staging', 'development' ];

const checkSubdomainValidity = (validUrlsMap: Record<TCodespaceEnvironment, string[]>, invalidUrlsMap: Record<TCodespaceEnvironment, string[]>) => {
    /**
         * For all environments, check that the Codespace `friendlyName` matches the subdomain.
         */
        for (let env of environments) {
            // make sure we return `true` from valid URLs
            for (let url of validUrlsMap[env]) {
                it(`should return "true" if GitHub Codespace friendly name matches subdomain [${env}] [${url}]`, () => {
                    setLocationHref(url);

                    expect(isValidCodespaceSubdomain(envInfo, env)).toBe(true);
                });
            }

            // make sure we return `false` from invalid URLs
            for (let url of invalidUrlsMap[env]) {
                it(`should return "false" if GitHub Codespace friendly name does not matche subdomain [${env}] [${url}]`, () => {
                    setLocationHref(url);

                    expect(isValidCodespaceSubdomain(envInfo, env)).toBe(false);
                });
            }

            // make sure that URLs that valid for other
            // environments are not valid for the current one
            for (let otherEnv of environments) {
                if (otherEnv === env) {
                    continue;
                }

                for (let url of validUrlsMap[otherEnv]) {
                    it(`should return "false" subdomains mismatches running environment [${env} -> ${otherEnv}] [${url}]`, () => {
                        setLocationHref(url);

                        expect(isValidCodespaceSubdomain(envInfo, env)).toBe(false);
                    });
                }
            }
        }
}

describe('isValidCodespaceSubdomain', () => {
    describe('GitHub', () => {
        it('should return `true` for local github', () => {
            setLocationHref('https://github.localhost');

            expect(isValidCodespaceSubdomain(envInfo, 'local')).toBe(true);
        });

        it('should return `false` for local github and location is not local', () => {
            setLocationHref('https://github.dev');

            expect(isValidCodespaceSubdomain(envInfo, 'local')).toBe(false);
        });

        checkSubdomainValidity(VALID_GITHUB_URLS_BY_ENVIRONMENT, INVALID_GITHUB_URLS_BY_ENVIRONMENT);
    });

    describe('Salesforce', () => {
        checkSubdomainValidity(VALID_SALESFORCE_URLS_BY_ENVIRONMENT, INVALID_SALESFORCE_URLS_BY_ENVIRONMENT);
    });
});
