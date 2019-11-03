import { action } from './middleware/useActionCreator';
import { useWebClient } from './middleware/useWebClient';
import { useDispatch } from './middleware/useDispatch';
import { ServiceAuthenticationError } from './middleware/useWebClient';
import { clearAuthToken } from './clearAuthToken';
import { useActionContext } from './middleware/useActionContext';

export const notificationsSubscribeActionType = 'async.notifications.subscribe';
export const notificationsSubscribeSuccessActionType = 'async.notifications.subscribe.success';
export const notificationsSubscribeFailureActionType = 'async.notifications.subscribe.failure';

// Basic actions dispatched for reducers
const notificationsSubscribeAction = (email: string) =>
    action(notificationsSubscribeActionType, { email });
const notificationsSubscribeSuccessAction = () => action(notificationsSubscribeSuccessActionType);
const notificationsSubscribeFailureAction = (error: Error) =>
    action(notificationsSubscribeFailureActionType, error);

// Types to register with reducers
export type notificationsSubscribeAction = ReturnType<typeof notificationsSubscribeAction>;
export type notificationsSubscribeSuccessAction = ReturnType<
    typeof notificationsSubscribeSuccessAction
>;
export type notificationsSubscribeFailureAction = ReturnType<
    typeof notificationsSubscribeFailureAction
>;

// Exposed - callable actions that have side-effects
export async function notificationsSubscribe(email: string) {
    const dispatch = useDispatch();
    const webClient = useWebClient();
    const actionContext = useActionContext();
    const { configuration } = actionContext.state;
    if (!configuration) {
        throw new Error('No configuration set, aborting.');
    }
    const { apiEndpoint } = configuration;

    try {
        dispatch(notificationsSubscribeAction(email));

        const url = new URL(`${apiEndpoint}/usersubscriptions`);
        url.searchParams.set('email', email);

        await webClient.post(url.toString(), '', { skipParsingResponse: true, retryCount: 2 });

        dispatch(notificationsSubscribeSuccessAction());
    } catch (err) {
        if (err instanceof ServiceAuthenticationError) {
            dispatch(clearAuthToken());
        }

        dispatch(notificationsSubscribeFailureAction(err));
    }
}
