import { action } from './middleware/useActionCreator';

import { authService } from '../services/authService';
import { useDispatch } from './middleware/useDispatch';
import { armAPIVersion } from '../constants';
import {
    PlanCreationError,
    PlanCreationFailureReason,
} from '../components/environmentsPanel/PlanCreationError';

export const createResourceGroupActionType = 'async.arm.create.resourceGroup';
export const createResourceGroupSuccessActionType = 'async.arm.create.resourceGroup.success';
export const createResourceGroupFailureActionType = 'async.arm.create.resourceGroup.failure';

// Basic actions dispatched for reducers
const createResourceGroupAction = () => action(createResourceGroupActionType);
const createResourceGroupSuccessAction = (resourceGroupId: string) =>
    action(createResourceGroupSuccessActionType, { resourceGroupId });
const createResourceGroupFailureAction = (error: Error) =>
    action(createResourceGroupFailureActionType, error);

// Types to register with reducers
export type createResourceGroupAction = ReturnType<typeof createResourceGroupAction>;
export type createResourceGroupSuccessAction = ReturnType<typeof createResourceGroupSuccessAction>;
export type createResourceGroupFailureAction = ReturnType<typeof createResourceGroupFailureAction>;

// Exposed - callable actions that have side-effects
export async function createResourceGroup(
    subscriptionId: string,
    resourceGroupName: string,
    location: string
) {
    const dispatch = useDispatch();
    try {
        dispatch(createResourceGroupAction());

        const myAuthToken = await authService.getARMToken(60);
        if (!myAuthToken) {
            throw new PlanCreationError(PlanCreationFailureReason.NotAuthenticated);
        }
        const { accessToken } = myAuthToken;

        const url = new URL(
            `/subscriptions/${subscriptionId}/resourcegroups/${resourceGroupName}`,
            'https://management.azure.com'
        );
        url.searchParams.set('api-version', armAPIVersion);

        const resourceGroupExistsResponse = await fetch(url.toString(), {
            method: 'HEAD',
            headers: {
                authorization: `Bearer ${accessToken}`,
                'Content-Type': 'application/json',
            },
        });

        if (resourceGroupExistsResponse.status !== 404) {
            throw new PlanCreationError(PlanCreationFailureReason.NewResourceGroupAlreadyExists);
        }

        const createResourceGroupResponse = await fetch(url.toString(), {
            method: 'PUT',
            body: JSON.stringify({
                location,
            }),
            headers: {
                authorization: `Bearer ${accessToken}`,
                'Content-Type': 'application/json',
            },
        });

        if (!createResourceGroupResponse.ok) {
            const { error } = (await createResourceGroupResponse.json()) as {
                error: {
                    code: string;
                    message: string;
                };
            };

            throw new PlanCreationError(
                PlanCreationFailureReason.FailedToCreateResourceGroup,
                error && error.message
            );
        }

        if (
            createResourceGroupResponse.status !== 200 &&
            createResourceGroupResponse.status !== 201
        ) {
            throw new PlanCreationError(PlanCreationFailureReason.FailedToCreateResourceGroup);
        }

        const responseData = (await createResourceGroupResponse.json()) as {
            id: string;
            properties?: {
                provisioningState: string;
            };
        };

        if (responseData.properties && responseData.properties.provisioningState !== 'Succeeded') {
            throw new PlanCreationError(
                PlanCreationFailureReason.ResourceGroupProvisioningNotSuccessful
            );
        }

        dispatch(createResourceGroupSuccessAction(responseData.id));
        return responseData.id;
    } catch (err) {
        return dispatch(createResourceGroupFailureAction(err));
    }
}
