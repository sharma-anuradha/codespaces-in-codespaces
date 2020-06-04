import React, { FormEvent, Component } from 'react';
import { IDropdownOption } from 'office-ui-fabric-react/lib/Dropdown';
import { connect } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';

import { newPlanPath } from '../../routerPaths';
import { ApplicationState } from '../../reducers/rootReducer';
import { selectPlan, blurPlanSelectorDropdown } from '../../actions/plans-actions';

import {
    createNewPlanKey,
    createNewPlanDropdownOption,
    dividerDropdownOption,
} from './plan-selector-constants';

import { IPlan } from '../../interfaces/IPlan';
import { IPlansDropdownOption } from '../../interfaces/IPlansDropdownOption';
import { DropDownWithLoader } from '../dropdown-with-loader/dropdown-with-loader';
import { locationToDisplayName } from '../../utils/locations';
import '../titlebar/titlebar.css';
import { injectMessageParameters } from '../../utils/injectMessageParameters';
import { withTranslation, WithTranslation } from 'react-i18next';

interface PlanSelectorProps extends RouteComponentProps, WithTranslation {
    plansList: IPlan[];
    selectedPlanId: string | null;
    isMadeInitialPlansRequest: boolean;
    isLoadingPlan: boolean;
    shouldPlanSelectorReceiveFocus: boolean;
    className?: string;
    isServiceAvailable: boolean;
    hasNoCreate?: boolean;
}

export class PlanSelectorComponent extends Component<PlanSelectorProps> {
    public constructor(props: PlanSelectorProps) {
        super(props);
    }

    componentDidMount() {
        const query = new URLSearchParams(this.props.location.search);
        const planName = query.get('plan');

        if (planName) {
            const planObject = this.props.plansList.find(
                (item) => item.name.toLowerCase() === planName.toLowerCase()
            );

            if (planObject && planObject.id !== this.props.selectedPlanId) {
                selectPlan(planObject);
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
            shouldPlanSelectorReceiveFocus,
            className = '',
            t: translation,
        } = this.props;

        const loadingMessage = !isMadeInitialPlansRequest
            ? translation('fetchingPlanInformation')
            : isLoadingPlan
            ? translation('fetchingPlans')
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
                shouldFocus={shouldPlanSelectorReceiveFocus}
                translation={translation}
            />
        );
    }

    private plansToDropdownArray(plans: IPlan[]): IPlansDropdownOption[] {
        const { t: translation } = this.props;
        const planOptions = plans.map(
            (plan: IPlan): IPlansDropdownOption => {
                const friendlyLocation = locationToDisplayName(plan.location);
                const title = injectMessageParameters(
                    translation('planDropdownTitle'),
                    plan.subscription,
                    plan.resourceGroup,
                    friendlyLocation
                );

                return {
                    key: plan.id,
                    text: plan.name,
                    plan,
                    title,
                };
            }
        );

        if (planOptions.length) {
            planOptions.push(dividerDropdownOption);
        }

        if (!this.props.hasNoCreate) {
            planOptions.push(createNewPlanDropdownOption(this.props.t));
        }

        return planOptions;
    }

    private selectedPlanChanged: (
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

        blurPlanSelectorDropdown();

        this.props.history.push(newPlanPath);
    };
}

const getPlansStoreState = ({ plans, serviceStatus: { isServiceAvailable } }: ApplicationState) => {
    const plansList = plans.plans;
    const {
        selectedPlan,
        isMadeInitialPlansRequest,
        isLoadingPlan,
        shouldPlanSelectorReceiveFocus,
    } = plans;

    return {
        plansList,
        isMadeInitialPlansRequest,
        isLoadingPlan,
        shouldPlanSelectorReceiveFocus,
        isServiceAvailable,
        selectedPlanId: selectedPlan && selectedPlan.id,
    };
};

export const PlanSelector = withTranslation()(connect(getPlansStoreState, {
    selectPlan,
})(PlanSelectorComponent));
