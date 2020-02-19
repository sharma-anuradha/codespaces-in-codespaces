		
/* tslint:disable */

import { ensureRedirectionIframe } from './ensureRedirectionIframe';
import { renewTokenFactory, IRenewTokenFactoryOptions } from './renewTokenFactory';
import { randomString } from '../../utils/randomString';

export const getFreshArmTokenSilentFactory = (paramOfInterest: 'access_token' | 'code', mode: 'hash' | 'query') => {
    let iframe: HTMLIFrameElement | undefined;
    const iframeId = `js-vso-${randomString()}`;

    return renewTokenFactory({
        paramOfInterest,
        mode,
        onCreateRenewEntity: (renewUrl: URL) => {
            iframe = ensureRedirectionIframe(iframeId);
            iframe.src = 'about:blank';
            iframe.src = renewUrl.toString();
        },
        getLocation: () => {
            try {
                const location = (!iframe!.contentWindow)
                ? null
                : iframe!.contentWindow.location;

                return location;
            } catch {
                // ignore
                return null;
            }
        },
        onComplete: () => {
            const iframeEl = document.getElementById(iframeId);

            if (iframeEl) {
                iframeEl.remove();
            }

            iframe = undefined;
        }
    });
}
