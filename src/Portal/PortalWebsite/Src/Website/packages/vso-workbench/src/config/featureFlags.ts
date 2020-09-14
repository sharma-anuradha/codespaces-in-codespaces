import { authService } from '../auth/authService';

class FeatureFlagsAccessor {
    public async isEnabled(flagName: FeatureFlags, defaultValue?: boolean): Promise<boolean> {
        const partnerInfo = await authService.getPartnerInfo();

        if (!partnerInfo || !('featureFlags' in partnerInfo)) {
            return false;
        }

        const { featureFlags = {} as Record<FeatureFlags, boolean | undefined> } = partnerInfo;

        const value = (flagName in featureFlags) ? featureFlags[flagName] : defaultValue;

        return !!value;
    }
}

export enum FeatureFlags {
    PortForwardingService = 'portForwardingServiceEnabled',
    ServerlessEnabled = 'serverlessEnabled',
}

export const featureFlags = new FeatureFlagsAccessor();
