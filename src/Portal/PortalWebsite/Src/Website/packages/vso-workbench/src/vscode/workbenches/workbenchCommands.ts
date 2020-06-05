import { telemetryMarks } from "../../telemetry/telemetryMarks";
import { sendTelemetry } from "../../telemetry/telemetry";
import { getTopLevelDomain } from 'vso-client-core';

export enum commandIds {
    codespacesTimeToInteractive = '_codespaces.timeToInteractive'
}

export const commands = [
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