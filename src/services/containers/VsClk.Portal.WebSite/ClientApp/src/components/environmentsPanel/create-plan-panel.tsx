import React, { Component, FormEvent, KeyboardEvent } from 'react';
import { connect } from 'react-redux';
import jwtDecode from 'jwt-decode';
import { PrimaryButton, DefaultButton } from 'office-ui-fabric-react/lib/Button';
import { Panel, PanelType } from 'office-ui-fabric-react/lib/Panel';
import { Stack } from 'office-ui-fabric-react/lib/Stack';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { IDropdownOption } from 'office-ui-fabric-react/lib/Dropdown';
import { KeyCodes } from '@uifabric/utilities';

import { DropDownWithLoader } from '../dropdown-with-loader/dropdown-with-loader';

import { Loader } from '../loader/loader';

import { getARMToken } from '../../services/authARMService';
import { authService } from '../../services/authService';
import { armAPIVersion } from '../../constants';
import { getPlans } from '../../actions/plans-actions';
import { ApplicationState } from '../../reducers/rootReducer';
import { ConfigurationState } from '../../reducers/configuration';
import { getLocations } from '../../actions/locations-actions';

import { IAzureSubscription } from '../../interfaces/IAzureSubscription';

export interface CreatePlanPanelProps {
    hidePanel: () => void;
    configuration: ConfigurationState;
}

export interface CreatePlanPanelState {
    planName?: string;
    subscriptionList: { key: string; text: string }[];
    resourceGroupList: { key: string; text: string }[];
    locationsList: { key: string; text: string }[];
    selectedSubscription?: string;
    selectedResourceGroup?: string;
    selectedRegion?: string;
    isCreatingPlan: boolean;
    isGettingSubscriptions: boolean;
    isGettingResourceGroups: boolean;
    isGettingClosestRegion: boolean;
}

export class CreatePlanPanelComponent extends Component<
    CreatePlanPanelProps,
    CreatePlanPanelState
