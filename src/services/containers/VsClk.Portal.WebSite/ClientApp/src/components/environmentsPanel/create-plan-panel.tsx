import React, { Component, FormEvent, KeyboardEvent } from 'react';
import { connect } from 'react-redux';
import jwtDecode from 'jwt-decode';
import { PrimaryButton, DefaultButton } from 'office-ui-fabric-react/lib/Button';
import { Panel, PanelType } from 'office-ui-fabric-react/lib/Panel';
import { Stack } from 'office-ui-fabric-react/lib/Stack';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { IDropdownOption } from 'office-ui-fabric-react/lib/Dropdown';
import { ComboBox, IComboBoxOption, IComboBoxProps } from 'office-ui-fabric-react/lib/ComboBox';
import { KeyCodes } from '@uifabric/utilities';

import { DropDownWithLoader } from '../dropdown-with-loader/dropdown-with-loader';

import { Loader } from '../loader/loader';

import { authService } from '../../services/authService';
import { armAPIVersion } from '../../constants';
import { getPlans } from '../../actions/plans-actions';
import { ApplicationState } from '../../reducers/rootReducer';
import { ConfigurationState } from '../../reducers/configuration';
import { getLocations } from '../../actions/locations-actions';

import { IAzureSubscription } from '../../interfaces/IAzureSubscription';
import { Collapsible } from '../collapsible/collapsible';
import { createUniqueId } from '../../dependencies';
import { isDefined } from '../../utils/isDefined';
import { isNotNullOrEmpty } from '../../utils/isNotNullOrEmpty';

export interface CreatePlanPanelProps {
    hidePanel: (canContinueToEnvironment?: boolean) => void;
    configuration: ConfigurationState;
}

interface IStringOption {
    key: string;
    text: string;
}

interface INewGroupOption extends IStringOption {
    isNewGroup?: boolean;
}

const asc = (a: IStringOption, b: IStringOption) => (a.text > b.text ? 1 : -1);

export interface CreatePlanPanelState {
    userSpecifiedPlanName?: string;
    subscriptionList: IStringOption[];
    resourceGroupList: INewGroupOption[];
    locationsList: IStringOption[];
    selectedSubscription?: string;
    newGroup?: string;
    userSelectedResourceGroup?: string;
    selectedLocation?: string;
    isCreatingPlan: boolean;
    isGettingSubscriptions: boolean;
    isGettingResourceGroups: boolean;
    isGettingClosestLocation: boolean;
}

function locationToDisplayName(location: string) {
    switch (location) {
        case 'EastUs':
            return 'East US';
        case 'SouthEastAsia':
            return 'Southeast Asia';
        case 'WestEurope':
            return 'West Europe';
        case 'WestUs2':
            return 'West US 2';

        default:
            return location;
    }
}

export class CreatePlanPanelComponent extends Component<
    CreatePlanPanelProps,
    CreatePlanPanelState
