import { TCodespaceEnvironment } from 'vso-client-core';

const SALESFORCE_VALID_URLS_PROD = [
    'https://8f889227-948e-4391-8a91-8e83523de9b8.builder.code.com',
    'https://8f889227-948e-4391-8a91-8e83523de9b8.builder.code.com/some/path',
    'https://8f889227-948e-4391-8a91-8e83523de9b8.builder.code.com/some/path?somequery=foo&baz=bar',
    'https://8f889227-948e-4391-8a91-8e83523de9b8.builder.code.com/some/path?somequery=foo&baz=bar#fragment',
];
const SALESFORCE_INVALID_URLS_PROD = [
    'https://some-other-subdomain.builder.code.com',
    'https://-1.builder.code.com',
    'https://.1.builder.code.com',
    'https://8f889227-948e-4391-8a91-8e83523de9b8-1.builder.code.com',
    'https://test-8f889227-948e-4391-8a91-8e83523de9b8-1.builder.code.com',
    'https://8f889227-948e-4391-8a91-8e83523de9b8-1.builder.code.io',
    'https://8f889227-948e-4391-8a91-8e83523de9b8-1.builder.mode.com',
    'https://8f889227-948e-4391-8a91-8e83523de9b8-1.builder.mode.net',
    'https://8f889227-948e-4391-8a91-8e83523de9b8-1.builder.mode.net?query=test-legomushroom-depot-jwg7.github.dev',
    'https://other-domain.io?query=8f889227-948e-4391-8a91-8e83523de9b8-1.builder.mode.net',
    'https://other-domain.io?query=https://8f889227-948e-4391-8a91-8e83523de9b8-1.builder.mode.net',
];
const SALESFORCE_VALID_URLS_PPE = [
    'https://8f889227-948e-4391-8a91-8e83523de9b8.ppe.builder.code.com',
    'https://8f889227-948e-4391-8a91-8e83523de9b8.ppe.builder.code.com/some/path',
    'https://8f889227-948e-4391-8a91-8e83523de9b8.ppe.builder.code.com/some/path?somequery=foo&baz=bar',
    'https://8f889227-948e-4391-8a91-8e83523de9b8.ppe.builder.code.com/some/path?somequery=foo&baz=bar#fragment',
];
const SALESFORCE_INVALID_URLS_PPE = [
    'https://some-other-subdomain.ppe.builder.code.com',
    'https://-1.ppe.builder.code.com',
    'https://.1.ppe.builder.code.com',
    'https://8f889227-948e-4391-8a91-8e83523de9b8-1.ppe.builder.code.com',
    'https://test-8f889227-948e-4391-8a91-8e83523de9b8-1.ppe.builder.code.com',
];
const SALESFORCE_VALID_URLS_DEV = [
    'https://8f889227-948e-4391-8a91-8e83523de9b8.dev.builder.code.com',
    'https://8f889227-948e-4391-8a91-8e83523de9b8.dev.builder.code.com/some/path',
    'https://8f889227-948e-4391-8a91-8e83523de9b8.dev.builder.code.com/some/path?somequery=foo&baz=bar',
    'https://8f889227-948e-4391-8a91-8e83523de9b8.dev.builder.code.com/some/path?somequery=foo&baz=bar#fragment',
];
const SALESFORCE_INVALID_URLS_DEV = [
    'https://some-other-subdomain.dev.builder.code.com',
    'https://-1.dev.builder.code.com',
    'https://.1.dev.builder.code.com',
    'https://8f889227-948e-4391-8a91-8e83523de9b8-1.dev.builder.code.com',
    'https://test-8f889227-948e-4391-8a91-8e83523de9b8-1.dev.builder.code.com',
];

export const VALID_SALESFORCE_URLS_BY_ENVIRONMENT: Record<TCodespaceEnvironment, string[]> = {
    ['production']: SALESFORCE_VALID_URLS_PROD,
    ['staging']: SALESFORCE_VALID_URLS_PPE,
    ['development']: SALESFORCE_VALID_URLS_DEV,
    ['local']: [],
};

export const INVALID_SALESFORCE_URLS_BY_ENVIRONMENT: Record<TCodespaceEnvironment, string[]> = {
    ['production']: SALESFORCE_INVALID_URLS_PROD,
    ['staging']: SALESFORCE_INVALID_URLS_PPE,
    ['development']: SALESFORCE_INVALID_URLS_DEV,
    ['local']: [],
};
