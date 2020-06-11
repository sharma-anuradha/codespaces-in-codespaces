import { action } from './middleware/useActionCreator';
import { useDispatch } from './middleware/useDispatch';
import { useActionContext } from './middleware/useActionContext';
import { useWebClient, ServiceResponseError } from './middleware/useWebClient';
import { ISecret, ICreateSecretRequest, SecretErrorCodes } from 'vso-client-core';

export const createSecretActionType = 'async.secret.create';
export const createSecretSuccessActionType = 'async.secret.create.success';
export const createSecretFailureActionType = 'async.secret.create.failure';

// Basic actions dispatched for reducers
const createSecretAction = () => action(createSecretActionType);
const createSecretSuccessAction = (secret: ISecret) =>
    action(createSecretSuccessActionType, { secret });
const createSecretFailureAction = (error: Error) => action(createSecretFailureActionType, error);

// Types to register with reducers
export type CreateSecretAction = ReturnType<typeof createSecretAction>;
export type CreateSecretSuccessAction = ReturnType<typeof createSecretSuccessAction>;
export type CreateSecretFailureAction = ReturnType<typeof createSecretFailureAction>;

// Exposed - callable actions that have side-effects
export async function createSecret(planId: string, createSecretRequest: ICreateSecretRequest) {
    const dispatch = useDispatch();

    const { state } = useActionContext();
    const { configuration } = state;

    if (!configuration) {
        throw new Error('Fetch configuration first.');
    }

    const { apiEndpoint } = configuration;
    try {
        dispatch(createSecretAction());

        const webClient = useWebClient();

        const secret = await webClient.post<ISecret>(
            `${apiEndpoint}/secrets?planId=${encodeURIComponent(planId)}`,
            createSecretRequest
        );

        dispatch(createSecretSuccessAction(secret));
        return secret;
    } catch (err) {
        if (err instanceof ServiceResponseError) {
            const errorCode = await err.response.json();
            if (errorCode) {
                err.message = errorCode;
            }
        }
        return dispatch(createSecretFailureAction(err));
    }
}
