import * as React from 'react';

import { DevPanelSection } from './DevPanelSection';
import {
    getGitHubApiEndpoint,
    isValidGithubApiEndpoint,
    setGitHubApiEndpoint,
} from '../../../utils/getGithubApiEndpoint';
import { isGithubTLD } from 'vso-client-core';

interface IDevPanelGitHubApiSectionProps {}

interface IDevPanelGitHubApiSectionState {
    currentEndpoint: string;
    endpoint: string;
}

export class DevPanelGitHubApiSection extends React.Component<
    IDevPanelGitHubApiSectionProps,
    IDevPanelGitHubApiSectionState
> {
    constructor(props: IDevPanelGitHubApiSectionProps) {
        super(props);

        this.state = {
            endpoint: '',
            currentEndpoint: '',
        };
    }

    public async componentDidMount() {
        const endpoint = await getGitHubApiEndpoint();

        this.setState({
            endpoint,
            currentEndpoint: endpoint,
        });
    }

    private onEndpointChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
        const { value: endpoint } = e.target;

        const currentEndpoint = await getGitHubApiEndpoint();
        const endpointToSet = (isValidGithubApiEndpoint(endpoint))
            ? endpoint
            : currentEndpoint;

        this.setState({
            endpoint: endpointToSet,
            currentEndpoint: currentEndpoint,
        });
    };

    private onSubmit = async (e: React.KeyboardEvent<HTMLInputElement>) => {
        if (e.keyCode !== 13) {
            return;
        }

        await this.saveGithubEndpoint();
    }

    private saveGithubEndpoint = async () => {
        const { endpoint } = this.state;

        if (!isValidGithubApiEndpoint(endpoint)) {
            return;
        }

        await setGitHubApiEndpoint(endpoint);

        this.setState({
            currentEndpoint: endpoint,
        });
    }

    public render() {
        // don't render the panel if not on GitHub
        if (!(isGithubTLD(location.href))) {
            return null;
        }

        const {
            endpoint,
            currentEndpoint,
        } = this.state;

        const isSaveEnabled = isValidGithubApiEndpoint(endpoint) && (endpoint !== currentEndpoint);

        return (
            <DevPanelSection id={'dev-panel-github-api-section'} title={'GitHub API Endpoint'}>
                <input
                    type='text'
                    className='vscs-dev-panel__input'
                    name='codespace-github-api'
                    placeholder='Codespace endpoint'
                    onChange={this.onEndpointChange}
                    onKeyUp={this.onSubmit}
                    defaultValue={this.state.endpoint}
                />

                <pre className='vscs-dev-panel__current-github-api-endpoint'>
                    🔌 {currentEndpoint}
                </pre>

                <p className='vscs-dev-panel-section__footer'>
                    <button
                        className='vso-button vscs-dev-panel__input vscs-dev-panel__input--button'
                        onClick={this.saveGithubEndpoint}
                        disabled={!isSaveEnabled}
                    >
                        Apply
                    </button>
                </p>
            </DevPanelSection>
        );
    }
}
