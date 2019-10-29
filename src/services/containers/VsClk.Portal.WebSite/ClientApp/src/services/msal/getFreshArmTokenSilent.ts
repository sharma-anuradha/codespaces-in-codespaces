		
import { ensureRedirectionIframe } from './ensureRedirectionIframe';
import { renewTokenFactory } from './renewTokenFactory';
import { randomString } from '../../utils/randomString';

export const getFreshArmTokenSilentFactory = () => {
    let iframe: HTMLIFrameElement | undefined;
    const iframeId = `js-vso-${randomString()}`;
    
    return renewTokenFactory({
        onCreateRenewEntity: (renewUrl: URL) => {
            iframe = ensureRedirectionIframe(iframeId);
            iframe.src = 'about:blank';
            iframe.src = renewUrl.toString();
        },
        getLocation: () => {
            return (!iframe!.contentWindow)
                ? null
                : iframe!.contentWindow.location;
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
