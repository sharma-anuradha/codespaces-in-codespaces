import {
    getPlansSuccessActionType,
    GetPlansSuccessAction,
    selectPlanActionType,
    SelectPlanAction,
    selectPlanSuccessActionType,
    SelectPlanSuccessAction,
    selectPlanFailureActionType,
    SelectPlanFailureAction,
    getPlansActionType,
    GetPlansAction,
    getPlansFailureActionType,
    GetPlansFailureAction,
    blurPlanSelectorActionType,
    BlurPlanSelectorAction,
    focusPlanSelectorActionType,
    FocusPlanSelectorAction,
} from '../actions/plans-actions';
import {
    deletePlanActionType,
    DeletePlanAction,
    deletePlanSuccessActionType,
    DeletePlanSuccessAction,
    deletePlanFailureActionType,
    DeletePlanFailureAction,
} from '../actions/deletePlan';

import { IPlan } from '../interfaces/IPlan';
import { ISku } from '../interfaces/ISku';

type AcceptedActions =
    | SelectPlanAction
    | SelectPlanSuccessAction
    | SelectPlanFailureAction
    | GetPlansAction
    | GetPlansSuccessAction
    | GetPlansFailureAction
    | BlurPlanSelectorAction
    | FocusPlanSelectorAction
    | DeletePlanAction
    | DeletePlanSuccessAction
    | DeletePlanFailureAction;

export type ActivePlanInfo = {
    availableSkus: ISku[];
} & IPlan;

export type PlansReducerState = {
    isMadeInitialPlansRequest: boolean;
    isLoadingPlan: boolean;
    shouldPlanSelectorReceiveFocus: boolean;
    plans: IPlan[];
    selectedPlan: ActivePlanInfo | null;
};

const defaultState: PlansReducerState = {
    isMadeInitialPlansRequest: false,
    isLoadingPlan: false,
    shouldPlanSelectorReceiveFocus: false,
    plans: [],
    selectedPlan: null,
};

const plansStoreStateKey = 'vsonline.plans.selector.state';

// TODO: move to persisted store
const savePlansState = (state: PlansReducerState) => {
    try {
        const stateToSave = {
            ...state,
            isMadeInitialPlansRequest: false,
            isLoadingPlan: false,
            shouldPlanSelectorReceiveFocus: false,
        };

        localStorage.setItem(plansStoreStateKey, JSON.stringify(stateToSave));
    } catch {
        // ignore
    }
};

const getDefaultPlansState = (): PlansReducerState => {
    try {
        const stateString = localStorage.getItem(plansStoreStateKey) || '';

        return JSON.parse(stateString) as PlansReducerState;
    } catch {
        return defaultState;
    }
};

const uniquePlans = (plans: IPlan[]) => {
    const seenPlans = new Set<string>();

    return plans.filter((plan: IPlan) => {
        if (!seenPlans.has(plan.id)) {
            seenPlans.add(plan.id);
            return true;
        }

        return false;
    });
};

export function plansReducer(
    state: PlansReducerState = getDefaultPlansState(),
    action: AcceptedActions
): PlansReducerState {
    const newState = plansReducerInternal(state, action);

    savePlansState(newState);

    return newState;
}

// tslint:disable-next-line: max-func-body-length
function plansReducerInternal(
    state: PlansReducerState,
    action: AcceptedActions
): PlansReducerState {
    switch (action.type) {
        case getPlansSuccessActionType: {
            let { plans, selectedPlanHint } = action.payload;
            let { selectedPlan } = state;
            // there is a bug that allows to create multiple equal plans,
            // filter out duplicates for now
            const plansList = uniquePlans(plans);

            // if no selected plan yet and there is some plans,
            // select the first one by default
            if (!selectedPlan && selectedPlanHint) {
                selectedPlan = selectedPlanHint;
                // if plansList is empty, deselect current selected plan
            } else if (!plansList.length) {
                selectedPlan = null;
            }

            return {
                ...state,
                selectedPlan,
                isMadeInitialPlansRequest: true,
                plans: [...plansList],
                isLoadingPlan: false,
            };
        }

        case getPlansFailureActionType: {
            return {
                ...state,
                plans: [],
                isMadeInitialPlansRequest: true,
                isLoadingPlan: false,
            };
        }

        case getPlansActionType: {
            return {
                ...state,
                isLoadingPlan: true,
            };
        }

        case selectPlanActionType: {
            return {
                ...state,
                selectedPlan: null,
                isLoadingPlan: true,
            };
        }

        case selectPlanSuccessActionType: {
            const { plan } = action.payload;

            return {
                ...state,
                selectedPlan: plan,
                isLoadingPlan: false,
            };
        }

        case selectPlanFailureActionType: {
            return {
                ...state,
                selectedPlan: null,
                isLoadingPlan: false,
            };
        }

        case deletePlanActionType: {
            return {
                ...state,
                selectedPlan: null,
                isLoadingPlan: true,
            };
        }

        case deletePlanSuccessActionType: {
            return {
                ...state,
                isLoadingPlan: false,
            };
        }

        case deletePlanFailureActionType: {
            return {
                ...state,
                isLoadingPlan: false,
            };
        }

        case blurPlanSelectorActionType: {
            return {
                ...state,
                shouldPlanSelectorReceiveFocus: false,
            };
        }

        case focusPlanSelectorActionType: {
            return {
                ...state,
                shouldPlanSelectorReceiveFocus: true,
            };
        }

        default: {
            return state;
        }
    }
}
