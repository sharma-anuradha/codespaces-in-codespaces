import { action } from './middleware/useActionCreator';

import { useWebClient } from './middleware/useWebClient';
import { useDispatch } from './middleware/useDispatch';
import { IPlan } from '../interfaces/IPlan';
import { useActionContext } from './middleware/useActionContext';
import { ActivePlanInfo } from '../reducers/plans-reducer';
import { getLocation } from './locations-actions';

export const selectPlanActionType = 'async.plan.select';
export const selectPlanSuccessActionType = 'async.plan.select.success';
export const selectPlanFailureActionType = 'async.plan.select.failure';

export const getPlansActionType = 'async.plans.getPlans';
export const getPlansSuccessActionType = 'async.plans.getPlans.success';
export const getPlansFailureActionType = 'async.plans.getPlans.failure';

export type SelectPlanAction = ReturnType<typeof selectPlanAction>;
export type SelectPlanSuccessAction = ReturnType<typeof selectPlanSuccessAction>;
export type SelectPlanFailureAction = ReturnType<typeof selectPlanFailureAction>;

export type GetPlansAction = ReturnType<typeof getPlansAction>;
export type GetPlansSuccessAction = ReturnType<typeof getPlansSuccessAction>;
export type GetPlansFailureAction = ReturnType<typeof getPlansFailureAction>;

const getPlansAction = () => action(getPlansActionType);

const getPlansSuccessAction = (plans: IPlan[], selectedPlanHint?: ActivePlanInfo) => {
    return action(getPlansSuccessActionType, { plans, selectedPlanHint });
};

const getPlansFailureAction = (error: Error) => {
    return action(getPlansFailureActionType, error);
};

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
            const locationInfo = await getLocation(plan.location);

            const activePlan = {
                ...plan,
                availableSkus: locationInfo && locationInfo.skus,
            };

            dispatch(selectPlanSuccessAction(activePlan));
        } else {
            dispatch(selectPlanSuccessAction(null));
        }
    } catch (err) {
        return dispatch(selectPlanFailureAction(err));
    }
};

export async function getPlans() {
    const dispatch = useDispatch();
    const actionContext = useActionContext();

    const { configuration } = actionContext.state;

    if (!configuration) {
        throw new Error('No configuration set, aborting.');
    }

    const { apiEndpoint } = configuration;

    try {
        dispatch(getPlansAction());

        const webClient = useWebClient();
        const plansList: IPlan[] = await webClient.get(`${apiEndpoint}/plans`, {
            retryCount: 2,
        });

        if (plansList.length) {
            const defaultPlan = plansList[0];
            const locationInfo = await getLocation(defaultPlan.location);

            dispatch(
                getPlansSuccessAction(plansList, {
                    ...defaultPlan,
                    availableSkus: locationInfo && locationInfo.skus,
                })
            );
        } else {
            dispatch(getPlansSuccessAction(plansList));
        }

        return plansList;
    } catch (err) {
        return dispatch(getPlansFailureAction(err));
    }
}
