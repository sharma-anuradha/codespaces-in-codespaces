import React, { FormEvent, Component } from 'react';
import { IDropdownOption } from 'office-ui-fabric-react/lib/Dropdown';
import { connect } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';

import { newPlanPath } from '../../routerPaths';
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
import { locationToDisplayName } from '../../utils/locations';

interface PlanSelectorProps extends RouteComponentProps {
    plansList: IPlan[];
    selectedPlanId: string | null;
    isMadeInitialPlansRequest: boolean;
    isLoadingPlan: boolean;
    className?: string;
    isServiceAvailable: boolean;
}

export class PlanSelectorComponent extends Component<PlanSelectorProps> {
    public constructor(props: PlanSelectorProps) {
        super(props);
        const query = new URLSearchParams(props.location.search);
        const selectedPlanName = query.get('plan');

        if (selectedPlanName) {
            const selectedPlanObj: IPlan | undefined = this.props.plansList.find(
                (item) => item.name.toLowerCase() === selectedPlanName.toLowerCase()
            );

            if (selectedPlanObj) {
                selectPlan(selectedPlanObj);
            }
        }
    }

    render() {
        if (!this.props.isServiceAvailable) {
            return null;
        }

        const {
            selectedPlanId,
            plansList,
            isMadeInitialPlansRequest,
            isLoadingPlan,
            className = '',
        } = this.props;

        const loadingMessage = !isMadeInitialPlansRequest
            ? 'Fetching plan information...'
            : isLoadingPlan
            ? 'Fetching your plans...'
            : '';

        return (
            <DropDownWithLoader
                className={`vsonline-titlebar__dropdown ${className}`}
                options={this.plansToDropdownArray(plansList)}
                onChange={this.selectedPlanChanged}
                selectedKey={selectedPlanId || createNewPlanKey}
                isLoading={!!loadingMessage}
                loadingMessage={loadingMessage}
                ariaLabel='Plan Dropdown'
            />
        );
    }

    private plansToDropdownArray(plans: IPlan[]): IPlansDropdownOption[] {
        const planOptions = plans.map(
            (plan: IPlan): IPlansDropdownOption => {
                const friendlyLocation = locationToDisplayName(plan.location);

                return {
                    key: plan.id,
                    text: plan.name,
                    plan,
                    title: `Subscription Id: ${plan.subscription}\nResource Group: ${plan.resourceGroup}\nLocation: ${friendlyLocation}`,
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

const getPlansStoreState = ({ plans, serviceStatus: { isServiceAvailable } }: ApplicationState) => {
    const plansList = plans.plans;
    const { selectedPlan, isMadeInitialPlansRequest, isLoadingPlan } = plans;

    return {
        plansList,
        isMadeInitialPlansRequest,
        isLoadingPlan,
        isServiceAvailable,
        selectedPlanId: selectedPlan && selectedPlan.id,
    };
};

export const PlanSelector = connect(
    getPlansStoreState,
    {
        selectPlan,
    }
)(PlanSelectorComponent);