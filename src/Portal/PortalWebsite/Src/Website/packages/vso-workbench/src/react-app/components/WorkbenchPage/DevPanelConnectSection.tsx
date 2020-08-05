import * as React from 'react';
import { TCodespaceInfo } from 'vso-client-core';

import { DevPanelSection } from './DevPanelSection';

interface IDevPanelConnectSectionProps {
    codespaceInfo: TCodespaceInfo | null;
}

interface IDevPanelConnectSectionState {
    endpoint: string;
    isNewTab: boolean;
}

const ROOT_PATH = '/';

export class DevPanelConnectSection extends React.Component<
    IDevPanelConnectSectionProps,
    IDevPanelConnectSectionState
> {
    constructor(props: IDevPanelConnectSectionProps) {
        super(props);

        this.state = {
            endpoint: ROOT_PATH,
            isNewTab: true,
        };
    }

    private onEndpointChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const { value = ROOT_PATH } = e.target;

        this.setState({
            endpoint: value.trim(),
        });
    };

    private onTargetChanged = () => {
        this.setState({
            isNewTab: !this.state.isNewTab,
        });
    };

    public render() {
        const { codespaceInfo } = this.props;

        if (!codespaceInfo || !('codespaceToken' in codespaceInfo)) {
            return null;
        }

        const { codespaceToken } = codespaceInfo;
        const { isNewTab, endpoint } = this.state;

        const target = isNewTab ? '_blank' : undefined;

        return (
            <DevPanelSection id={'dev-panel-connect-section'} title={'Connect to Codespace'}>
                <form action={endpoint || ROOT_PATH} method='POST' target={target}>
                    <p>
                        <label htmlFor='codespace-connection'>Connection endpoint:</label>
                    </p>
                    <input
                        type='text'
                        className='vscs-dev-panel__input'
                        name='codespace-connection'
                        placeholder='Codespace endpoint'
                        onChange={this.onEndpointChange}
                        value={this.state.endpoint}
                    />
                    <p>
                        <input
                            id='codespace-connection-new-tab'
                            type='checkbox'
                            className='vscs-dev-panel__input'
                            onChange={this.onTargetChanged}
                            checked={isNewTab}
                        />
                        <label htmlFor='codespace-connection-new-tab'>into new tab</label>{' '}
                    </p>
                    <input
                        type='text'
                        name='cascadeToken'
                        defaultValue={codespaceToken}
                        hidden={true}
                    />
                    <input
                        type='text'
                        name='partnerInfo'
                        defaultValue={JSON.stringify(codespaceInfo)}
                        hidden={true}
                    />
                    <p className='vscs-dev-panel-section__footer'>
                        <input
                            className='vso-button vscs-dev-panel__input vscs-dev-panel__input--button'
                            type='submit'
                            value='Submit'
                        />
                    </p>
                </form>
            </DevPanelSection>
        );
    }
}
