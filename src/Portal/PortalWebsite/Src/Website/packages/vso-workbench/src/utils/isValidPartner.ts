import { TKnownPartners } from '../interfaces/TKnownPartners';

export const isValidPartner = (partnerName: string): boolean => {
    if (!partnerName) {
        return false;
    }

    const partner = partnerName as TKnownPartners;

    const isGitHub = partner === 'github';
    const isSalesForce = partner === 'salesforce';
    const isVSO = partner === 'vso';

    return isGitHub || isSalesForce || isVSO;
};
