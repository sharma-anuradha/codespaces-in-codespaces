import React, { FormEvent, Component } from 'react';

import { Dropdown, IDropdownOption } from 'office-ui-fabric-react/lib/Dropdown';
//import { IComponentStyles } from '@uifabric/foundation/lib/IComponent';
import { RouteComponentProps, withRouter } from 'react-router-dom';
import { useWebClient } from '../../actions/middleware/useWebClient';
import { newAccountPath, environmentsPath } from '../../routes';

const createNewPlanKey: string = 'Create-Plan';

export class AccountSelector extends Component<RouteComponentProps>{

    static selectedAccountID: string;
    static selectedAccountLocation: string;

    myOptions: Array<{key: string, text: string}> = [];

    public constructor(props: RouteComponentProps){
        super(props);
        this.getAccounts();
    }
    
    render() {
        return (
            <Dropdown 
                className='vsonline-titlebar__dropdown'
                options={this.myOptions}
                onChange={this.selectedAccountChanged}
                selectedKey={AccountSelector.selectedAccountID}
                placeholder='Select a Plan'
            />
        );
    }

    private showPanel = () => {
        this.props.history.replace(newAccountPath);
    };

    public selectedAccountChanged: (
        event: FormEvent<HTMLDivElement>,
        option?: IDropdownOption,
        index?: number
    ) => void = (_e, option) => {  
        if (!option){
            return;
        }
        if(option.key === createNewPlanKey){
            this.showPanel();
        } else{
            AccountSelector.selectedAccountLocation = option.text.split('(')[1].split(')')[0];
            AccountSelector.selectedAccountID = option.key as string;   //option.key has type string | number by default
            this.props.history.replace(environmentsPath);
        }
    }

    public static getAccountID(){
        return this.selectedAccountID;
    }

    public static getAccountLocation(){
        return this.selectedAccountLocation;
    }

    private async getAccounts() { 

        const webClient = useWebClient();
        let accountsList: Array<any> = await webClient.get('/api/v1/accounts');
       
        for(let account of accountsList) {
            this.myOptions.push({key: account.id, text: account.name + ' (' + account.location + ')'});
        }
        this.myOptions.push({key: createNewPlanKey, text: 'Create a new Plan'}); 
        
        if(!AccountSelector.selectedAccountID && this.myOptions.length > 1){
            //account not set
            AccountSelector.selectedAccountID = this.myOptions[0].key;
            AccountSelector.selectedAccountLocation = this.myOptions[0].text.split('(')[1].split(')')[0];
        }
    }
}

