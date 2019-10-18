import React, { FormEvent, Component } from 'react';

import { Dropdown, IDropdownOption } from 'office-ui-fabric-react/lib/Dropdown';
//import { IComponentStyles } from '@uifabric/foundation/lib/IComponent';
import { RouteComponentProps, withRouter } from 'react-router-dom';
import { useWebClient } from '../../actions/middleware/useWebClient';
import { newPlanPath, environmentsPath } from '../../routes';

const createNewPlanKey: string = 'Create-Plan';

export class PlanSelector extends Component<RouteComponentProps>{

    static selectedPlanID: string;
    static selectedPlanLocation: string;

    myOptions: Array<{key: string, text: string}> = [];

    public constructor(props: RouteComponentProps){
        super(props);
        this.getPlans();
    }
    
    render() {
        return (
            <Dropdown 
                className='vsonline-titlebar__dropdown'
                options={this.myOptions}
                onChange={this.selectedPlanChanged}
                selectedKey={PlanSelector.selectedPlanID}
                placeholder='Select a Plan'
            />
        );
    }

    private showPanel = () => {
        this.props.history.replace(newPlanPath);
    };

    public selectedPlanChanged: (
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
            PlanSelector.selectedPlanLocation = option.text.split('(')[1].split(')')[0];
            PlanSelector.selectedPlanID = option.key as string;   //option.key has type string | number by default
            this.props.history.replace(environmentsPath);
        }
    }

    public static getPlanID(){
        return this.selectedPlanID;
    }

    public static getPlanLocation(){
        return this.selectedPlanLocation;
    }

    private async getPlans() { 

        const webClient = useWebClient();
        let plansList: Array<any> = await webClient.get('/api/v1/plans');
       
        for(let plan of plansList) {
            this.myOptions.push({key: plan.id, text: plan.name + ' (' + plan.location + ')'});
        }
        this.myOptions.push({key: createNewPlanKey, text: 'Create a new Plan'}); 
        
        if(!PlanSelector.selectedPlanID && this.myOptions.length > 1){
            //plan not set
            PlanSelector.selectedPlanID = this.myOptions[0].key;
            PlanSelector.selectedPlanLocation = this.myOptions[0].text.split('(')[1].split(')')[0];
        }
    }
}

