import { PostMessageRepoInfoRetriever } from '../../split/github/postMessageRepoInfoRetriever';
import { sendTelemetry } from 'vso-workbench/src/telemetry/telemetry';
import { telemetryMarks } from 'vso-workbench/src/telemetry/telemetryMarks'
import { getTopLevelDomain } from '../../utils/getTopLevelDomain';

export enum commandIds {
    githubGoHome = '_github.gohome',
    codespacesTimeToInteractive = '_codespaces.timeToInteractive'
}

export const commands = [
    {
        id: commandIds.githubGoHome,
        handler: () => {
            PostMessageRepoInfoRetriever.sendMessage('vso-go-home');
        },
    },
    {
        id: commandIds.codespacesTimeToInteractive,
        handler: () => {
            window.performance.measure(telemetryMarks.timeToInteractive);
            const [measure] = window.performance.getEntriesByName(telemetryMarks.timeToInteractive);
            sendTelemetry(`vsonline/portal/vscode-time-to-interactive`, {
                duration: measure.duration,
                hostedOn: getTopLevelDomain(location.href),
            });
        },
    },
];