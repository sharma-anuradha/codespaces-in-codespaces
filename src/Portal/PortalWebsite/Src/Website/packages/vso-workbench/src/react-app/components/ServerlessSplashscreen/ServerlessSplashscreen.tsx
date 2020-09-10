import React from 'react';

import { IEnvironment, ILocalEnvironment } from 'vso-client-core';
import { ICredentialsProvider } from 'vscode-web';

import { ServerlessWorkbench } from '../../../vscode/workbenches/serverlessWorkbench';
import { Spinner } from '../Spinner/Spinner';
import { GitHubApi } from 'vso-workbench/src/api/githubApi';

export interface IServerlessSplashscreenProps {
    environment: IEnvironment | ILocalEnvironment | null;
    credentialsProvider: ICredentialsProvider;
    getGithubToken?: () => Promise<string | null>;
}

export interface IServerlessSplashscreenState {
    headForPull: string;
    githubQueryDone: boolean;
}

export class ServerlessSplashscreen extends React.Component<
    IServerlessSplashscreenProps,
    IServerlessSplashscreenState
> {
    constructor(props: IServerlessSplashscreenProps, state: IServerlessSplashscreenState) {
        super(props, state);
        this.state = {
            headForPull: '',
            githubQueryDone: false,
        };
    }

    render() {
        const { environment, credentialsProvider } = this.props;
        const githubURLString = environment?.seed?.moniker;
        if (!environment || !githubURLString) {
            return <div>Invalid github seed</div>;
        }
        const githubURL = new URL(githubURLString);
        const [org, repository, sourceType, ...rest] = githubURL.pathname.substr(1).split('/');

        let branchOrCommit = '';
        if (sourceType === 'commit' || sourceType === 'tree') {
            branchOrCommit = '+' + rest.join('/');
        } else if (sourceType === 'pull' && rest.length > 0) {
            const head = this.getHeadForPullRequest(org, repository, rest[0]);
            if (!head) {
                return <Spinner className='loading-pull-request-spinner' />;
            }
            branchOrCommit = '+' + head;
        }
        const folderUri = `github://${org}+${repository}${branchOrCommit}/`;

        return (
            <ServerlessWorkbench folderUri={folderUri} credentialsProvider={credentialsProvider} />
        );
    }

    private getHeadForPullRequest(org: string, repository: string, prNumber: string) {
        if (this.state.headForPull) {
            return this.state.headForPull;
        } else {
            if (!this.state.githubQueryDone) {
                this.getHeadForPullRequestFromGithub(org, repository, prNumber);
            }
            return null;
        }
    }

    private async getHeadForPullRequestFromGithub(org: string, repository: string, prNumber: string) {
        this.setState({ githubQueryDone: true });
        let token = null;
        if (this.props.getGithubToken) {
            token = await this.props.getGithubToken();
        }
        const head = await GitHubApi.getHeadOfPullRequest(org, repository, prNumber, token);
        this.setState({ headForPull: head });
    }
}
