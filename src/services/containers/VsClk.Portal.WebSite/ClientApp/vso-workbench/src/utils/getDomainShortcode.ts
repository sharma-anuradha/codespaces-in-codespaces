import { TKnownPartnerShortcodes } from '../interfaces/TKnownPartners';
import { isValidPartnerShortcode } from './isValidPartnerShortcode';
import { KNOWN_VSO_HOSTNAMES } from 'vso-client-core';

const getShortCodeFromFragment = (fragment: string) => {
    const split = fragment.split('-');
    const maybeShortcode = split[1];

    if (split.length !== 2 || !maybeShortcode) {
        throw new Error('Unknown partner.');
    }

    if (isValidPartnerShortcode(maybeShortcode)) {
        return maybeShortcode as TKnownPartnerShortcodes;
    }

    throw new Error(`No shortcode found in "${fragment}"`);
};

export const getPartnerShortcode = (): TKnownPartnerShortcodes => {
    const split = location.hostname.split('.');
    const eTLD = split.slice(1).join('.');

    if (!KNOWN_VSO_HOSTNAMES.includes(eTLD)) {
        throw new Error(`Unknown eTLD of ${eTLD}`);
    }

    const shortCode = getShortCodeFromFragment(split[0]);
    return shortCode;
};
