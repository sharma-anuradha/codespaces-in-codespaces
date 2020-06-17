import { FeatureSet } from 'vso-client-core';
import { authService } from "../auth/authService";

export const getPartnerInfoFeatureSet = async (): Promise<FeatureSet | null> => {
    const partnerInfo = await authService.getPartnerInfo();

    if (!partnerInfo || !('vscodeSettings' in partnerInfo)) {
        return null;
    }

    const { vscodeSettings } = partnerInfo;
    const { vscodeChannel } = vscodeSettings;

    if (vscodeChannel === 'insider') {
        return FeatureSet.Insider;
    }

    if (vscodeChannel === 'stable') {
        return FeatureSet.Stable;
    }

    return null;
}
