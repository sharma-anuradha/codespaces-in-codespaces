import { useActionCreator } from './middleware/useActionCreator';
import { useDispatch } from './middleware/useDispatch';

export const unhandledAsyncErrorActionType = 'async.unhandled.failure';

export function unhandledAsyncError(err: Error) {
    const dispatch = useDispatch();
    const action = useActionCreator();

    dispatch(action(unhandledAsyncErrorActionType, err));
}
