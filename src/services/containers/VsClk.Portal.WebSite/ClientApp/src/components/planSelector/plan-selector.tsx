import React, { FormEvent, Component } from 'react';
import { Dropdown, IDropdownOption } from 'office-ui-fabric-react/lib/Dropdown';
import { connect } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';

import { newPlanPath } from '../../routes';
import { ApplicationState } from '../../reducers/rootReducer';
import { selectPlan } from '../../actions/plans-actions';

import {
    createNewPlanKey,
    createNewPlanDropdownOption,
    dividerDropdownOption,
} from './plan-selector-constants';

import { IPlan } from '../../interfaces/IPlan';
import { IPlansDropdownOption } from '../../interfaces/IPlansDropdownOption';
import { DropDownWithLoader } from '../dropdown-with-loader/dropdown-with-loader';

interface PlanSelectorProps extends RouteComponentProps {
    plansList: IPlan[];
    selectedPlanId: string | null;
    isMadeInitalPlansRequest: boolean;
    className?: string;
}

export class PlanSelectorComponent extends Component<PlanSelectorProps> {
    render() {
        const {
            selectedPlanId,
            plansList,
            isMadeInitalPlansRequest,
            className = ''
        } = this.props;

        return (
            <DropDownWithLoader
                className={`vsonline-titlebar__dropdown ${className}`}
                options={this.plansToDropdownArray(plansList)}
                onChange={this.selectedPlanChanged}
                selectedKey={selectedPlanId || createNewPlanKey}
                isLoading={!isMadeInitalPlansRequest}
                loadingMessage={'Fetching your plans...'}
            />
        );
    }

    private plansToDropdownArray(plans: IPlan[]): IPlansDropdownOption[] {
        const planOptions = plans.map(
            (plan: IPlan): IPlansDropdownOption => {
                return {
                    key: plan.id,
                    text: plan.name,
                    plan,
                };
            }
        );

        if (planOptions.length) {
            planOptions.push(dividerDropdownOption);
        }

        planOptions.push(createNewPlanDropdownOption);

        return planOptions;
    }

    public selectedPlanChanged: (
        event: FormEvent<HTMLDivElement>,
        option?: IDropdownOption,
        index?: number
    ) => void = (_e, option?: IDropdownOption) => {
        if (!option) {
            throw new Error('Plan dropdown changed but no selected option received.');
        }

        const { plan } = option as IPlansDropdownOption;
        if (plan !== null) {
            return selectPlan(plan);
        }

        this.props.history.push(newPlanPath);
    };
}

const getPlansStoreState = ({ plans }: ApplicationState) => {
    const plansList = plans.plans;
    const { selectedPlan, isMadeInitalPlansRequest } = plans;

    return {
        plansList,
        isMadeInitalPlansRequest,
        selectedPlanId: selectedPlan && selectedPlan.id,
    };
};

export const PlanSelector = connect(
    getPlansStoreState,
    {
        selectPlan,
    }
)(PlanSelectorComponent);
