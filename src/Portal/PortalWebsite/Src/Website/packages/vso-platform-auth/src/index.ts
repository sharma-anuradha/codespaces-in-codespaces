
import { PostMessageChannel, authService, createTrace, isInIframe } from 'vso-client-core';

const trace = createTrace(`vso-platform-auth:auth-page`);

if (!isInIframe()) {
    throw new Error('Not in iframe.');
}

const postMessageChannel = new PostMessageChannel();
self.addEventListener('load', async () => {
    try {
        const info = await postMessageChannel.getRepoInfo();

        if (!('vscodeSettings' in info)) {
            (info as any).vscodeSettings = {
                // Salesforce defaults
                defaultSettings: {
                    'workbench.startupEditor': 'welcomePageInEmptyWorkbench',
                    'workbench.colorTheme': 'Codey Midnight',
                    'defaultExtensions': ['salesforce.codey-midnight'],
                },
                vscodeChannel: 'insider',
                productConfiguration: {
                    sendASmile: null
                },
            };
        }

        const result = await authService.storePartnerInfo(info);

        await (result)
            ? postMessageChannel.reportResult('success', 'Nice to deal with you.')
            : postMessageChannel.reportResult('error', 'Could not store the credentials.');

    } catch (e) {
        trace.error(e);

        try {
            await postMessageChannel.reportResult('error', `Unexpected error: ${e.message}`);
        } catch (err) {
            trace.error(err);
            // no-op
        }

        throw e;
    }
});
