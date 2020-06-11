import { action } from './middleware/useActionCreator';

import {
    useWebClient,
    ServiceAuthenticationError,
} from './middleware/useWebClient';
import { useDispatch } from './middleware/useDispatch';
import { IPlan } from '../interfaces/IPlan';
import { useActionContext } from './middleware/useActionContext';
import { ActivePlanInfo } from '../reducers/plans-reducer';
import { getLocation } from './locations-actions';
import { logout } from './logout';
import { fetchSecrets } from './fetchSecrets';

export const selectPlanActionType = 'async.plan.select';
export const selectPlanSuccessActionType = 'async.plan.select.success';
export const selectPlanFailureActionType = 'async.plan.select.failure';

export const getPlansActionType = 'async.plans.getPlans';
export const getPlansSuccessActionType = 'async.plans.getPlans.success';
export const getPlansFailureActionType = 'async.plans.getPlans.failure';

export const blurPlanSelectorActionType = 'async.plans.blur';
export const focusPlanSelectorActionType = 'async.plans.focus';

export type SelectPlanAction = ReturnType<typeof selectPlanAction>;
export type SelectPlanSuccessAction = ReturnType<typeof selectPlanSuccessAction>;
export type SelectPlanFailureAction = ReturnType<typeof selectPlanFailureAction>;

export type GetPlansAction = ReturnType<typeof getPlansAction>;
export type GetPlansSuccessAction = ReturnType<typeof getPlansSuccessAction>;
export type GetPlansFailureAction = ReturnType<typeof getPlansFailureAction>;

export type BlurPlanSelectorAction = ReturnType<typeof blurPlanSelectorAction>;
export type FocusPlanSelectorAction = ReturnType<typeof focusPlanSelectorAction>;

const getPlansAction = () => action(getPlansActionType);

const getPlansSuccessAction = (plans: IPlan[], selectedPlanHint?: ActivePlanInfo) => {
    return action(getPlansSuccessActionType, { plans, selectedPlanHint });
};

const getPlansFailureAction = (error: Error) => {
    return action(getPlansFailureActionType, error);
};

const blurPlanSelectorAction = () => action(blurPlanSelectorActionType);
const focusPlanSelectorAction = () => action(focusPlanSelectorActionType);

export const selectPlanAction = () => action(selectPlanActionType);

export const selectPlanSuccessAction = (plan: ActivePlanInfo | null) => {
    return action(selectPlanSuccessActionType, { plan });
};

export const selectPlanFailureAction = (error: Error) => {
    return action(selectPlanFailureActionType, error);
};

export const selectPlan = async (plan: IPlan | null) => {
    const dispatch = useDispatch();

    try {
        dispatch(selectPlanAction());

        if (plan) {
            const locationInfo = await getLocation(plan.location, plan.id);

            const activePlan = {
                ...plan,
                availableSkus: (locationInfo && locationInfo.skus) || [],
            };

            await dispatch(fetchSecrets(plan.id));

            dispatch(selectPlanSuccessAction(activePlan));
        } else {
            dispatch(selectPlanSuccessAction(null));
        }
    } catch (err) {
        if (err instanceof ServiceAuthenticationError) {
            dispatch(logout({ isExplicit: false }));
        }

        return dispatch(selectPlanFailureAction(err));
    }
};

export async function getPlans() {
    const dispatch = useDispatch();
    const actionContext = useActionContext();

    const { configuration, plans } = actionContext.state;

    if (!configuration) {
        throw new Error('No configuration set, aborting.');
    }

    const { apiEndpoint } = configuration;

    let plansList: IPlan[];
    try {
        dispatch(getPlansAction());

        const webClient = useWebClient();
        plansList = await webClient.get(`${apiEndpoint}/plans`, {
            retryCount: 2,
        });
    } catch (err) {
        return dispatch(getPlansFailureAction(err));
    }

    try {
        if (plansList.length) {
            if (!plans.selectedPlan) {
                const defaultPlan = plansList[0];
                await dispatch(selectPlan(defaultPlan));
            }
            else {
                await dispatch(fetchSecrets(plans.selectedPlan.id));
            }
        }
        dispatch(getPlansSuccessAction(plansList));

        return plansList;
    } catch (err) {
        if (err instanceof ServiceAuthenticationError) {
            dispatch(logout({ isExplicit: false }));
        }

        return dispatch(getPlansFailureAction(err));
    }
}

export const blurPlanSelectorDropdown = () => {
    const dispatch = useDispatch();
    dispatch(blurPlanSelectorAction());
};

export const focusPlanSelectorDropdown = () => {
    const dispatch = useDispatch();
    dispatch(focusPlanSelectorAction());
};
