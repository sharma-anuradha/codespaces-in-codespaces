import { useDispatch } from './middleware/useDispatch';
import { action } from './middleware/useActionCreator';
import { useActionContext } from './middleware/useActionContext';

export type UserInfo = {
    displayName: string;
    mail: string;
    photoUrl?: string;
};

export const getUserInfoActionType = 'async.user.getInfo';
export const getUserInfoSuccessActionType = 'async.user.getInfo.success';
export const getUserInfoFailureActionType = 'async.user.getInfo.failure';

// Basic actions dispatched for reducers
const getUserInfoAction = () => action(getUserInfoActionType);
const getUserInfoSuccessAction = (userInfo: UserInfo) =>
    action(getUserInfoSuccessActionType, { userInfo });
const getUserInfoFailureAction = (error: Error) => action(getUserInfoFailureActionType, error);

// Types to register with reducers
export type GetUserInfoAction = ReturnType<typeof getUserInfoAction>;
export type GetUserInfoSuccessAction = ReturnType<typeof getUserInfoSuccessAction>;
export type GetUserInfoFailureAction = ReturnType<typeof getUserInfoFailureAction>;

// Exposed - callable actions that have side-effects
export async function getUserInfo() {
    const dispatch = useDispatch();
    const { userInfo, authentication } = useActionContext().state;

    if (userInfo) {
        return Promise.resolve(userInfo);
    }

    try {
        dispatch(getUserInfoAction());

        const token = authentication.token;
        if (token) {
            const { idTokenClaims } = token.account;
            const { email, preferred_username } = idTokenClaims;

            const userInfo = {
                displayName: token.account.name,
                mail: email || preferred_username,
                photoUrl: await fetchMyPhoto(),
            };

            dispatch(getUserInfoSuccessAction(userInfo));

            return userInfo;
        } else {
            throw new Error('Unauthenticated.');
        }
    } catch (err) {
        dispatch(getUserInfoFailureAction(err));
    }
}

async function fetchMyPhoto() {
    return undefined;
    /*
       `Note:` since getting the token with the `user.read` scope results in additional consent prompt,
        we commented out this logic until we have a better solution with first-party app.
    */

    // try {
    //     const token = await acquireToken(['user.read']);

    //     if (!token) {
    //         return;
    //     }
    //     
    ///    const defaultPhotoUrl = 'https://graph.microsoft.com/v1.0/me/photos/48x48/$value';
    //     const webClient = useWebClient();
    //     const response = await webClient.request(
    //         defaultPhotoUrl,
    //         {
    //             headers: {
    //                 Authorization: `Bearer ${token.accessToken}`,
    //             },
    //         },
    //         { skipParsingResponse: true }
    //     );
    //     const imageBlob = await response.blob();

    //     return URL.createObjectURL(imageBlob);
    // } catch (err) {
    //     // If the user doesn't have an image then that returns a 404 which results in an exception.
    //     // Simply return empty string here so that it shows the default image.
    //     return defaultPhotoUrl;
    // }
}
