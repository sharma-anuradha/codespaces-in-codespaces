import { telemetryMarks } from '../../telemetry/telemetryMarks';
import { sendTelemetry } from '../../telemetry/telemetry';
import { getParentDomain, IPartnerInfo } from 'vso-client-core';
import { authService } from '../../auth/authService';
import { VSCodespacesPlatformInfoGeneral } from 'vs-codespaces-authorization';

export enum commandIds {
    codespacesGoHome = '_codespaces.embedder.gohome',
    codespacesTimeToInteractive = '_codespaces.timeToInteractive',
    deprecated_codespacesGitHubGoHome = '_github.gohome',
}

const gotHomeHandler = async () => {
    const platformInfo:
        | IPartnerInfo
        | VSCodespacesPlatformInfoGeneral
        | null = await authService.getPartnerInfo();

    if (!platformInfo) {
        throw new Error(`No platform info found.`);
    }

    // if there is no `homeIndicator` set, use management portal url instead
    if (
        !('vscodeSettings' in platformInfo) ||
        !('homeIndicator' in platformInfo.vscodeSettings)
    ) {
        const redirectUrl = await authService.getManagementPortalUrl();
        location.href = redirectUrl.toString();
        return;
    }

    // re-use the home `href` defined by the partner in the `homeIndicator`
    const { vscodeSettings } = platformInfo;
    const { homeIndicator } = vscodeSettings;
    if (homeIndicator) {
        location.href = homeIndicator.href;
        return;
    }

    throw new Error(`Cannot find home link.`);
};

export const commands = [
    /**
     * _Deprecated_ This command registered to support old VSCS extension that was
     * using GitHub-specific logic to trigger `_github.gohome` command.
     * The command could be removed at ~ 2020-08-22 (double check that
     * extension does not invoke the command anymore)
     */
    {
        id: commandIds.deprecated_codespacesGitHubGoHome,
        handler: gotHomeHandler,
    },
    {
        id: commandIds.codespacesGoHome,
        handler: gotHomeHandler,
    },
    {
        id: commandIds.codespacesTimeToInteractive,
        handler: () => {
            window.performance.measure(telemetryMarks.timeToInteractive);
            const [measure] = window.performance.getEntriesByName(telemetryMarks.timeToInteractive);
            sendTelemetry(`vsonline/portal/vscode-time-to-interactive`, {
                duration: measure.duration,
                hostedOn: getParentDomain(location.href),
            });
        },
    },
];
