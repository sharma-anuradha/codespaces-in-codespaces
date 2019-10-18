import React, { Component, FormEvent, KeyboardEvent, SyntheticEvent } from 'react';
import {
    PrimaryButton,
    DefaultButton,
} from 'office-ui-fabric-react/lib/Button';
import { Panel, PanelType } from 'office-ui-fabric-react/lib/Panel';
import { Stack } from 'office-ui-fabric-react/lib/Stack';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { Dropdown, IDropdownOption } from 'office-ui-fabric-react/lib/Dropdown';
import { KeyCodes } from '@uifabric/utilities';
import { CommentThreadCollapsibleState } from 'vscode';
import { getARMToken } from '../../services/authARMService';
import { authService } from '../../services/authService';
import jwtDecode from 'jwt-decode';


export interface CreatePlanPanelProps {
    hidePanel: () => void;
}

export interface CreatePlanPanelState {
    planName?: string;
    regionList: Array<{key: string, text: string}>;
    subscriptionList: Array<{key: string, text: string}>;
    resourceGroupList: Array<{key: string, text: string}>;
    selectedSubscription?: string;
    resourceGroup?: string;
    selectedRegion?: string;
}

export class CreatePlanPanel extends Component<CreatePlanPanelProps, CreatePlanPanelState>{

    
    public constructor(props: CreatePlanPanelProps){
        super(props);
        
        this.state = {
            regionList: this.getRegions(),
            subscriptionList: [],
            resourceGroupList: [],
        };
        this.getSubscriptions();
    }

