import { renewTokenFactory } from './renewTokenFactory';
import { CancellationTokenSource } from 'vscode-jsonrpc';

let win: Window | null;

let cancellationTokenSource: CancellationTokenSource | null;

export const getFreshArmTokenPopup = renewTokenFactory({
    paramOfInterest: 'access_token',
    mode: 'hash',
    onCreateRenewEntity: (renewUrl: URL) => {
        const popUpWidth = 485;
        const popUpHeight = 600;
         /**
         * adding winLeft and winTop to account for dual monitor
         * using screenLeft and screenTop for IE8 and earlier
         */
        const winLeft = window.screenLeft ? window.screenLeft : window.screenX;
        const winTop = window.screenTop ? window.screenTop : window.screenY;
        /**
         * window.innerWidth displays browser window"s height and width excluding toolbars
         * using document.documentElement.clientWidth for IE8 and earlier
         */
        const width = window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth;
        const height = window.innerHeight || document.documentElement.clientHeight || document.body.clientHeight;
        const left = ((width / 2) - (popUpWidth / 2)) + winLeft;
        const top = ((height / 2) - (popUpHeight / 2)) + winTop;
        renewUrl.searchParams.set('prompt', 'select_account');
        win = window.open(renewUrl.toString(), 'popup', `width=${popUpWidth}, height=${popUpHeight}, top=${top}, left=${left}`);
        
        if (!win) {
            throw new Error('Cannot create popup window.');
        }

        if (win.focus) {
            win.focus();
        }

        cancellationTokenSource = new CancellationTokenSource();

        return cancellationTokenSource.token;
    },
    getLocation: () => {
        try {
            if (win!.closed) {
                if (cancellationTokenSource) {
                    cancellationTokenSource.cancel();
                }

                return null;
            }

            return (win!.location.href === 'about:blank')
                ? null
                : win!.location;

        } catch (e) {
            return null;
        }
    },
    onComplete: () => {
        try {
            if (win && !win.closed) {
                win.close();
            }
        } finally {
            win = null;
            cancellationTokenSource = null;
        }
    }
});
