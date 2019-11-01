import { Account as MsalAccount } from '@vs/msal';

import { IToken } from './IToken';

export interface ITokenWithMsalAccount extends IToken {
    account: MsalAccount;
}