    render() {
        return (
            <Panel
                isOpen={true}
                type={PanelType.smallFixedFar}
                headerText='Create a Plan'
                closeButtonAriaLabel='Close'
                onKeyDown={this.dismissPanel}
                onDismiss={this.props.hidePanel}
                onRenderFooterContent={this.onRenderFooterContent}
            >
                <Stack>
                    <Dropdown
                        label='Subscription'
                        onChange={this.subscriptionChanged}
                        options={this.state.subscriptionList}
                    />

                    <Dropdown
                        label='Resource Group'
                        onChange={this.resourceGroupChanged}
                        options={this.state.resourceGroupList}
                        disabled={!this.state.selectedSubscription}
                    />
                    <Dropdown
                        label='Region'
                        onChange={this.regionChanged}
                        options={this.state.regionList}
                    />
                    <TextField
                        label='Plan Name'
                        placeholder='planNameExample'
                        onChange={this.planNameChanged}
                        value={this.state.planName}
                    />
                </Stack>
            </Panel>
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
        if(!this.isCurrentStateValid()){
            return;
        }
        const userID = await this.getUserID();

        const data = {
            "location":this.state.selectedRegion,
            "tags":{},
            "properties":{ 
                "userId": userID
            },
        }
 
        const url = 'https://management.azure.com/'+ this.state.resourceGroup    //NOTE: Resource group id contains subscription id
                    +'/providers/Microsoft.VSOnline//plans/'+ this.state.planName
                    +'?api-version=' + this.getAPIVersion();

        const myAuthToken = await getARMToken(60);
        if (myAuthToken){
            var authToken = 'Bearer ' + myAuthToken.accessToken;
            await fetch(url, {
                method: "PUT",
                body: JSON.stringify(data),
                headers: {
                    authorization: authToken,
                    'Content-Type': 'application/json'
                },     
            });

            this.props.hidePanel();
        }
    }

    private clearForm = () => { 
        this.setState({
            planName: undefined,
            regionList: [],
        });
        this.props.hidePanel();
    };

    private async getUserID(){
        let tokenString = await authService.getCachedToken();
        const jwtToken = jwtDecode(tokenString!.accessToken) as { oid: string, tid: string };
        return `${jwtToken.tid}_${jwtToken.oid}`;
    }

    /**
     * API Version	Endpoint URL
     * 2019-07-01-preview	online.visualstudio.com/api/v1
     * 2019-07-01-beta	    online-ppe.vsengsaas.visualstudio.com/api/v1
     * 2019-07-01-alpha	    online.dev.vsengsaas.visualstudio.com/api/v1
     */
    public getAPIVersion(){  
        const baseURL = window.location.href.split('/')[2];
        let apiVersion;
        if(baseURL.includes('dev')){
            apiVersion = '2019-07-01-alpha';
        }
        else if(baseURL.includes('ppe')){
            apiVersion = '2019-07-01-beta';
        }
        else {
            apiVersion = '2019-07-01-preview';
        }
        return apiVersion;
    }

    //Functions that handle Azure API calls to populate dropdowns

    private getRegions(): Array<{key: string, text: string}>{
        //This will need to not be hardcoded eventually

        let myList = [
            { key: 'EastUs', text: 'EastUs'},
            { key: 'SouthEastAsia', text: 'SouthEastAsia'},
            { key: 'WestEurope', text: 'WestEurope'},
            { key: 'WestUs2', text: 'WestUs2'},
        ];

        return myList;
    }
    private async getFromAzure(url:string){
        const myAuthToken = await getARMToken(60);
        if(myAuthToken){
            const authToken = 'Bearer ' + myAuthToken.accessToken;
            let response = await fetch(url, {
                headers: {
                    authorization: authToken
                }
            })

            return response.json()
        }
    }

    private async getSubscriptions() {
       
        const myJson = await this.getFromAzure(`https://management.azure.com/subscriptions?api-version=2019-05-10`);

        const subscriptionsJsonList = [];
    
        for (let subscription of myJson.value) {
            subscriptionsJsonList.push({key: subscription.subscriptionId, text: subscription.displayName});
        }

        this.setState({
            subscriptionList: subscriptionsJsonList,
        }); 
    }

    private async getResourceGroups(subID: string) {
        if((this.state.selectedSubscription != undefined)){
            const rgURL = 'https://management.azure.com/subscriptions/' + subID +'/resourcegroups?api-version=2019-05-10';
            const myJson = await this.getFromAzure(rgURL);
            let resourceGroupJsonList = [];

            for(let resGroup of myJson.value){
                resourceGroupJsonList.push({key: resGroup.id, text: resGroup.name});
            }
            this.setState({
                resourceGroupList: resourceGroupJsonList,
            });
        }
    }

    private isCurrentStateValid(){
        let isInvalid = false;

        const planName = this.state.planName && this.state.planName.trim();
        isInvalid = isInvalid || !planName || planName.length === 0;

        if(!this.state.resourceGroup
            || !this.state.selectedRegion
            || !this.state.selectedSubscription){

            isInvalid = true;
        }
        return !isInvalid;
    }

    //Functions that handle dropdown change events

    private planNameChanged: (
        event: FormEvent<HTMLInputElement | HTMLTextAreaElement>,
        planName?: string
    ) => void = (_event, planName) => {
        this.setState({
            planName,
        });
    };

    private subscriptionChanged:( 
        event: FormEvent<HTMLDivElement>,
        option?: IDropdownOption,
        index?: number
    ) => void = (_e, option, index) => {
        if (!option) {
            return;
        }

        this.setState({
            selectedSubscription: option.key as string, 
        })

        this.getResourceGroups(option.key as string);

        return;
    };

    private resourceGroupChanged: (
        event: FormEvent<HTMLDivElement>,
        option?: IDropdownOption,
        index?: number
    ) => void = (_e, option, index) => {
        if (!option) {
            return;
        }
        this.setState({
            resourceGroup: option.key.toString(),   //option.key has type string | number by default
        });
    };

    private regionChanged: (
        event: FormEvent<HTMLDivElement>,
        option?: IDropdownOption,
        index?: number
    ) => void = (_e, option, index) => {
        if (!option) {
            return;
        }
        this.setState({
            selectedRegion: option.key.toString(),   //option.key has type string | number by default
        })
    };
}
