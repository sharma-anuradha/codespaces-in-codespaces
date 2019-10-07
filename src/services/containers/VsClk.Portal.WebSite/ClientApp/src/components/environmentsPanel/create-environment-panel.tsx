import React, { Component, FormEvent, KeyboardEvent, SyntheticEvent } from 'react';
import { PrimaryButton, DefaultButton } from 'office-ui-fabric-react/lib/Button';
import { Panel, PanelType } from 'office-ui-fabric-react/lib/Panel';
import { Stack } from 'office-ui-fabric-react/lib/Stack';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { KeyCodes } from '@uifabric/utilities';
import { GITHUB_BASE_URL, GITHUB_API_URL } from '../../constants';
import { useWebClient } from '../../actions/middleware/useWebClient';
import { getToken, getStoredGitHubToken } from '../../ts-agent/services/gitCredentialService';
import { Signal } from '../../utils/signal';

const validIconProp = { iconName: 'CheckMark' };

enum SupportedGitServices {
    Unknown,
    GitHub = 'github.com',
    BitBucket = 'bitbucket.org',
    GitLab = 'gitlab.com',
}

export interface CreateEnvironmentPanelProps {
    showPanel: boolean;
    hidePanel: () => void;
    onCreateEnvironment: (friendlyName: string, githubRepositoryUrl?: string) => void;
}

export interface CreateEnvironmentPanelState {
    environmentName?: string;
    gitRepositoryUrl?: string;
    normalizedGitHubUrl?: string;
    gitValidationErrorMessage?: string;
    gitValidationMessage?: string;
    gitHubAuthenticationUrl?: string;
    isGitUrlValid?: boolean;
    isEnvironmentNameValid?: boolean;
}

export class CreateEnvironmentPanel extends Component<
    CreateEnvironmentPanelProps,
    CreateEnvironmentPanelState