> {
    public constructor(props: CreatePlanPanelProps) {
        super(props);

        this.state = {
            subscriptionList: [],
            resourceGroupList: [],
            locationsList: [],
            isCreatingPlan: false,
            isGettingSubscriptions: true,
            isGettingResourceGroups: true,
            isGettingClosestRegion: true,
        };
    }

    componentDidMount() {
        this.getSubscriptions();
        this.getClosestRegion();
    }

    render() {
        const {
            isGettingSubscriptions,
            isGettingResourceGroups,
            isGettingClosestRegion,
            subscriptionList,
            resourceGroupList,
            locationsList,
            selectedSubscription,
            selectedResourceGroup,
            selectedRegion,
        } = this.state;

        return (
            <Panel
                isOpen={true}
                type={PanelType.smallFixedFar}
                headerText='Create a Plan'
                closeButtonAriaLabel='Close'
                onKeyDown={this.dismissPanel}
                onDismiss={this.props.hidePanel}
                onRenderFooterContent={this.onRenderFooterContent}
                className='create-environment-panel'
            >
                <Stack>
                    {this.renderOverlay()}

                    <DropDownWithLoader
                        label='Subscription'
                        onChange={this.subscriptionChanged}
                        options={subscriptionList}
                        isLoading={isGettingSubscriptions}
                        loadingMessage='Fetching your subscriptions...'
                        selectedKey={selectedSubscription}
                        className='create-environment-panel__dropdown'
                    />

                    <DropDownWithLoader
                        label='Resource Group'
                        onChange={this.resourceGroupChanged}
                        options={resourceGroupList}
                        disabled={!selectedSubscription}
                        isLoading={isGettingResourceGroups}
                        loadingMessage='Fetching your resource groups...'
                        selectedKey={selectedResourceGroup}
                        className='create-environment-panel__dropdown'
                    />

                    <DropDownWithLoader
                        label='Region'
                        onChange={this.regionChanged}
                        options={locationsList}
                        loadingMessage='Fetching the closest region...'
                        isLoading={isGettingClosestRegion}
                        selectedKey={selectedRegion}
                        className='create-environment-panel__dropdown'
                    />

                    <TextField
                        label='Plan Name'
                        placeholder='My plan'
                        onChange={this.planNameChanged}
                        value={this.state.planName}
                    />
                </Stack>
            </Panel>
        );
    }

    private renderOverlay() {
        const { isCreatingPlan, planName } = this.state;

        if (!isCreatingPlan || !planName) {
            return null;
        }

        return (
            <div className='create-environment-panel__overlay'>
                <Loader message='Creating the plan...' />
            </div>
        );
    }

    private onRenderFooterContent = () => {
        return (
            <>
                <PrimaryButton
                    onClick={this.createPlan}
                    style={{ marginRight: '.8rem' }}
                    disabled={!this.isCurrentStateValid()}
                >
                    Create
                </PrimaryButton>
                <DefaultButton onClick={this.clearForm}>Cancel</DefaultButton>
            </>
        );
    };

    dismissPanel: ((event: KeyboardEvent<any>) => void) | undefined = (event) => {
        if (event.keyCode === KeyCodes.escape) {
            this.clearForm();
        }
    };

    private createPlan = async () => {
        if (!this.isCurrentStateValid()) {
            return;
        }
        const userID = await this.getUserID();

        const data = {
            location: this.state.selectedRegion,
            tags: {},
            properties: {
                userId: userID,
            },
        };

        const { selectedResourceGroup, planName } = this.state;

        const url =
            'https://management.azure.com' +
            selectedResourceGroup + // NOTE: Resource group id contains subscription id
            '/providers/Microsoft.VSOnline/plans/' +
            planName +
            '?api-version=' +
            this.getAPIVersion();

        const myAuthToken = await getARMToken(60);
        if (myAuthToken) {
            try {
                this.setState({ isCreatingPlan: true });

                await fetch(url, {
                    method: 'PUT',
                    body: JSON.stringify(data),
                    headers: {
                        authorization: `Bearer ${myAuthToken.accessToken}`,
                        'Content-Type': 'application/json',
                    },
                });
            } catch (e) {
                throw e;
            } finally {
                this.setState({ isCreatingPlan: false });
            }

            getPlans();

            this.props.hidePanel();
        }
    };

    private clearForm = () => {
        this.setState({ planName: undefined });
        this.props.hidePanel();
    };

    private async getUserID() {
        let tokenString = await authService.getCachedToken();
        const jwtToken = jwtDecode(tokenString!.accessToken) as { oid: string; tid: string };
        return `${jwtToken.tid}_${jwtToken.oid}`;
    }

    /**
     * API Version	Endpoint URL
     * 2019-07-01-preview	online.visualstudio.com/api/v1
     * 2019-07-01-beta	    online-ppe.vsengsaas.visualstudio.com/api/v1
     * 2019-07-01-alpha	    online.dev.vsengsaas.visualstudio.com/api/v1
     */
    public getAPIVersion() {
        const baseURL = window.location.href.split('/')[2];
        let apiVersion;
        if (baseURL.includes('dev')) {
            apiVersion = '2019-07-01-alpha';
        } else if (baseURL.includes('ppe')) {
            apiVersion = '2019-07-01-beta';
        } else {
            apiVersion = '2019-07-01-preview';
        }
        return apiVersion;
    }

    private async getFromAzure(url: string) {
        const myAuthToken = await getARMToken(60);
        if (myAuthToken) {
            const authToken = 'Bearer ' + myAuthToken.accessToken;
            let response = await fetch(url, {
                headers: {
                    authorization: authToken,
                },
            });

            return response.json();
        }
    }

    private async getSubscriptions() {
        try {
            this.setState({
                isGettingSubscriptions: true,
            });

            const myJson = await this.getFromAzure(
                `https://management.azure.com/subscriptions?api-version=${armAPIVersion}`
            );

            const subscriptionsJsonList = myJson.value.map((sub: IAzureSubscription) => {
                return {
                    key: sub.subscriptionId,
                    text: sub.displayName,
                };
            });

            this.setState({
                subscriptionList: subscriptionsJsonList,
            });

            // if no subscription selected, select the first one by default
            const defaultSubscription = subscriptionsJsonList[0];
            if (defaultSubscription && !this.state.selectedSubscription) {
                this.subscriptionChanged({}, defaultSubscription);
            }
        } catch (e) {
            throw e;
        } finally {
            this.setState({ isGettingSubscriptions: false });
        }
    }

    private async getClosestRegion() {
        try {
            const { configuration } = this.props;

            if (!configuration) {
                return;
            }

            this.setState({ isGettingClosestRegion: true });

            const locations = await getLocations();
            const closestRegion: string = locations.current;

            const { selectedRegion } = this.state;

            this.setState({
                selectedRegion: selectedRegion || closestRegion,
                locationsList: locations.available.map((l) => {
                    return {
                        key: l,
                        text: l,
                    };
                }),
            });
        } catch {
            // ignore
        } finally {
            this.setState({ isGettingClosestRegion: false });
        }
    }

    private async getResourceGroups(subID: string) {
        if (this.state.selectedSubscription !== undefined) {
            try {
                this.setState({
                    isGettingResourceGroups: true,
                    resourceGroupList: [],
                    selectedResourceGroup: undefined,
                });
                const rgURL = `https://management.azure.com/subscriptions/${subID}/resourcegroups?api-version=${armAPIVersion}`;

                const myJson = await this.getFromAzure(rgURL);
                let resourceGroupJsonList = [];

                for (let resGroup of myJson.value) {
                    resourceGroupJsonList.push({ key: resGroup.id, text: resGroup.name });
                }
                this.setState({
                    resourceGroupList: resourceGroupJsonList,
                });

                if (!this.state.selectedResourceGroup && resourceGroupJsonList.length) {
                    const defaultResourceGroup = resourceGroupJsonList[0];
                    this.resourceGroupChanged({}, defaultResourceGroup);
                }
            } catch (e) {
                throw e;
            } finally {
                this.setState({ isGettingResourceGroups: false });
            }
        }
    }

    private isCurrentStateValid() {
        let isInvalid = false;

        const planName = this.state.planName && this.state.planName.trim();
        isInvalid = isInvalid || !planName || planName.length === 0;

        if (
            !this.state.selectedResourceGroup ||
            !this.state.selectedRegion ||
            !this.state.selectedSubscription
        ) {
            isInvalid = true;
        }
        return !isInvalid;
    }

    // Functions that handle dropdown change events

    private planNameChanged: (
        event: FormEvent<HTMLInputElement | HTMLTextAreaElement>,
        planName?: string
    ) => void = (_event, planName) => {
        this.setState({
            planName,
        });
    };

    private subscriptionChanged: (_: any, option?: IDropdownOption, index?: number) => void = (
        _e,
        option
    ) => {
        if (!option) {
            return;
        }

        this.setState({
            selectedSubscription: option.key as string,
        });

        this.getResourceGroups(option.key as string);

        return;
    };

    private resourceGroupChanged: (_: any, option?: IDropdownOption, index?: number) => void = (
        _e,
        option,
        index
    ) => {
        if (!option) {
            return;
        }

        this.setState({
            selectedResourceGroup: option.key.toString(), // option.key has type string | number by default
        });
    };

    private regionChanged: (
        event: FormEvent<HTMLDivElement>,
        option?: IDropdownOption,
        index?: number
    ) => void = (_e, option) => {
        if (!option) {
            return;
        }

        this.setState({
            selectedRegion: option.key.toString(), // option.key has type string | number by default
        });
    };
}

export const CreatePlanPanel = connect(({ configuration }: ApplicationState) => ({
    configuration,
}))(CreatePlanPanelComponent);
