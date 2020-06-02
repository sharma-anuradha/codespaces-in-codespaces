import {
    PostMessageChannel,
    createTrace,
    isInIframe,
    randomString,
    ICrossDomainPartnerInfo,
    isGithubTLD,
    PARTNER_INFO_KEYCHAIN_KEY
} from 'vso-client-core';
import { DEFAULT_GITHUB_VSCODE_AUTH_PROVIDER_ID } from '../constants';

const trace = createTrace(`vso-platform-authentication:auth-page`);

/**
 * While we working on the isolated Codespace page platform, we can use the
 * GitHub `postMessage` handshake data to automatically authorize the new platform:
 *  - This will work under local GitHub stamp and inside the Iframe.
 *  - And no prior auth was made (PARTNER_INFO_KEYCHAIN_KEY key).
 * 
 * If all above holds, then the script will get the repo info, transform it to 
 * the `ICrossDomainPlatformInfo` and do top-level POST navigation to the `/platform-authentication`
 * route with the data, implementing the new auth flow. This allows us in meantime piggyback on the
 * current GitHub auth flow while developing the new experience.
 */
export const checkTemporaryGitHubIFrameHandshake = async () => {
    if (location.hostname.split('.')[1] !== 'workspaces-dev' || !isGithubTLD(location.origin)) {
        return;
    }

    const isAuthed = !!localStorage.getItem(PARTNER_INFO_KEYCHAIN_KEY);
    if (!isInIframe() || isAuthed) {
        return;
    }

    const postMessageChannel = new PostMessageChannel('https://github.com');
    self.addEventListener('load', async () => {
        try {
            const info = await postMessageChannel.getRepoInfo(randomString(), 'vso-retrieve-repository-info') as any;
            
            const formEl = document.createElement('form');
            formEl.setAttribute('action', `${location.origin}/platform-authentication`);
            formEl.setAttribute('method', 'POST');
            
            const cascadeTokenInput = document.createElement('input');
            const partnerInfoInput = document.createElement('input');

            cascadeTokenInput.name = 'cascadeToken';
            cascadeTokenInput.value = info.cascadeToken;

            const data: ICrossDomainPartnerInfo = {
                partnerName: 'github',
                // where to redirect to in case the credentials expire
                managementPortalUrl: 'https://github.com/codespaces',
                cascadeToken: info.cascadeToken,
                credentials: [
                    {
                        // Sat Nov 20 2286 09:46:40 GMT-0800 (Pacific Standard Time)
                        expiration: 10000000000000,
                        token: info.githubToken,
                        host: 'github.com',
                        path: '/'
                    }
                ],
                codespaceId: info.workspaceId,
                vscodeSettings: {
                    // set the GitHub theme as default, note that this will work only
                    //  - on fresh codespace
                    //  - and if the settings sync service is not turned on (or user using the default theme)
                    defaultSettings: {'workbench.colorTheme': 'GitHub Light', 'workbench.startupEditor': 'welcomePageInEmptyWorkbench'},
                    // go home button
                    homeIndicator: {
                        icon: 'github-inverted',
                        href: 'https://github.com/codespaces',
                        title: 'Go Home',
                    },
                    // list of the VSCode extension ids that should be installed on the first codespace run
                    // hence user has the option to remove the extensions explicitelly
                    defaultExtensions: [
                        'GitHub.vscode-pull-request-github',
                        'github.github-vscode-theme',
                        'ms-vsliveshare.vsliveshare'
                    ],
                    // settings sync / native auth providers
                    enableSyncByDefault: true,
                    authenticationSessionId: DEFAULT_GITHUB_VSCODE_AUTH_PROVIDER_ID,
                    defaultAuthSessions: [
                        // the default auth session used for Settings Sync Service auth
                        {
                            type: 'github',
                            id: DEFAULT_GITHUB_VSCODE_AUTH_PROVIDER_ID,
                            accessToken: info.githubToken,
                            scopes: ['email'],
                        },
                        // used by GH PR extension
                        {
                            type: 'github',
                            id: 'github-session-github-pr',
                            accessToken: info.githubToken,
                            scopes: ['read:user', 'user:email', 'repo'].sort(),
                        }
                    ]
                },
            };

            partnerInfoInput.name = 'partnerInfo';
            partnerInfoInput.value = JSON.stringify(data);

            formEl.appendChild(cascadeTokenInput);
            formEl.appendChild(partnerInfoInput);
            document.body.append(formEl);

            formEl.submit();
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
}