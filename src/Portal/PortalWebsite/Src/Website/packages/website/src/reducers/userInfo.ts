import {
    getUserInfoSuccessActionType,
    GetUserInfoSuccessAction,
    UserInfo,
} from '../actions/getUserInfo';

type UserInfoState = UserInfo | null;
type AcceptedActions = GetUserInfoSuccessAction;

export function userInfo(
    state: UserInfoState | undefined = null,
    action: AcceptedActions
): UserInfoState {
    switch (action.type) {
        case getUserInfoSuccessActionType:
            return action.payload.userInfo;
        default:
            return state;
    }
}
