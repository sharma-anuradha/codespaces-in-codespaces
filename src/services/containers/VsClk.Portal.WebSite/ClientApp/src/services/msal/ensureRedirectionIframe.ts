const ARM_IFRAME_ID = 'js-vso-arm-iframe';

/**
 * Function to set up the hidden redirection iframe into the DOM.
 */
export const ensureRedirectionIframe = (): HTMLIFrameElement => {
    const currentIframe = document.getElementById(ARM_IFRAME_ID) as HTMLIFrameElement | null;

    if (currentIframe) {
        return currentIframe;
    } else {
        const iframe = document.createElement('iframe');
        iframe.setAttribute(`id`, ARM_IFRAME_ID);
        iframe.style.visibility = 'hidden';
        iframe.style.position = 'absolute';
        iframe.style.width = iframe.style.height = '0';
        iframe.style.border = '0';
        iframe.setAttribute('sandbox', 'allow-same-origin');

        document.body.appendChild(iframe);

        return iframe;
    }
}