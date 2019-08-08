import React, { Component, FormEvent, MouseEvent } from 'react';
import {
    BaseButton,
    Button,
    PrimaryButton,
    DefaultButton,
} from 'office-ui-fabric-react/lib/Button';
import { Panel, PanelType } from 'office-ui-fabric-react/lib/Panel';
import { Stack } from 'office-ui-fabric-react/lib/Stack';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { Dropdown, IDropdownOption } from 'office-ui-fabric-react/lib/Dropdown';

enum EnvironmentType {
    Empty = 'empty',
    GitHub = 'github',
}

export interface CreateEnvironmentPanelProps {
    showPanel: boolean;
    hidePanel: () => void;
    onCreateEnvironment: (friendlyName: string, githubRepositoryUrl?: string) => void;
}

export interface CreateEnvironmentPanelState {
    environmentName?: string;
    selectedEnvironmentType: string;
    githubRepositoryUrl?: string;
    githubRepositoryUrlValidationMessage?: string;
}

export class CreateEnvironmentPanel extends Component<
    CreateEnvironmentPanelProps,
    CreateEnvironmentPanelState
> {
    private githubUrlValidationPending: boolean = false;

    public constructor(props: CreateEnvironmentPanelProps) {
        super(props);

        this.state = {
            selectedEnvironmentType: EnvironmentType.Empty,
        };
    }

    render() {
        const repositoryUrlValue = this.state.githubRepositoryUrl || '';
        const environmentNameValue = this.state.environmentName || '';

        return (
            <Panel
                isOpen={this.props.showPanel}
                type={PanelType.smallFixedFar}
                isFooterAtBottom={true}
                onDismiss={this.props.hidePanel}
                headerText='Create Environment'
                closeButtonAriaLabel='Close'
                onRenderFooterContent={this.onRenderFooterContent}
            >
                <Stack>
                    <TextField
                        label='Environment Name'
                        placeholder='environmentNameExample'
                        value={environmentNameValue}
                        autoFocus
                        required
                        onChange={this.environmentNameChanged}
                    />
                    <Dropdown
                        label='Environment Type'
                        selectedKey={this.state.selectedEnvironmentType}
                        onChange={this.envTypeSelectionChanged}
                        options={[
                            { key: EnvironmentType.Empty, text: 'Empty' },
                            { key: EnvironmentType.GitHub, text: 'GitHub' },
                        ]}
                    />

                    {this.state.selectedEnvironmentType === EnvironmentType.GitHub && (
                        <TextField
                            label='GitHub Repository'
                            value={repositoryUrlValue}
                            onChange={this.githubRepositoryUrlChanged}
                            errorMessage={this.state.githubRepositoryUrlValidationMessage}
                            placeholder='vsls-contrib/guestbook'
                        />
                    )}
                </Stack>
            </Panel>
        );
    }

    private onRenderFooterContent = () => {
        return (
            <div>
                <PrimaryButton
                    onClick={this.createEnvironment}
                    style={{ marginRight: '.8rem' }}
                    disabled={this.isCurrentStateValid()}
                >
                    Create
                </PrimaryButton>
                <DefaultButton onClick={this.clearForm}>Cancel</DefaultButton>
            </div>
        );
    };

    private createEnvironment: (
        event: MouseEvent<
            HTMLButtonElement | HTMLAnchorElement | HTMLDivElement | BaseButton | Button
        >
    ) => void = (event) => {
        if (this.isCurrentStateValid()) {
            event.preventDefault();
            event.stopPropagation();

            return;
        }

        const githubRepositoryUrl = this.getNormalizedGithubRepositoryUrl();

        // The environmentName will be always set here
        this.props.onCreateEnvironment(this.state.environmentName!, githubRepositoryUrl);

        this.clearForm();
    };

    private getNormalizedGithubRepositoryUrl(): string | undefined {
        if (this.state.selectedEnvironmentType !== EnvironmentType.GitHub) {
            return undefined;
        }

        if (!this.state.githubRepositoryUrl) {
            return undefined;
        }

        const githubUrl = 'https://github.com/';
        let repo = this.state.githubRepositoryUrl;
        if (repo.indexOf(githubUrl) === -1) {
            if (repo.startsWith('/')) repo = repo.substr(1, repo.length);
            repo = githubUrl.concat(repo);
        }

        return repo;
    }

    private clearForm = () => {
        this.setState({
            environmentName: undefined,
            selectedEnvironmentType: EnvironmentType.Empty,
            githubRepositoryUrl: undefined,
            githubRepositoryUrlValidationMessage: undefined,
        });
        this.props.hidePanel();
    };

    private envTypeSelectionChanged: (
        event: FormEvent<HTMLDivElement>,
        option?: IDropdownOption,
        index?: number
    ) => void = (_e, option) => {
        if (!option) {
            return;
        }
        switch (option.key) {
            case EnvironmentType.Empty:
            case EnvironmentType.GitHub:
                this.setState({
                    selectedEnvironmentType: option.key,
                });
                break;
            default:
                break;
        }
    };

    private isCurrentStateValid() {
        let validationFailed = false;

        const environmentName = this.state.environmentName && this.state.environmentName.trim();
        validationFailed = validationFailed || !environmentName || environmentName.length === 0;

        if (this.state.selectedEnvironmentType === EnvironmentType.GitHub) {
            if (
                this.state.githubRepositoryUrlValidationMessage ||
                !this.state.githubRepositoryUrl ||
                this.githubUrlValidationPending ||
                this.state.githubRepositoryUrl.length === 0
            ) {
                validationFailed = true;
            }
        }

        return validationFailed;
    }

    private environmentNameChanged: (
        event: FormEvent<HTMLInputElement | HTMLTextAreaElement>,
        environmentName?: string
    ) => void = (_event, environmentName) => {
        this.setState({
            environmentName,
        });
    };

    private githubRepositoryUrlChanged: (
        event: FormEvent<HTMLInputElement | HTMLTextAreaElement>,
        githubRepositoryUrl?: string
    ) => void = (_event, githubRepositoryUrl) => {
        this.setState({
            githubRepositoryUrl,
        });
    };
}
