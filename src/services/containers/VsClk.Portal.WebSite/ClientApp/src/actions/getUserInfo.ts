// import { acquireToken, IToken } from '../services/authService';

import { useDispatch } from './middleware/useDispatch';
import { action } from './middleware/useActionCreator';
import { IToken, acquireToken } from '../services/authService';
import { useWebClient } from './middleware/useWebClient';
import { useActionContext } from './middleware/useActionContext';

export type UserInfo = {
    displayName: string;
    mail: string;
    photoUrl: string;
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
    const { userInfo } = useActionContext().state;

    if (userInfo) {
        return Promise.resolve(userInfo);
    }

    try {
        dispatch(getUserInfoAction());

        const token = await acquireToken(['user.read']);

        const [content, photoUrl = defaultPhotoUrl] = await Promise.all([
            fetchMyInfo(token),
            fetchMyPhoto(token),
        ]);
        const userInfo = {
            ...content,
            photoUrl,
        };

        dispatch(getUserInfoSuccessAction(userInfo));

        return userInfo;
    } catch (err) {
        dispatch(getUserInfoFailureAction(err));
    }
}

const graphEndpoint = 'https://graph.microsoft.com/v1.0/me';
async function fetchMyInfo(token: IToken) {
    const webClient = useWebClient();

    const userInfo = await webClient.request<UserInfo>(graphEndpoint, {
        headers: {
            Authorization: `Bearer ${token.accessToken}`,
        },
    });

    return userInfo;
}

export const defaultPhotoUrl = 'https://graph.microsoft.com/v1.0/me/photos/48x48/$value';
async function fetchMyPhoto(token: IToken) {
    const webClient = useWebClient();

    try {
        const response = await webClient.request(
            defaultPhotoUrl,
            {
                headers: {
                    Authorization: `Bearer ${token.accessToken}`,
                },
            },
            { skipParsingResponse: true }
        );    
        const imageBlob = await response.blob();

        return URL.createObjectURL(imageBlob);
    } catch (err) {
        // If the user doesn't have an image then that returns a 404 which results in an exception.
        // Simply return empty string here so that it shows the default image.
        return "";
    } 
}
