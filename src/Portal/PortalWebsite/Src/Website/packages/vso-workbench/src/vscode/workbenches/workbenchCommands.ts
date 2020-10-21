import { getParentDomain, IPartnerInfo } from 'vso-client-core';
import { VSCodespacesPlatformInfoGeneral } from 'vs-codespaces-authorization';

import { telemetryMarks } from '../../telemetry/telemetryMarks';
import { sendTelemetry } from '../../telemetry/telemetry';
import { authService } from '../../auth/authService';
import {
    CodespacePerformance,
    getMainCodespacePerformance,
} from '../../utils/performance/CodespacePerformance';
import { PerformanceEventIds } from '../../utils/performance/PerformanceEvents';
import { ITelementryStartupTimes, ITimeBlock } from '../../telemetry/sendTelemetry';

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
    if (!('vscodeSettings' in platformInfo) || !('homeIndicator' in platformInfo.vscodeSettings)) {
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

const getBlockTimings = (blockId: PerformanceEventIds): ITimeBlock => {
    return {
        startTime: CodespacePerformance.getBlockStartTime(blockId),
        duration: CodespacePerformance.getBlockDurationTime(blockId),
    };
}

const getStartupTimes = (): ITelementryStartupTimes => {
    const {
        getBlockStartTime,
        getBlockDurationTime,
        getBlockEndTime
    } = CodespacePerformance;

    // see `PerformanceEventIds` for more info on the events
    const result = {
        timeToJavascript: getBlockStartTime(PerformanceEventIds.Start),
        timeToTerminal: getBlockEndTime(PerformanceEventIds.InitTimeToRemoteExtensions),
        startCodespaceTime: getBlockDurationTime(PerformanceEventIds.StartCodespace),
        timeToVSCode: getBlockStartTime(PerformanceEventIds.VSCodeInitialization),
        getEnvironmentInfo1Time: getBlockDurationTime(PerformanceEventIds.GetEnvironmentInfo1),
        getEnvironmentInfo2Time: getBlockDurationTime(PerformanceEventIds.GetEnvironmentInfo2),
        getLiveshareWorkspaceInfo: getBlockDurationTime(PerformanceEventIds.GetLiveshareWorkspaceInfo),
        vscodeTime: getBlockTimings(PerformanceEventIds.VSCodeInitialization),
        workbenchComponentTime: getBlockTimings(PerformanceEventIds.WorkbenchComponent),
        workbenchPageTime: getBlockTimings(PerformanceEventIds.WorkbenchPage),
        workbenchPageInitTime: getBlockDurationTime(PerformanceEventIds.WorkbenchPageInitialization),
        pureConnectionTime: getBlockDurationTime(PerformanceEventIds.WorkbenchClientConnection),
        vscodeServerStartupTime: getBlockDurationTime(PerformanceEventIds.VSCodeServerStartup),
        clientServerHandshake: getBlockDurationTime(PerformanceEventIds.VSCodeClientServerHandshake),
    };

    return result;
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

            const codespacePerformance = getMainCodespacePerformance();
            codespacePerformance.markBlockEnd({
                id: PerformanceEventIds.InitTimeToRemoteExtensions,
                name: '',
            });

            sendTelemetry('vsonline/portal/startup-times', getStartupTimes());

            const [measure] = window.performance.getEntriesByName(telemetryMarks.timeToInteractive);
            sendTelemetry(`vsonline/portal/vscode-time-to-interactive`, {
                duration: measure.duration,
                hostedOn: getParentDomain(location.href),
            });
        },
    },
];
