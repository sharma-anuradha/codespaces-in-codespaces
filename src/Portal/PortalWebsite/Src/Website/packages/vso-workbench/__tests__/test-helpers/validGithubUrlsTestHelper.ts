import { TCodespaceEnvironment } from 'vso-client-core';

const GITHUB_VALID_URLS_PROD = [
    'https://legomushroom-depot-jwg7.github.dev',
    'https://legomushroom-depot-jwg7.github.dev/some/path',
    'https://legomushroom-depot-jwg7.github.dev/some/path?somequery=foo&baz=bar',
    'https://legomushroom-depot-jwg7.github.dev/some/path?somequery=foo&baz=bar#fragment',
];
const GITHUB_INVALID_URLS_PROD = [
    'https://some-other-subdomain.github.dev',
    'https://-1.github.dev',
    'https://.1.github.dev',
    'https://legomushroom-depot-jwg7-1.github.dev',
    'https://test-legomushroom-depot-jwg7-1.github.dev',
    'https://legomushroom-depot-jwg7.github.com',
    'https://legomushroom-depot-jwg7.other-domain.io',
    'https://legomushroom-depot-jwg7.other-domain.io?query=legomushroom-depot-jwg7.github.dev',
    'https://other-domain.io?query=legomushroom-depot-jwg7.github.dev',
    'https://other-domain.io?query=https://legomushroom-depot-jwg7.github.dev',
];
const GITHUB_VALID_URLS_PPE = [
    'https://legomushroom-depot-jwg7.ppe.github.dev',
    'https://legomushroom-depot-jwg7.ppe.github.dev/some/path',
    'https://legomushroom-depot-jwg7.ppe.github.dev/some/path?somequery=foo&baz=bar',
    'https://legomushroom-depot-jwg7.ppe.github.dev/some/path?somequery=foo&baz=bar#fragment',
];
const GITHUB_INVALID_URLS_PPE = [
    'https://some-other-subdomain.ppe.github.dev',
    'https://-1.ppe.github.dev',
    'https://.1.ppe.github.dev',
    'https://legomushroom-depot-jwg7-1.ppe.github.dev',
    'https://test-legomushroom-depot-jwg7-1.ppe.github.dev',
];
const GITHUB_VALID_URLS_DEV = [
    'https://legomushroom-depot-jwg7.dev.github.dev',
    'https://legomushroom-depot-jwg7.dev.github.dev/some/path',
    'https://legomushroom-depot-jwg7.dev.github.dev/some/path?somequery=foo&baz=bar',
    'https://legomushroom-depot-jwg7.dev.github.dev/some/path?somequery=foo&baz=bar#fragment',
];
const GITHUB_INVALID_URLS_DEV = [
    'https://some-other-subdomain.dev.github.dev',
    'https://-1.dev.github.dev',
    'https://.1.dev.github.dev',
    'https://legomushroom-depot-jwg7-1.dev.github.dev',
    'https://test-legomushroom-depot-jwg7-1.dev.github.dev',
];
export const VALID_GITHUB_URLS_BY_ENVIRONMENT: Record<TCodespaceEnvironment, string[]> = {
    ['production']: GITHUB_VALID_URLS_PROD,
    ['staging']: GITHUB_VALID_URLS_PPE,
    ['development']: GITHUB_VALID_URLS_DEV,
    ['local']: [],
};
export const INVALID_GITHUB_URLS_BY_ENVIRONMENT: Record<TCodespaceEnvironment, string[]> = {
    ['production']: GITHUB_INVALID_URLS_PROD,
    ['staging']: GITHUB_INVALID_URLS_PPE,
    ['development']: GITHUB_INVALID_URLS_DEV,
    ['local']: [],
};
