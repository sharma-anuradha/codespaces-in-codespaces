import { URI } from 'vscode-web';
import { PortForwardingExternalUriProvider } from '../externalUriProvider';

jest.mock('../../../../telemetry/telemetry');
jest.mock('../../../../vscode/vscodeAssets/vscode', () => ({
    vscode: {
        URI: class {
            constructor(
                private readonly scheme: string,
                private readonly authority?: string,
                private readonly path?: string,
                private readonly query?: string,
                private readonly fragment?: string
            ) {}
        },
    },
}));

describe('externalUriProvider', () => {
    describe('PortForwardingExternalUriProvider', () => {
        it('inserts environment id and port into configured template', async () => {
            const uriProvider = new PortForwardingExternalUriProvider('{0}.app.vso.io', 'envId');

            const externalUri = await uriProvider.resolveExternalUri({
                scheme: 'https',
                authority: '127.0.0.1:1234',
            } as URI);

            expect(externalUri).toMatchObject({ authority: 'envid-1234.app.vso.io' });
        });

        it('github port forwarding domain', async () => {
            const uriProvider = new PortForwardingExternalUriProvider(
                '{0}.apps.workspaces.githubusercontent.com',
                'envId'
            );

            const externalUri = await uriProvider.resolveExternalUri({
                scheme: 'https',
                authority: '127.0.0.1:1234',
            } as URI);

            expect(externalUri).toMatchObject({
                authority: 'envid-1234.apps.workspaces.githubusercontent.com',
            });
        });

        it('salesforce port forwarding domain', async () => {
            const uriProvider = new PortForwardingExternalUriProvider(
                '{0}-sf.app.online.visualstudio.com',

                'envId'
            );

            const externalUri = await uriProvider.resolveExternalUri({
                scheme: 'https',
                authority: '127.0.0.1:1234',
            } as URI);

            expect(externalUri).toMatchObject({
                authority: 'envid-1234-sf.app.online.visualstudio.com',
            });
        });

        it('vsonline port forwarding domain', async () => {
            const uriProvider = new PortForwardingExternalUriProvider(
                '{0}-vs.app.online.visualstudio.com',
                'envId'
            );

            const externalUri = await uriProvider.resolveExternalUri({
                scheme: 'https',
                authority: '127.0.0.1:1234',
            } as URI);

            expect(externalUri).toMatchObject({
                authority: 'envid-1234-vs.app.online.visualstudio.com',
            });
        });

        it('sets default port to 80 for http', async () => {
            const uriProvider = new PortForwardingExternalUriProvider('{0}.app.vso.io', 'envId');

            const externalUri = await uriProvider.resolveExternalUri({
                scheme: 'http',
                authority: '127.0.0.1',
            } as URI);

            expect(externalUri).toMatchObject({ authority: 'envid-80.app.vso.io' });
        });

        it('sets default port to 443 for https', async () => {
            const uriProvider = new PortForwardingExternalUriProvider('{0}.app.vso.io', 'envId');

            const externalUri = await uriProvider.resolveExternalUri({
                scheme: 'https',
                authority: '127.0.0.1',
            } as URI);

            expect(externalUri).toMatchObject({ authority: 'envid-443.app.vso.io' });
        });

        it('doesn not change hosts we cannot handle in PF', async () => {
            const uriProvider = new PortForwardingExternalUriProvider('{0}.app.vso.io', 'envId');

            const externalUri = await uriProvider.resolveExternalUri({
                scheme: 'https',
                authority: 'giphy.com',
            } as URI);

            expect(externalUri).toMatchObject({ authority: 'giphy.com' });
        });

        it('sets scheme to https for handled hosts', async () => {
            const uriProvider = new PortForwardingExternalUriProvider('{0}.app.vso.io', 'envId');

            const externalUri = await uriProvider.resolveExternalUri({
                scheme: 'http',
                authority: '0.0.0.0',
            } as URI);

            expect(externalUri).toMatchObject({
                authority: 'envid-80.app.vso.io',
                scheme: 'https',
            });
        });

        it('keeps scheme for unhandled hosts', async () => {
            const uriProvider = new PortForwardingExternalUriProvider('{0}.app.vso.io', 'envId');

            const externalUri = await uriProvider.resolveExternalUri({
                scheme: 'http',
                authority: 'giphy.com',
            } as URI);

            expect(externalUri).toMatchObject({ authority: 'giphy.com', scheme: 'http' });
        });

        it('passes query, path, and fragment', async () => {
            const uriProvider = new PortForwardingExternalUriProvider('{0}.app.vso.io', 'envId');

            const externalUri = await uriProvider.resolveExternalUri({
                scheme: 'https',
                authority: 'localhost:1234',
                query: '?search=abc',
                path: '/auth',
                fragment: 'idtoken=secret',
            } as URI);

            expect(externalUri).toMatchObject({
                authority: 'envid-1234.app.vso.io',
                scheme: 'https',
                path: '/auth',
                query: '?search=abc',
                fragment: 'idtoken=secret',
            });
        });

        it('creates new URI object since VSCode expects new instance', async () => {
            const uriProvider = new PortForwardingExternalUriProvider('{0}.app.vso.io', 'envId');

            var originalUri = {
                scheme: 'https',
                authority: 'localhost:1234',
            } as URI;
            const externalUri = await uriProvider.resolveExternalUri(originalUri);

            expect(externalUri).not.toBe(originalUri);
        });
    });
});
