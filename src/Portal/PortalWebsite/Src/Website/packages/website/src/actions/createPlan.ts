import jwtDecode from 'jwt-decode';

import { action } from './middleware/useActionCreator';
import { useDispatch } from './middleware/useDispatch';

import { authService } from '../services/authService';
import {
    PlanCreationError,
    PlanCreationFailureReason,
} from '../components/environmentsPanel/PlanCreationError';
import { getAzureManagementApiVersion } from '../utils/getAzureManagementApiVersion';

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
        url.searchParams.set('api-version', getAzureManagementApiVersion());

        const response = await fetch(url.toString(), {
            method: 'PUT',
            body: JSON.stringify(data),
            headers: {
                'authorization': `Bearer ${accessToken}`,
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

async function getUserId() {
    let tokenString = await authService.getCachedToken();
    const jwtToken = jwtDecode(tokenString!.accessToken) as { oid: string; tid: string };
    return `${jwtToken.tid}_${jwtToken.oid}`;
}
