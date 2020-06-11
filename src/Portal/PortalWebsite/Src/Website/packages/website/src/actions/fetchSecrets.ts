import { action } from './middleware/useActionCreator';
import { ISecret } from 'vso-client-core';
import { useDispatch } from './middleware/useDispatch';
import { useActionContext } from './middleware/useActionContext';
import { useWebClient } from './middleware/useWebClient';

export const fetchSecretsActionType = 'async.secrets.fetch';
export const fetchSecretsSuccessActionType = 'async.secrets.fetch.success';
export const fetchSecretsFailureActionType = 'async.secrets.fetch.failure';

// Basic actions dispatched for reducers
const fetchSecretsAction = () => action(fetchSecretsActionType);
const fetchSecretsSuccessAction = (secrets: ISecret[]) =>
    action(fetchSecretsSuccessActionType, { secrets });
const fetchSecretsFailureAction = (error: Error) => action(fetchSecretsFailureActionType, error);

// Types to register with reducers
export type FetchSecretsAction = ReturnType<typeof fetchSecretsAction>;
export type FetchSecretsSuccessAction = ReturnType<typeof fetchSecretsSuccessAction>;
export type FetchSecretsFailureAction = ReturnType<typeof fetchSecretsFailureAction>;

// Exposed - callable actions that have side-effects
export async function fetchSecrets(planId: string) {
    const dispatch = useDispatch();

    const { state } = useActionContext();
    const { configuration } = state;

    if (!configuration) {
        throw new Error('Fetch configuration first.');
    }

    const { apiEndpoint } = configuration;
    try {
        dispatch(fetchSecretsAction());

        const webClient = useWebClient();

        const secrets = await webClient.get<ISecret[]>(
            `${apiEndpoint}/secrets?planId=${encodeURIComponent(planId)}`
        );

        dispatch(fetchSecretsSuccessAction(secrets));
        return secrets;
    } catch (err) {
        return dispatch(fetchSecretsFailureAction(err));
    }
}
