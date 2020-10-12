import { VSCodespacesPlatformInfoGeneral } from 'vs-codespaces-authorization';
import { authService } from '../auth/authService';

type TFeatureFlagsPayload = Record<string, FeatureFlags | unknown> | undefined;

class FeatureFlagsAccessor {
    public async isEnabled(flagName: FeatureFlags, defaultValue: boolean = false): Promise<boolean> {
        const partnerInfo = await authService.getPartnerInfo() as VSCodespacesPlatformInfoGeneral;

        if (!partnerInfo) {
            return false;
        }

        return this.isEnabledInPayload(partnerInfo.featureFlags, flagName, defaultValue);
    }

    public isEnabledInPayload(
        featureFlags: TFeatureFlagsPayload,
        flagName: FeatureFlags,
        defaultValue: boolean = false
    ): boolean {
        if (!featureFlags) {
            return false;
        }

        const value = (flagName in featureFlags)
            ? featureFlags[flagName]
            : defaultValue;

        return `${value}` === 'true';
    }
}

export enum FeatureFlags {
    Developer = 'developer',
    PortForwardingService = 'portForwardingServiceEnabled',
    ServerlessEnabled = 'serverlessEnabled',
}

export const featureFlags = new FeatureFlagsAccessor();
