import * as React from 'react';
import { TCodespaceInfo } from 'vso-client-core';

import { DevPanelSection } from './DevPanelSection';
import { vsoAPI } from '../../../api/vsoAPI';

interface IDevPanelSuspendSectionProps {
    codespaceInfo: TCodespaceInfo | null;
}

export const DevPanelSuspendSection: React.SFC<IDevPanelSuspendSectionProps> = ({
    codespaceInfo
}) => {
    if (!codespaceInfo) {
        return null;
    }

    const suspendCodespace = React.useCallback(async () => {
        if (!codespaceInfo || !('codespaceToken' in codespaceInfo)) {
            return null;
        }
        const codespace = await vsoAPI.getEnvironmentInfo(
            codespaceInfo.codespaceId,
            codespaceInfo.codespaceToken
        );
        await vsoAPI.suspendCodespace(codespace, codespaceInfo.codespaceToken);
    }, [codespaceInfo]);

    return (
        <DevPanelSection id={'dev-panel-suspend--section'} title={'Codespace Commands'}>
            <button
                className='vso-button vscs-dev-panel__input vscs-dev-panel__input--button'
                onClick={suspendCodespace}
            >
                😴&nbsp;&nbsp;Suspend
            </button>
        </DevPanelSection>
    );
};