> {
    private timeout: ReturnType<typeof setTimeout> | undefined;

    private validationRequest: Signal<void> | undefined;

    private authenticationRequest: Signal<boolean> | undefined;

    public constructor(props: CreateEnvironmentPanelProps) {
        super(props);

        this.state = {};
    }

    componentWillUnmount() {
        if (this.timeout) {
            clearTimeout(this.timeout);
        }

        if (this.validationRequest) {
            this.validationRequest.cancel();
        }

        if (this.authenticationRequest) {
            this.authenticationRequest.cancel();
        }
    }

    render() {
        const repositoryUrlValue = this.state.gitRepositoryUrl || '';
        const environmentNameValue = this.state.environmentName || '';

        return (
            <Panel
                isOpen={this.props.showPanel}
                type={PanelType.smallFixedFar}
                isFooterAtBottom={true}
                onKeyDown={this.dismissPanel}
                onDismiss={this.props.hidePanel}
                headerText='Create Environment'
                closeButtonAriaLabel='Close'
                onRenderFooterContent={this.onRenderFooterContent}
            >
                <Stack tokens={{ childrenGap: 4 }}>
                    <TextField
                        label='Environment Name'
                        placeholder='environmentNameExample'
                        onKeyDown={this.submitForm}
                        value={environmentNameValue}
                        onChange={this.environmentNameChanged}
                        iconProps={this.state.isEnvironmentNameValid ? validIconProp : undefined}
                        autoFocus
                        required
                    />
                    <TextField
                        label='Git Repository'
                        placeholder='vsls-contrib/guestbook'
                        onKeyDown={this.submitForm}
                        value={repositoryUrlValue}
                        onChange={this.githubRepositoryUrlChanged}
                        errorMessage={this.state.gitValidationErrorMessage}
                        iconProps={this.state.isGitUrlValid ? validIconProp : undefined}
                    />
                    <label>{this.state.gitValidationMessage}</label>
                </Stack>
            </Panel>
        );
    }

    private onRenderFooterContent = () => {
        return (
            <>
                <PrimaryButton
                    onClick={this.createEnvironment}
                    style={{ marginRight: '.8rem' }}
                    disabled={!this.isCurrentStateValid()}
                >
                    {this.state.gitHubAuthenticationUrl ? 'Auth & Create' : 'Create'}
                </PrimaryButton>
                <DefaultButton onClick={this.clearForm}>Cancel</DefaultButton>
            </>
        );
    };

    submitForm = async (event: KeyboardEvent<HTMLInputElement | HTMLTextAreaElement>) => {
        if (event.keyCode === KeyCodes.enter) {
            await this.createEnvironment(event);
        }
    };

    dismissPanel: ((event: KeyboardEvent<any>) => void) | undefined = (event) => {
        if (event.keyCode === KeyCodes.escape) {
            this.clearForm();
        }
    };

    private createEnvironment = async (event: SyntheticEvent<any, any>) => {
        if (!this.isCurrentStateValid()) {
            event.preventDefault();
            event.stopPropagation();

            return;
        }

        const gitUrl = this.state.normalizedGitHubUrl
            ? this.state.normalizedGitHubUrl
            : this.state.gitRepositoryUrl;

        if (this.state.gitHubAuthenticationUrl) {
            if (this.authenticationRequest) {
                this.authenticationRequest.cancel();
            }

            this.authenticationRequest = Signal.from(
                this.authenticateGitHub(this.state.gitHubAuthenticationUrl)
            );
            if (await this.authenticationRequest.promise) {
                this.props.onCreateEnvironment(this.state.environmentName!, gitUrl);

                this.clearForm();
            } else {
                this.setState({
                    gitValidationErrorMessage: 'You do not have access to this repo.',
                });
            }

            this.authenticationRequest = undefined;
        } else {
            this.props.onCreateEnvironment(this.state.environmentName!, gitUrl);

            this.clearForm();
        }
    };

    private clearForm = () => {
        this.setState({
            environmentName: undefined,
            gitRepositoryUrl: undefined,
            gitValidationErrorMessage: undefined,
            gitValidationMessage: undefined,
            gitHubAuthenticationUrl: undefined,
            isGitUrlValid: undefined,
            isEnvironmentNameValid: undefined,
            normalizedGitHubUrl: undefined,
        });
        this.props.hidePanel();
    };

    private isCurrentStateValid() {
        let validationFailed = false;

        const environmentName = this.state.environmentName && this.state.environmentName.trim();
        validationFailed = validationFailed || !environmentName || environmentName.length === 0;

        if (validationFailed || this.state.gitValidationErrorMessage) {
            validationFailed = true;
        }

        return !validationFailed;
    }

    private environmentNameChanged: (
        event: FormEvent<HTMLInputElement | HTMLTextAreaElement>,
        environmentName?: string
    ) => void = (_event, environmentName) => {
        this.setState({
            environmentName,
        });

        if (environmentName) {
            this.setState({
                isEnvironmentNameValid: true,
            });
        } else {
            this.setState({
                isEnvironmentNameValid: undefined,
            });
        }
    };

    private githubRepositoryUrlChanged: (
        event: FormEvent<HTMLInputElement | HTMLTextAreaElement>,
        githubRepositoryUrl?: string
    ) => void = (_event, githubRepositoryUrl) => {
        this.setState({
            gitRepositoryUrl: githubRepositoryUrl,
            gitValidationErrorMessage: undefined,
            gitValidationMessage: undefined,
            gitHubAuthenticationUrl: undefined,
            isGitUrlValid: undefined,
            normalizedGitHubUrl: undefined,
        });

        if (this.validationRequest) {
            this.validationRequest.cancel();
        }

        if (this.timeout) {
            clearTimeout(this.timeout);
        }

        this.timeout = setTimeout(() => {
            if (githubRepositoryUrl) {
                githubRepositoryUrl = githubRepositoryUrl.trim();
                this.validationRequest = Signal.from(this.validateGitUrl(githubRepositoryUrl));
                this.validationRequest.promise;
                this.validationRequest = undefined;
            }
        }, 1000);
    };

    private async validateGitUrl(githubRepositoryUrl: string) {
        const matchTokens = this.getGitProvider(githubRepositoryUrl);
        if (matchTokens) {
            const gitProvider = matchTokens[0];
            switch (gitProvider) {
                case SupportedGitServices.BitBucket:
                case SupportedGitServices.GitLab:
                    if (await this.pingUrl(githubRepositoryUrl)) {
                        this.setState({
                            isGitUrlValid: true,
                        });
                    } else {
                        this.setState({
                            gitValidationErrorMessage:
                                'Unable to connect to this repo. Create an empty environment.',
                        });
                    }
                    break;
                case SupportedGitServices.GitHub:
                    const gitHubUrl = GITHUB_API_URL.concat(matchTokens[1]);
                    this.setState({
                        normalizedGitHubUrl: GITHUB_BASE_URL.concat(matchTokens[1]),
                    });
                    if (getStoredGitHubToken()) {
                        if (await this.authenticateGitHub(gitHubUrl)) {
                            this.setState({
                                isGitUrlValid: true,
                            });
                        } else {
                            this.setState({
                                gitValidationErrorMessage: 'You do not have access to this repo.',
                            });
                        }
                    } else {
                        if (await this.pingUrl(gitHubUrl)) {
                            this.setState({
                                isGitUrlValid: true,
                            });
                        } else {
                            this.setState({
                                gitValidationMessage: 'Private GitHub repo detected.',
                                gitHubAuthenticationUrl: gitHubUrl,
                            });
                        }
                    }
                    break;
                default:
                    this.setState({
                        gitValidationErrorMessage: 'Invalid url',
                    });
            }
        } else {
            this.setState({
                gitValidationErrorMessage: 'Invalid url',
            });
        }
    }

    private async authenticateGitHub(url: string): Promise<boolean> {
        const token = await getToken();
        if (token && url) {
            const webClient = useWebClient();

            try {
                await webClient.request(
                    url,
                    {
                        headers: {
                            Authorization: `Bearer ${token}`,
                        },
                    },
                    { skipParsingResponse: true }
                );
                return true;
            } catch {
                return false;
            }
        }

        return false;
    }

    public async pingUrl(url: string): Promise<boolean> {
        const webClient = useWebClient();
        try {
            await webClient.request(
                url,
                {},
                {
                    requiresAuthentication: false,
                    skipParsingResponse: true,
                }
            );
            return true;
        } catch {
            return false;
        }
    }

    private getGitProvider(repo: string): string[] | undefined {
        repo = this.convertShortUrlToFull(repo);
        const regex = /(https?:\/\/)?(www\.)?(github\.com|bitbucket\.org|gitlab\.com)?\b([-a-zA-Z0-9()@:%_\+.~#?&\/\/=]*)/g;
        const match = regex.exec(repo);
        if (match && match.length > 4 && match[3] && match[4]) {
            return [match[3], match[4]];
        } else {
            return undefined;
        }
    }

    private convertShortUrlToFull(repo: string): string {
        if (repo.startsWith('/')) {
            repo = repo.substr(1, repo.length);
        }
        if (repo.endsWith('/')) {
            repo = repo.substr(0, repo.length - 1);
        }
        const split = repo.split('/');
        if (split.length === 2) {
            repo = GITHUB_BASE_URL.concat('/').concat(repo);
        }

        repo = repo.toLowerCase();

        return repo;
    }
}