> {
    public constructor(props: CreatePlanPanelProps) {
        super(props);

        this.state = {
            subscriptionList: [],
            newGroup: `vso-rg-${createUniqueId().substr(0, 7)}`,
            resourceGroupList: [],
            locationsList: [],
            isCreatingPlan: false,
            isGettingSubscriptions: true,
            isGettingResourceGroups: true,
            isGettingClosestLocation: true,
        };
    }

    componentDidMount() {
        this.getSubscriptions();
        this.getClosestLocation();
    }

    render() {
        const {
            isGettingSubscriptions,
            isGettingClosestLocation,
            subscriptionList,
            resourceGroupList,
            locationsList,
            selectedSubscription,
            selectedLocation,
            newGroup,
        } = this.state;

        const resourceGroupDescription = isDefined(newGroup)
            ? 'We will create a new resource group for you automatically.'
            : null;

        return (
            <Panel
                isOpen={true}
                type={PanelType.smallFixedFar}
                headerText='Create a Billing Plan'
                closeButtonAriaLabel='Close'
                onKeyDown={this.dismissPanel}
                onDismiss={this.onDismissPanel}
                isFooterAtBottom={true}
                onRenderFooterContent={this.onRenderFooterContent}
                className='create-environment-panel'
            >
                <Stack tokens={{ childrenGap: 'l1' }}>
                    {this.renderOverlay()}

                    <Stack tokens={{ childrenGap: 4 }}>
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
                            label='Location'
                            onChange={this.locationChanged}
                            options={locationsList}
                            loadingMessage='Determining the closest location...'
                            isLoading={isGettingClosestLocation}
                            selectedKey={selectedLocation}
                            className='create-environment-panel__dropdown'
                        />
                    </Stack>

                    <Collapsible tokens={{ childrenGap: 4 }} title='Advanced Options'>
                        <TextField
                            label='Plan Name'
                            required
                            placeholder='My plan'
                            onChange={this.planNameChanged}
                            value={this.planName}
                        />
                        <ComboBox
                            label='Resource Group'
                            onChange={this.resourceGroupChanged}
                            options={resourceGroupList}
                            onResolveOptions={this.resolveResourceGroups}
                            disabled={!selectedSubscription}
                            allowFreeform
                            autoComplete='on'
                            useComboBoxAsMenuWidth={true}
                            selectedKey={this.selectedResourceGroup}
                            text={newGroup}
                            className='create-environment-panel__dropdown'
                        />
                        {resourceGroupDescription}
                    </Collapsible>
                </Stack>
            </Panel>
        );
    }

    private renderOverlay() {
        const { isCreatingPlan } = this.state;

        if (!isCreatingPlan || !this.planName) {
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

    private onDismissPanel = () => {
        this.props.hidePanel();
    };

    private createNewResourceGroup = async (accessToken: string): Promise<string> => {
        if (!this.state.selectedSubscription) {
            throw new Error('NoSubscription');
        }
        if (!this.selectedResourceGroup) {
            throw new Error('NoResourceGroup');
        }
        if (!this.state.selectedLocation) {
            throw new Error('NoLocation');
        }

        const subscriptionId = this.state.selectedSubscription;
        const selectedResourceGroup = this.selectedResourceGroup;

        const url = new URL(
            `/subscriptions/${subscriptionId}/resourcegroups/${selectedResourceGroup}`,
            'https://management.azure.com'
        );
        url.searchParams.set('api-version', armAPIVersion);

        const resourceGroupExistsResponse = await fetch(url.toString(), {
            method: 'HEAD',
            headers: {
                authorization: `Bearer ${accessToken}`,
                'Content-Type': 'application/json',
            },
        });

        if (resourceGroupExistsResponse.status !== 404) {
            throw new Error('ResourceGroupExists');
        }

        const createResourceGroupResponse = await fetch(url.toString(), {
            method: 'PUT',
            body: JSON.stringify({
                location: this.state.selectedLocation,
            }),
            headers: {
                authorization: `Bearer ${accessToken}`,
                'Content-Type': 'application/json',
            },
        });

        if (
            createResourceGroupResponse.status !== 200 &&
            createResourceGroupResponse.status !== 201
        ) {
            throw new Error('FailedToCreateResourceGroup');
        }

        const responseData = await createResourceGroupResponse.json();

        return responseData.id;
    };

    private createPlan = async () => {
        if (!this.isCurrentStateValid()) {
            return;
        }

        try {
            this.setState({ isCreatingPlan: true });

            const userID = await this.getUserID();
            const myAuthToken = await authService.getARMToken(60);
            if (!myAuthToken) {
                return;
            }

            const data = {
                location: this.state.selectedLocation,
                tags: {},
                properties: {
                    userId: userID,
                },
            };

            let resourceGroupPath: string;
            if (this.selectedResourceGroup === this.state.newGroup) {
                resourceGroupPath = await this.createNewResourceGroup(myAuthToken.accessToken);
            } else if (isDefined(this.selectedResourceGroup)) {
                resourceGroupPath = this.selectedResourceGroup;
            } else {
                throw new Error('Cannot create plan without a group.');
            }

            const url = new URL(
                `${resourceGroupPath}/providers/Microsoft.VSOnline/plans/${this.planName}`,
                'https://management.azure.com'
            );
            url.searchParams.set('api-version', this.getAPIVersion());

            await fetch(url.toString(), {
                method: 'PUT',
                body: JSON.stringify(data),
                headers: {
                    authorization: `Bearer ${myAuthToken.accessToken}`,
                    'Content-Type': 'application/json',
                },
            });

            await getPlans();
            this.props.hidePanel(true);
        } catch (e) {
            this.setState({ isCreatingPlan: false });

            throw e;
        }
    };

    private clearForm = () => {
        this.setState({ userSpecifiedPlanName: undefined });
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

    private async getFromAzure(url: string){
        const myAuthToken = await authService.getARMToken(60);
        
        if(myAuthToken){
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

            const subscriptionList: IStringOption[] = myJson.value.map(
                (sub: IAzureSubscription) => {
                    return {
                        key: sub.subscriptionId,
                        text: sub.displayName,
                    };
                }
            );

            this.setState({
                subscriptionList: subscriptionList.sort(asc),
            });

            // if no subscription selected, select the first one by default
            const defaultSubscription = subscriptionList[0];
            if (defaultSubscription && !this.state.selectedSubscription) {
                this.subscriptionChanged({}, defaultSubscription);
            }
        } finally {
            this.setState({ isGettingSubscriptions: false });
        }
    }

    private async getClosestLocation() {
        try {
            const { configuration } = this.props;

            if (!configuration) {
                return;
            }

            this.setState({ isGettingClosestLocation: true });

            const locations = await getLocations();
            const closestLocation: string = locations.current;

            const { selectedLocation } = this.state;

            const locationsList = locations.available.map((l) => {
                return {
                    key: l,
                    text: locationToDisplayName(l),
                };
            });

            this.setState({
                selectedLocation: selectedLocation || closestLocation,
                locationsList: locationsList.sort(asc),
            });
        } catch {
            // ignore
        } finally {
            this.setState({ isGettingClosestLocation: false });
        }
    }

    private resolveResourceGroups = async (): Promise<IComboBoxOption[]> => {
        if (!this.state.selectedSubscription) {
            throw new Error('NoSubscription');
        }

        if (isNotNullOrEmpty(this.state.resourceGroupList)) {
            return this.state.resourceGroupList;
        }

        const url = new URL(
            `/subscriptions/${this.state.selectedSubscription}/resourcegroups`,
            'https://management.azure.com'
        );
        url.searchParams.set('api-version', armAPIVersion);

        const myJson = await this.getFromAzure(url.toString());
        let resourceGroupList: INewGroupOption[] = [];

        if (this.state.newGroup) {
            resourceGroupList.push({
                key: this.state.newGroup,
                text: this.state.newGroup,
                isNewGroup: true,
            });
        }

        for (let resGroup of myJson.value) {
            resourceGroupList.push({ key: resGroup.id, text: resGroup.name });
        }

        this.setState({
            resourceGroupList: resourceGroupList.sort(asc),
        });

        return resourceGroupList;
    };

    private isCurrentStateValid() {
        let isInvalid = false;

        const planName = this.planName.trim();
        isInvalid = isInvalid || !planName;

        if (
            !this.selectedResourceGroup ||
            !this.state.selectedLocation ||
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
            userSpecifiedPlanName: planName,
        });
    };

    private get automaticallyGeneratedPlanName() {
        if (!this.state.selectedLocation) {
            return '';
        }

        return `vso-plan-${this.state.selectedLocation.toLowerCase()}`;
    }

    private get planName() {
        return isDefined(this.state.userSpecifiedPlanName)
            ? this.state.userSpecifiedPlanName
            : this.automaticallyGeneratedPlanName;
    }

    private subscriptionChanged: (_: unknown, option?: IDropdownOption, index?: number) => void = (
        _e,
        option
    ) => {
        if (!option) {
            return;
        }

        if (typeof option.key !== 'string') {
            throw new Error('Subscription id should be a string');
        }

        this.setState({
            selectedSubscription: option.key,
            resourceGroupList: [],
        });

        return;
    };

    private resourceGroupChanged: IComboBoxProps['onChange'] = (
        _event,
        option,
        _index,
        value = ''
    ) => {
        const updateNewResourceGroupItem = (groupName: string) => {
            const { resourceGroupList } = this.state;
            if (!isNotNullOrEmpty(this.state.resourceGroupList)) {
                return;
            }

            this.setState({
                resourceGroupList: [
                    { key: groupName, text: groupName, isNewGroup: true },
                    ...resourceGroupList.filter((g) => !g.isNewGroup),
                ].sort(asc),
            });
        };

        if (option) {
            if (typeof option.key === 'number') {
                throw new Error('Resource group ids should always be a strings.');
            }

            // We keep a flag on the the new group option so it's re-selectable
            const isNewGroup = !!(option as INewGroupOption).isNewGroup;
            if (isNewGroup) {
                this.setState({
                    newGroup: option.key,
                    userSelectedResourceGroup: undefined,
                });
            } else {
                this.setState({
                    newGroup: undefined,
                    userSelectedResourceGroup: option.key,
                });
            }
        } else if (value) {
            this.setState({
                newGroup: value,
                userSelectedResourceGroup: undefined,
            });

            updateNewResourceGroupItem(value);
        }
    };

    private get selectedResourceGroup() {
        return this.state.userSelectedResourceGroup || this.state.newGroup;
    }

    private locationChanged: (
        event: FormEvent<HTMLDivElement>,
        option?: IDropdownOption,
        index?: number
    ) => void = (_e, option) => {
        if (!option) {
            return;
        }

        if (typeof option.key === 'number') {
            throw new Error('Resource group id should always be a string.');
        }

        this.setState({
            selectedLocation: option.key.toString(), // option.key has type string | number by default
        });
    };
}

export const CreatePlanPanel = connect(({ configuration }: ApplicationState) => ({
    configuration,
}))(CreatePlanPanelComponent);
