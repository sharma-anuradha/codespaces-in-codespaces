import { TKnownPartnerShortcodes } from '../interfaces/TKnownPartners';

export const isValidPartnerShortcode = (partnerShortcode: string): boolean => {
    if (!partnerShortcode) {
        return false;
    }

    const partner = partnerShortcode as TKnownPartnerShortcodes;

    const isGitHub = partner === 'gh';
    const isSalesForce = partner === 'sf';
    const isVSO = partner === 'vs';

    return isGitHub || isSalesForce || isVSO;
};
