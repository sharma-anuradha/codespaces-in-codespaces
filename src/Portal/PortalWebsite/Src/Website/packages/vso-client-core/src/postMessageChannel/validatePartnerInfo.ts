import { IPartnerInfo } from '../interfaces/IPartnerInfo';

const KNOWN_PARTNERS = ['github', 'salesforce'];

export const validatePartnerInfo = (info: IPartnerInfo) => {
    if (!info.token) {
        throw new Error('No Cascade token set.');
    }

    if (!info.managementPortalUrl) {
        throw new Error('No managementPortalUrl set.');
    }

    if (!info.environmentId) {
        throw new Error('No environmentId set.');
    }

    if (!KNOWN_PARTNERS.includes(info.partnerName)) {
        throw new Error(`Unknown partner "${info.partnerName}".`);
    }
};
