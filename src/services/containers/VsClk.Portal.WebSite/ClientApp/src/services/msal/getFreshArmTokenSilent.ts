		
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
