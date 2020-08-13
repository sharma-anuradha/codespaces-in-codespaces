import * as React from 'react';
import classnames from 'classnames';

import { TCodespaceInfo } from 'vso-client-core';

import { TEnvironment } from '../../../config/config';
import { DevPanelConnectSection } from './DevPanelConnectSection';
import { DevPanelHeader, LOADING_ENVIRONMENT_STAGE } from './DevPanelHeader';
import { DevPanelDefaultExtensionsSection } from './DevPanelDefaultExtensionsSection';
import { PLATFORM_REQUIRED_EXTENSIONS } from '../../../constants';
import { VSCodeExtension } from 'vs-codespaces-authorization';
import { DevPanelToggleComponent } from './DevPanelToggleComponent';
import { DevPanelSuspendSection } from './DevPanelSuspendSection';

import './DevPanel.css';

interface IDevPanelProps {
    id?: string;
    className?: string;
    codespaceInfo: TCodespaceInfo | null;
    environment: TEnvironment | typeof LOADING_ENVIRONMENT_STAGE;
    isDevPanel: boolean;
}

interface IDevPanelState {
    isClosed: boolean;
}

interface IMaybeDevPanelProps extends IDevPanelProps {
    isDevPanel: boolean;
}

export const MaybeDevPanel: React.FunctionComponent<IMaybeDevPanelProps> = (
    props: IMaybeDevPanelProps
) => {
    const { isDevPanel } = props;

    if (!isDevPanel) {
        return null;
    }

    const { id = 'vscs-dev-panel-ls-key' } = props;

    return <DevPanel {...props} id={id} />;
};

export class DevPanel extends DevPanelToggleComponent<IDevPanelProps, IDevPanelState> {
    public render() {
        const { environment, codespaceInfo, className } = this.props;
        const { isOn } = this.state;

        const buttonText = isOn ? '⬆' : '⬇';
        const buttonTitle = isOn ? 'collapse panel' : 'expand panel';

        const cls = classnames(className, {
            'is-hidden': !isOn,
        });

        const requiredExtensions: VSCodeExtension[] = PLATFORM_REQUIRED_EXTENSIONS.map((id) => {
            return { id };
        });

        return (
            <div className={`vscs-dev-panel ${cls}`}>
                <DevPanelHeader environment={environment} onClick={this.onToggle} />

                <div className='vscs-dev-panel__body'>
                    <DevPanelConnectSection codespaceInfo={codespaceInfo} />
                    <DevPanelDefaultExtensionsSection defaultExtensions={requiredExtensions} />
                    <DevPanelSuspendSection codespaceInfo={codespaceInfo} />
                </div>

                <div
                    className='vscs-dev-panel__toggle-button'
                    onClick={this.onToggle}
                    title={buttonTitle}
                >
                    {buttonText}
                </div>
            </div>
        );
    }
}
