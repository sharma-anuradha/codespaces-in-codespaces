import { useDispatch } from './middleware/useDispatch';
import { action } from './middleware/useActionCreator';
import { ServiceAuthenticationError, ServiceResponseError } from './middleware/useWebClient';

import { authService } from '../services/authService';
import { getAzureManagementApiVersion } from '../utils/getAzureManagementApiVersion';
import { getPlans } from './plans-actions';

export const deletePlanActionType = 'async.arm.delete.plan';
export const deletePlanSuccessActionType = 'async.arm.delete.plan.success';
export const deletePlanFailureActionType = 'async.arm.delete.plan.failure';

// Basic actions dispatched for reducers
const deletePlanAction = () => action(deletePlanActionType);
const deletePlanSuccessAction = () => action(deletePlanSuccessActionType);
const deletePlanFailureAction = (error: Error) => action(deletePlanFailureActionType, error);

// Types to register with reducers
export type DeletePlanAction = ReturnType<typeof deletePlanAction>;
export type DeletePlanSuccessAction = ReturnType<typeof deletePlanSuccessAction>;
export type DeletePlanFailureAction = ReturnType<typeof deletePlanFailureAction>;

// Exposed - callable actions that have side-effects
export async function deletePlan(planId: string) {
    const dispatch = useDispatch();
    try {
        dispatch(deletePlanAction());
        const myAuthToken = await authService.getARMToken(60);

        if (!myAuthToken) {
            dispatch(deletePlanFailureAction(new ServiceAuthenticationError()));
            return 'Not authenticated';
        }

        const { accessToken } = myAuthToken;
        const url = `https://management.azure.com${planId}?api-version=${getAzureManagementApiVersion()}`;

        const response = await fetch(url, {
            method: 'DELETE',
            headers: {
                authorization: `Bearer ${accessToken}`,
            },
        });

        if (!response.ok) {
            dispatch(
                deletePlanFailureAction(new ServiceResponseError(url, response.status, response))
            );
            return `Plan deletion failed: ${response.status}`;
        }
        dispatch(deletePlanSuccessAction());
    } catch (err) {
        dispatch(deletePlanFailureAction(err));
        return 'Plan deletion failed';
    } finally {
        // Call getPlans to refresh the plans list after deletion
        getPlans();
    }
}
