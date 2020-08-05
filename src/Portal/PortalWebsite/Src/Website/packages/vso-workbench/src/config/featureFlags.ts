import { authService } from '../auth/authService';

class FeatureFlagsAccessor {
    public async isEnabled(flagName: FeatureFlags): Promise<boolean> {
        const partnerInfo = await authService.getPartnerInfo();

        if (!partnerInfo || !('featureFlags' in partnerInfo)) {
            return false;
        }

        const { featureFlags = {} as Record<FeatureFlags, boolean | undefined> } = partnerInfo;

        return !!featureFlags[flagName];
    }
}

export enum FeatureFlags {
    PortForwardingService = 'port-forwarding-service',
}

export const featureFlags = new FeatureFlagsAccessor();
