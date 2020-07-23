import * as Az from '@microsoft/azureportal-reactview/Az';
import * as React from 'react';
import { Fabric } from '@fluentui/react/lib/Fabric';
import * as ReactView from '@microsoft/azureportal-reactview/ReactView';
import * as Ajax from '@microsoft/azureportal-reactview/Ajax';
import { versionId } from '@microsoft/azureportal-reactview/major-version/1';
import { DefaultButton, PrimaryButton } from "@fluentui/react/lib/Button";
import { Spinner } from '@fluentui/react/lib/Spinner';
import {
    MessageBar,
    MessageBarType,
  } from '@fluentui/react/lib/MessageBar';

interface ComponentState {
    sessionId: string;
    isConnecting: boolean;
    errorMessage: string;
    connectParams: ConnectParams;
}

interface ConnectParams {
    parameters: {
        codespaceId: string;
        planId?: string;
        codespacesEndpoint?: string;
        armApiVersion?: string;
        encryptedGitToken?: string;
    };
}

type ArmList = {
    value: { id: string }[]
};

type TokenSearchStatus = {
    allPlansFound: boolean;
    allTokensFound: boolean;
    matchingTokenFound: boolean;
};

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
        this.state = {
            sessionId: '',
            errorMessage: '',
            isConnecting: true,
            connectParams: props,
        };
        Az.getSessionId().then((sessionId) => this.setState({ sessionId }));
        this.connectAndClose = this.connectAndClose.bind(this);
        this.closeBlade = this.closeBlade.bind(this);
    }

    public render() {
        return (
            <Fabric>
                {this.state.errorMessage &&
                    <MessageBar messageBarType={MessageBarType.error} isMultiline={false}>
                        {this.state.errorMessage}
                    </MessageBar>
                }
                {this.state.isConnecting &&
                    <Spinner label="Opening New Tab"/>
                }
                {!this.state.isConnecting && 
                    <div className="ms-Grid" dir="ltr" style={{maxWidth: '500px'}}>
                        <div className="ms-Grid-row">
                            <div className="ms-Grid-col ms-sm12 ms-lg6">
                                <h2>Not seeing the Codespace open in a new tab?</h2>
                                To prevent this from happening again, disable your pop-up blocker for this site.
                            </div>
                        </div>
                        
                        <div className="ms-Grid-row" style={{marginTop: '30px'}}>
                            <div className="ms-Grid-col ms-sm12 ms-lg6">
                                <DefaultButton onClick={this.closeBlade} style={{float: 'left'}}>Cancel</DefaultButton>
                                <PrimaryButton onClick={this.connectAndClose} style={{float: 'right'}}>Connect to Codespace</PrimaryButton>
                            </div>
                        </div>
                    </div>
                }
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
        this.setState({errorMessage: undefined, isConnecting: true});
        let githubToken: string;
        if (this.props.parameters.encryptedGitToken) {
            githubToken = await Ajax.ajax({
                type: 'POST',
                crossDomain: true,
                headers: {
                    'Content-Type': 'application/json',
                },
                uri: this.formatCodespacesUri('github-auth/decrypt-token'),
                data: JSON.stringify({
                    value: this.props.parameters.encryptedGitToken,
                }),
            });
        }

        const accessToken = await this.getCascadeToken().catch((errorMessage) => {
            this.setState({errorMessage, isConnecting: false});
            throw errorMessage;
        });
        const authUri = this.formatCodespacesUri(`platform-authentication?redirect=workspace/${this.props.parameters.codespaceId}`);
        const form = document.createElement('form');
        form.setAttribute('action', authUri);
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
                    token: githubToken,
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
        this.setState({isConnecting: false});
    }

    private getSubscriptions(): Promise<string[]> {
        return Ajax.ajax({
            setAuthorizationHeader: true,
            type: 'GET',
            uri: this.formatArmUri('subscriptions', '2020-01-01'),
        }).then(
            (subs: ArmList) => subs.value.map(({id}) => id)
        );
    }

    private async getCascadeToken(): Promise<string> {
        if (this.props.parameters.planId) {
            const { accessToken } = await Ajax.ajax({
                setAuthorizationHeader: true,
                type: 'POST',
                uri: this.formatArmUri(`${this.props.parameters.planId}/writeCodespaces`),
            });
            return accessToken;
        }

        const searchStatus: TokenSearchStatus = { allPlansFound: false, allTokensFound: false, matchingTokenFound: false };
        const subs = await this.getSubscriptions();
        const plans: string[] = [];
        const cascadeTokens: string[] = [];

        this.findPlans(subs, plans, searchStatus); // fire and forget
        this.findCascadeTokens(plans, cascadeTokens, searchStatus); // fire and forget
        return this.findMatchingCascadeToken(cascadeTokens, searchStatus);
    }
    
    private async findPlans(subscriptions: string[], plans: string[], searchStatus: TokenSearchStatus): Promise<void> {
        while (!searchStatus.matchingTokenFound) {
            if (subscriptions.length === 0) {
                break;
            }
            const batchSize = Math.min(20, subscriptions.length);
            const subBatch = subscriptions.splice(0, batchSize);
            const batchRequests: Common.Ajax.BatchRequest[] = subBatch.map((sub) => ({
                httpMethod: 'GET',
                uri: this.formatArmUri(`${sub}/providers/Microsoft.Codespaces/plans`)
            }));
            const batchResponse = await Ajax.batchMultiple({ batchRequests });
            batchResponse.responses.forEach(
                (element: {content: ArmList}) => element.content.value.forEach((plan) => plans.push(plan.id))
            );
        }
        searchStatus.allPlansFound = true;
    }
    
    private async findCascadeTokens(plans: string[], cascadeTokens: string[], searchStatus: TokenSearchStatus): Promise<void> {
        while (!searchStatus.matchingTokenFound) {
            if (plans.length === 0) {
                if (searchStatus.allPlansFound) {
                    break;
                }
                await new Promise((resolve) => setTimeout(resolve, 500));
                continue;
            }
            const batchSize = Math.min(20, plans.length);   
            const planBatch = plans.splice(0, batchSize);
            const batchRequests: Common.Ajax.BatchRequest[] = planBatch.map((planId) => ({
                httpMethod: 'POST',
                uri: this.formatArmUri(`${planId}/writeCodespaces`)
            }));
            const batchResponse = await Ajax.batchMultiple({ batchRequests });
            batchResponse.responses.forEach((r) => cascadeTokens.push(r.content.accessToken));
        }
        searchStatus.allTokensFound = true;
    }
    
    private async findMatchingCascadeToken(cascadeTokens: string[], searchStatus: TokenSearchStatus): Promise<string> {
        return new Promise(async (resolve, reject) => {
            // Helps us determine if there are checks in progress
            var inProgressCount = 0;

            while(!searchStatus.matchingTokenFound) {
                if (cascadeTokens.length === 0) {
                    if (searchStatus.allTokensFound && inProgressCount === 0) {
                        reject(`Could not determine plan for Codespace ${this.props.parameters.codespaceId}`);
                        return;
                    }
                    await new Promise((resolve) => setTimeout(resolve, 500));
                    continue;
                }

                inProgressCount++;
                const cascadeToken = cascadeTokens.pop();
                const uri = this.formatCodespacesUri(`api/v1/environments/${this.props.parameters.codespaceId}`);

                // fire-and-forget
                Ajax.ajax({
                    type: 'GET',
                    crossDomain: true,
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${cascadeToken}`
                    },
                    uri,
                }).then(() => {
                    searchStatus.matchingTokenFound = true;
                    resolve(cascadeToken);
                }).catch(() => inProgressCount--);
            }
        });
    }

    private formatCodespacesUri(path: string = ''): string {
        const endpoint = this.props.parameters.codespacesEndpoint || 'https://online.visualstudio.com/';
        return new URL(path, endpoint).href;
    }

    private formatArmUri(path: string = '', apiVersion: string = ''): string {
        apiVersion = apiVersion || this.props.parameters.armApiVersion || '2020-06-16';
        const endpoint = 'https://management.azure.com/';
        const url = new URL(path, endpoint);
        url.searchParams.set('api-version', apiVersion);
        return url.href;
    }
}
