import { Account as MsalAccount } from 'msal';

import { IToken } from './IToken';

export interface ITokenWithMsalAccount extends IToken {
    account: MsalAccount;
}
