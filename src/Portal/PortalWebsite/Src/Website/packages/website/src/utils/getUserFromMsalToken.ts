import { ITokenWithMsalAccount } from '../typings/ITokenWithMsalAccount';
import { IUser } from '../interfaces/IUser';

export const getUserFromMsalToken = (token: ITokenWithMsalAccount): IUser => {
    const { email, preferred_username } = token.account.idTokenClaims;

    const user: IUser = {
        email: email || preferred_username,
        username: token.account.userName,
        name: token.account.name
    };

    return user;
};
