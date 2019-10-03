import { InMemoryLiveShareClient } from '../inMemoryClient';
import { IWorkspaceInfo, IWorkspaceAccess } from '../ILiveShareClient';

describe('inMemoryClient', () => {
    let client: InMemoryLiveShareClient;

    beforeEach(() => {
        jest.useFakeTimers();

        client = new InMemoryLiveShareClient();
    });
    afterEach(() => {
        jest.runAllTimers();

        client.dispose();
    });

    describe('workspaceInfo', () => {
        const invitationId = '42';

        it('returns workspace info request', async () => {
            const workspaceInfoRequest = client.getWorkspaceInfo(invitationId);

            const response = {
                id: 'test-workspace-info',
            } as IWorkspaceInfo;

            client.setWorkspaceInfo(invitationId, response);

            expect(await workspaceInfoRequest).toBe(response);
        });

        it('returns resolved info request', async () => {
            const response = {
                id: 'test-workspace-info',
            } as IWorkspaceInfo;
            client.setWorkspaceInfo(invitationId, response);

            const workspaceInfoRequest = client.getWorkspaceInfo(invitationId);

            expect(await workspaceInfoRequest).toBe(response);
        });

        it('times out in 30s', async () => {
            const workspaceInfoRequest = client.getWorkspaceInfo(invitationId);

            expect(workspaceInfoRequest).resolves.toBe(null);
            jest.runTimersToTime(30 * 1000);
        });

        it('returns the same request', async () => {
            const workspaceInfoRequest1 = client.getWorkspaceInfo(invitationId);
            const workspaceInfoRequest2 = client.getWorkspaceInfo(invitationId);

            const response = {
                id: 'test-workspace-info',
            } as IWorkspaceInfo;
            client.setWorkspaceInfo(invitationId, response);

            expect(workspaceInfoRequest1).toBe(workspaceInfoRequest2);
        });
    });

    describe('getWorkspaceAccess', () => {
        const workspaceId = '1';

        it('returns workspace info request', async () => {
            const workspaceInfoRequest = client.getWorkspaceAccess(workspaceId);

            const response = {
                sessionToken: '123',
            } as IWorkspaceAccess;

            client.setWorkspaceAccess(workspaceId, response);

            expect(await workspaceInfoRequest).toBe(response);
        });

        it('returns resolved info request', async () => {
            const response = {
                sessionToken: '123',
            } as IWorkspaceAccess;

            client.setWorkspaceAccess(workspaceId, response);

            const workspaceInfoRequest = client.getWorkspaceAccess(workspaceId);

            expect(await workspaceInfoRequest).toBe(response);
        });

        it('returns the same request', async () => {
            const workspaceAccessRequest1 = client.getWorkspaceAccess(workspaceId);
            const workspaceAccessRequest2 = client.getWorkspaceAccess(workspaceId);

            const response = {
                sessionToken: '123',
            } as IWorkspaceAccess;
            client.setWorkspaceAccess(workspaceId, response);

            expect(workspaceAccessRequest1).toBe(workspaceAccessRequest2);
        });
    });
});
