import { VSCS_FEATURESET_LOCALSTORAGE_KEY } from 'vso-client-core';
import { authService } from '../auth/authService';

export const ensureVSCodeChannelFlag = async (): Promise<void> => {
    const vsoFeatureSet = window.localStorage.getItem(VSCS_FEATURESET_LOCALSTORAGE_KEY);
    if (vsoFeatureSet) {
        return;
    }

    const info = await authService.getPartnerInfo();
    if (!info) {
        return;
    }

    if (!('vscodeSettings' in info)) {
        return;
    }

    const { vscodeSettings } = info;
    const { vscodeChannel } = vscodeSettings;

    if (!vscodeChannel) {
        return;
    }

    window.localStorage.setItem(VSCS_FEATURESET_LOCALSTORAGE_KEY, vscodeChannel);
}
