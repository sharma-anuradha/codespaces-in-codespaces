import { action } from './middleware/useActionCreator';
import { useDispatch } from './middleware/useDispatch';
import { useActionContext } from './middleware/useActionContext';
import { useWebClient } from './middleware/useWebClient';

export const createPortForwardingConnectionActionType = 'async.portForwardingConnection.create';
export const createPortForwardingConnectionActionNotSupportedType =
    'async.portForwardingConnection.create.notSupported';
export const createPortForwardingConnectionSuccessActionType =
    'async.portForwardingConnection.create.success';
export const createPortForwardingConnectionFailureActionType =
    'async.portForwardingConnection.create.failure';

// Basic actions dispatched for reducers
const createPortForwardingConnectionAction = () => action(createPortForwardingConnectionActionType);
const createPortForwardingConnectionSuccessAction = (id: string, port: number) =>
    action(createPortForwardingConnectionSuccessActionType, { id, port });
const createPortForwardingConnectionFailureAction = (error: Error) =>
    action(createPortForwardingConnectionFailureActionType, error);

// Types to register with reducers
export type CreatePortForwardingConnectionAction = ReturnType<
    typeof createPortForwardingConnectionAction
>;
export type CreatePortForwardingConnectionSuccessAction = ReturnType<
    typeof createPortForwardingConnectionSuccessAction
>;
export type CreatePortForwardingConnectionFailureAction = ReturnType<
    typeof createPortForwardingConnectionFailureAction
>;

// Exposed - callable actions that have side-effects
export async function createPortForwardingConnection(id: string, port: number) {
    const dispatch = useDispatch();
    const webClient = useWebClient();

    try {
        dispatch(createPortForwardingConnectionAction());
        const {
            state: { configuration },
        } = useActionContext();

        if (!configuration) {
            throw new Error('Fetch configuration first.');
        }

        if (!configuration.portForwardingServiceEnabled) {
            dispatch(action(createPortForwardingConnectionActionNotSupportedType));
            return;
        }

        await webClient.post(configuration.portForwardingManagementEndpoint, {
            id,
            port,
        }, { skipParsingResponse: true });

        dispatch(createPortForwardingConnectionSuccessAction(id, port));
    } catch (err) {
        return dispatch(createPortForwardingConnectionFailureAction(err));
    }
}
