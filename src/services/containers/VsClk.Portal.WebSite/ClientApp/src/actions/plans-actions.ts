import { action } from './middleware/useActionCreator';

import { useWebClient } from './middleware/useWebClient';
import { useDispatch } from './middleware/useDispatch';
import { IPlan } from '../interfaces/IPlan';
import { useActionContext } from './middleware/useActionContext';

export const selectPlanActionType = 'async.plan.select';
export const getPlansActionType = 'async.plans.getPlans';
export const getPlansSuccessActionType = 'async.plans.getPlans.success';
export const getPlansFailureActionType = 'async.plans.getPlans.failure';

const getPlansAction = () => action(getPlansActionType);

const getPlansSuccessAction = (plansList: IPlan[]) => {
    return action(getPlansSuccessActionType, { plansList });
}

const getPlansFailureAction = (error: Error) => {
    return action(getPlansFailureActionType, error);
}

export const selectPlanAction = (plan: IPlan | null) => {
    return action(selectPlanActionType, { plan });
}

export const selectPlan = (plan: IPlan | null) => {
    const dispatch = useDispatch();
    
    dispatch(selectPlanAction(plan));
}

export type SelectPlanAction = ReturnType<typeof selectPlanAction>;
export type GetPlansAction = ReturnType<typeof getPlansAction>;
export type GetPlansSuccessAction = ReturnType<typeof getPlansSuccessAction>;
export type GetPlansFailureAction = ReturnType<typeof getPlansFailureAction>;

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
        const plansList: Array<any> = await webClient.get(`${apiEndpoint}/plans`);

        dispatch(getPlansSuccessAction(plansList));

        return plansList;
    } catch (err) {
        return dispatch(getPlansFailureAction(err));
    }
}
