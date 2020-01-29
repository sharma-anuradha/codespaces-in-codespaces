import { action } from './middleware/useActionCreator';

import { authService } from '../services/authService';
import { useDispatch } from './middleware/useDispatch';
import {
    PlanCreationError,
    PlanCreationFailureReason,
} from '../components/environmentsPanel/PlanCreationError';
import jwtDecode from 'jwt-decode';

export const createPlanActionType = 'async.arm.create.plan';
export const createPlanSuccessActionType = 'async.arm.create.plan.success';
export const createPlanFailureActionType = 'async.arm.create.plan.failure';

// Basic actions dispatched for reducers
const createPlanAction = () => action(createPlanActionType);
const createPlanSuccessAction = () => action(createPlanSuccessActionType);
const createPlanFailureAction = (error: Error) => action(createPlanFailureActionType, error);

// Types to register with reducers
export type createPlanAction = ReturnType<typeof createPlanAction>;
export type createPlanSuccessAction = ReturnType<typeof createPlanSuccessAction>;
export type createPlanFailureAction = ReturnType<typeof createPlanFailureAction>;

// Exposed - callable actions that have side-effects
export async function createPlan(resourceGroupPath: string, planName: string, location: string) {
    const dispatch = useDispatch();

    try {
        dispatch(createPlanAction());

        const userId = await getUserId();

        const myAuthToken = await authService.getARMToken(60);
        if (!myAuthToken) {
            throw new PlanCreationError(PlanCreationFailureReason.NotAuthenticated);
        }
        const { accessToken } = myAuthToken;

        const data = {
            location,
            tags: {},
            properties: { userId },
        };

        const url = new URL(
            `${resourceGroupPath}/providers/Microsoft.VSOnline/plans/${planName}`,
            'https://management.azure.com'
        );
        url.searchParams.set('api-version', getAPIVersion());

        const response = await fetch(url.toString(), {
            method: 'PUT',
            body: JSON.stringify(data),
            headers: {
                authorization: `Bearer ${accessToken}`,
                'Content-Type': 'application/json',
            },
        });

        if (!response.ok) {
            const { error } = (await response.json()) as {
                error: {
                    code: string;
                    message: string;
                };
            };

            throw new PlanCreationError(
                PlanCreationFailureReason.FailedToCreatePlan,
                error && error.message
            );
        }

        dispatch(createPlanSuccessAction());
    } catch (err) {
        return dispatch(createPlanFailureAction(err));
    }
}

export async function deletePlan (planId: string) {
    const myAuthToken = await authService.getARMToken(60);

    if (!myAuthToken) {
        return "Not authenticated";
    }

    const { accessToken } = myAuthToken;

    const url = `https://management.azure.com${planId}?api-version=${getAPIVersion()}`

    const response = await fetch(url, {
        method: 'DELETE',
        headers: {
            authorization: `Bearer ${accessToken}`,
        },
    });

    if (!response.ok) {
        return `Plan deletion failed: ${response.status}`;
    }
    
}

/**
 * API Version	Endpoint URL
 * 2019-07-01-preview	online.visualstudio.com/api/v1
 * 2019-07-01-beta	    online-ppe.vsengsaas.visualstudio.com/api/v1
 * 2019-07-01-alpha	    online.dev.vsengsaas.visualstudio.com/api/v1
 */
function getAPIVersion() {
    const baseURL = window.location.href.split('/')[2];
    let apiVersion;
    if (baseURL.includes('dev')) {
        apiVersion = '2019-07-01-alpha';
    } else if (baseURL.includes('ppe')) {
        apiVersion = '2019-07-01-beta';
    } else {
        apiVersion = '2019-07-01-preview';
    }
    return apiVersion;
}

async function getUserId() {
    let tokenString = await authService.getCachedToken();
    const jwtToken = jwtDecode(tokenString!.accessToken) as { oid: string; tid: string };
    return `${jwtToken.tid}_${jwtToken.oid}`;
}
