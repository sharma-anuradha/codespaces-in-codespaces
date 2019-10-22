import {
    GetPlansSuccessAction,
    getPlansSuccessActionType,
    selectPlanActionType,
    SelectPlanAction,
    GetPlansAction,
    GetPlansFailureAction,
    getPlansFailureActionType,
    getPlansActionType
} from '../actions/plans-actions'

import { IPlan } from '../interfaces/IPlan';

type AcceptedActions =
    | GetPlansAction
    | SelectPlanAction
    | GetPlansSuccessAction
    | GetPlansFailureAction;

export type PlansReducerState = {
    isMadeInitalPlansRequest: boolean;
    plans: IPlan[];
    selectedPlan: IPlan | null;
};

const defaultState: PlansReducerState = {
    isMadeInitalPlansRequest: false,
    plans: [],
    selectedPlan: null
};

const plansStoreStateKey = 'vso-plans-store-state';

// TODO: move to persisted store
const savePlansState = (state: PlansReducerState) => {
    try {
        const stateToSave = {
            ...state,
            isMadeInitalPlansRequest: false
        }
        
        localStorage.setItem(plansStoreStateKey, JSON.stringify(stateToSave));
    } catch {
        // ignore
    }
}

const getDefaultPlansState = (): PlansReducerState => {
    try {
        const stateString = localStorage.getItem(plansStoreStateKey) || '';

        return JSON.parse(stateString) as PlansReducerState;
    } catch {
        return defaultState;
    }
}

const uniquePlans = (plans: IPlan[]) => {
    const seenPlans = new Set<string>();

    return plans.filter((plan: IPlan) => {
        if (!seenPlans.has(plan.id)) {
            seenPlans.add(plan.id);
            return true;
        }

        return false;
    });
}

export function plansReducer(
    state: PlansReducerState = getDefaultPlansState(),
    action: AcceptedActions
): PlansReducerState {
    const newState = plansReducerInternal(state, action);

    savePlansState(newState);

    return newState;
}

function plansReducerInternal(
    state: PlansReducerState,
    action: AcceptedActions
): PlansReducerState {
    switch (action.type) {
        case getPlansSuccessActionType: {
            let { plansList } = action.payload;
            let { selectedPlan } = state;
            // there is a bug that allows to create multiple equal plans,
            // filter oout duplicates for now
            plansList = uniquePlans(plansList);

            // if no selected plan yet and there is some plans,
            // select the first one by default
            if (!selectedPlan && plansList.length) {
                selectedPlan = plansList[0];
            // if plansList is empty, deselec current selected plan
            } else if (!plansList.length) {
                selectedPlan = null;
            }

            return {
                ...state,
                selectedPlan: selectedPlan,
                isMadeInitalPlansRequest: true,
                plans: [ ...plansList ]
            }
        }

        case selectPlanActionType: {
            const { plan } = action.payload;

            return {
                ...state,
                selectedPlan: plan
            }
        }

        case getPlansFailureActionType: {
            return {
                ...state,
                isMadeInitalPlansRequest: true
            };
        }

        default: {
            return state;
        }
    }
}
