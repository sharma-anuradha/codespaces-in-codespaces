import * as Az from '@microsoft/azureportal-reactview/Az';
import * as React from 'react';
import { Fabric } from '@fluentui/react/lib/Fabric';
import * as ReactView from '@microsoft/azureportal-reactview/ReactView';
import * as Ajax from '@microsoft/azureportal-reactview/Ajax';
import { versionId } from '@microsoft/azureportal-reactview/major-version/1';

interface ComponentState {
    sessionId: string;
    connectParams: ConnectParams;
}

interface ConnectParams {
    parameters: {
        planId: string;
        codespaceId: string;
        codespacesEndpoint?: string;
        armApiVersion?: string;
        encryptedGitToken?: string;
    };
}

/**
 * This is a view that does not have a corresponding model but instead keeps all of it's business login view side.
 * This is a good place to start for simpler expereriences where the increased complexity of having your business
 * logic live in a model is not necessary/needed for performance or simplicity.
 */
@ReactView.ReduxFree.Decorator<{}, ComponentState>({
    viewReady: () => true,
    versionId,
})
export class ModelFree extends React.Component<ConnectParams, ComponentState> {
    public constructor(props: ConnectParams) {
        super(props);
        Az.setTitle('Opening Codespace...');
        Az.getSessionId().then((sessionId) => this.setState({ sessionId }));
        this.connectAndClose = this.connectAndClose.bind(this);
        this.closeBlade = this.closeBlade.bind(this);
    }

    public render() {
        return (
            <Fabric>
                <div>
                    <h2>Not seeing the Codespace open in a new tab?</h2>
                    To prevent this from happening again, disable your pop-up blocker for this site.
                    <br />
                    <br />
                    <button onClick={this.connectAndClose}>Connect to Codespace</button>
                    <button onClick={this.closeBlade}>Cancel</button>
                </div>
            </Fabric>
        );
    }

    public componentDidMount() {
        this.connect();
        setTimeout(() => this.closeBlade(), 20 * 1000);
    }

    private closeBlade() {
        Az.closeCurrentBlade();
    }

    private connectAndClose() {
        this.connect().then(() => this.closeBlade());
    }

    private async connect() {
        let token: string;
        if (this.props.parameters.encryptedGitToken) {
            token = await Ajax.ajax({
                type: 'POST',
                crossDomain: true,
                headers: {
                    'Content-Type': 'application/json',
                },
                uri: `${this.getCodespacesEndpoint()}github-auth/decrypt-token`,
                data: JSON.stringify({
                    value: this.props.parameters.encryptedGitToken,
                }),
            });
        }

        return Ajax.ajax({
            setAuthorizationHeader: true,
            type: 'POST',
            uri: this.getArmUri(),
        }).then(({ accessToken }) => {
            const form = document.createElement('form');
            form.setAttribute('action', this.getCodespacesUri());
            form.setAttribute('method', 'POST');
            form.setAttribute('target', '_blank');

            const partnerInfo = JSON.stringify({
                partnerName: 'azureportal',
                managementPortalUrl: 'https://portal.azure.com',
                codespaceToken: accessToken,
                codespaceId: this.props.parameters.codespaceId,
                credentials: [
                    {
                        expiration: 10000000000000,
                        token: token,
                        host: 'github.com',
                        path: '/',
                    },
                ],
                vscodeSettings: {},
            });

            // set the Codespace token
            const codespaceTokenInput = document.createElement('input');
            codespaceTokenInput.name = 'codespaceToken';
            codespaceTokenInput.value = accessToken;

            // set the partner info
            const partnerInfoInput = document.createElement('input');
            partnerInfoInput.name = 'partnerInfo';
            partnerInfoInput.value = partnerInfo;

            // append the form to the DOM
            form.appendChild(codespaceTokenInput);
            form.appendChild(partnerInfoInput);
            document.body.append(form);

            // submit the form to initiate the top-level HTTP POST
            form.submit();
        });
    }

    private getArmUri(): string {
        const apiVersion = this.props.parameters.armApiVersion || '2019-07-01-preview';
        return `https://management.azure.com${this.props.parameters.planId}/writeCodespaces?api-version=${apiVersion}`;
    }

    private getCodespacesEndpoint() {
        var codespacesEndpoint =
            this.props.parameters.codespacesEndpoint || 'https://online.visualstudio.com/';
        if (codespacesEndpoint.substring(codespacesEndpoint.length - 1) !== '/') {
            codespacesEndpoint += '/';
        }
        return codespacesEndpoint;
    }

    private getCodespacesUri(): string {
        return `${this.getCodespacesEndpoint()}platform-authentication?redirect=workspace/${
            this.props.parameters.codespaceId
        }`;
    }
}
