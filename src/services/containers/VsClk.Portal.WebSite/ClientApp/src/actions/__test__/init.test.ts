import { init, initActionType, initActionSuccessType } from '../init';
import {
    createMockStore,
    MockStore,
    createMockMakeRequestFactory,
    authenticated,
    test_setMockRequestFactory,
} from '../../utils/testUtils';
import * as Auth from '../../services/authService';
import { defaultConfig, configurationEndpoint } from '../../services/configurationService';

jest.mock('../getUserInfo', () => {
    return {
        getUserInfo: jest.fn().mockReturnValue({ mail: 'test@test.com', displayName: 'test' }),
    };
});

jest.mock('../../serviceWorker', () => {
    return {
        register: jest.fn(),
    };
});

jest.mock('../../utils/setAuthCookie', () => {
    return {
        setAuthCookie: jest.fn().mockReturnValue(Promise.resolve()),
    };
});

describe('actions - init', () => {
    let store: MockStore;

    beforeEach(() => {
        store = createMockStore();
    });

    it('succeeds', async () => {
        jest.spyOn(Auth.authService, 'getCachedToken').mockReturnValue(
            Promise.resolve(authenticated.token)
        );
        jest.spyOn(Auth, 'acquireToken').mockReturnValue(Promise.resolve(authenticated.token));

        test_setMockRequestFactory(
            createMockMakeRequestFactory({
                responses: [
                    // don't care about config
                    {
                        body: defaultConfig,
                    },
                    // get some environments (or empty ¯\_(ツ)_/¯)
                    {
                        body: [],
                    },
                ],
            })
        );
        await store.dispatch(init());

        expect(store.dispatchedActions).not.toHaveFailed();
        expect(store.dispatchedActions).toBeHaveBeenDispatched(initActionType);
        expect(store.dispatchedActions).toBeHaveBeenDispatched(initActionSuccessType);
    });

    it('uses the right configuration', async () => {
        const environmentRegistrationEndpoint = 'https://random.com/api/v1/environments';
        jest.spyOn(Auth.authService, 'getCachedToken').mockReturnValue(
            Promise.resolve(authenticated.token)
        );
        jest.spyOn(Auth, 'acquireToken').mockReturnValue(Promise.resolve(authenticated.token));

        const mockFetch = jest.fn().mockImplementation((url: string) => {
            if (url === configurationEndpoint) {
                return {
                    ok: true,
                    json: async () =>
                        Promise.resolve({
                            environmentRegistrationEndpoint,
                        }),
                };
            }
            return {
                ok: true,
                json: async () => Promise.resolve({}),
            };
        });
        test_setMockRequestFactory(mockFetch);

        await store.dispatch(init());

        expect(store.dispatchedActions).not.toHaveFailed();
        expect(mockFetch).toHaveBeenCalledWith(
            environmentRegistrationEndpoint,
            expect.objectContaining({ method: 'GET' })
        );
    });

    it('fails to get auth token', async () => {
        jest.spyOn(Auth.authService, 'getCachedToken').mockReturnValue(Promise.resolve(undefined));
        jest.spyOn(Auth, 'acquireToken').mockReturnValue(Promise.resolve(undefined!));

        test_setMockRequestFactory(
            createMockMakeRequestFactory({
                responses: [
                    // don't care about config
                    {
                        body: defaultConfig,
                    },
                    // get some environments (or empty ¯\_(ツ)_/¯)
                    {
                        body: [],
                    },
                ],
            })
        );

        await store.dispatch(init());

        expect(store.dispatchedActions).toHaveFailed();
        expect(store.dispatchedActions).toHaveBeenDispatchedInOrder(
            'async.app.init',
            'async.authentication.getToken',
            'async.configuration.fetch',
            'async.authentication.getToken.failure',
            'async.app.init.failure'
        );
    });

    it('fails to fetch environments', async () => {
        jest.spyOn(Auth.authService, 'getCachedToken').mockReturnValue(
            Promise.resolve(authenticated.token)
        );
        jest.spyOn(Auth, 'acquireToken').mockReturnValue(Promise.resolve(authenticated.token));

        test_setMockRequestFactory(
            createMockMakeRequestFactory({
                responses: [
                    // don't care about config
                    {
                        body: defaultConfig,
                    },
                    // get some environments (or empty ¯\_(ツ)_/¯)
                    {
                        status: 401,
                    },
                ],
            })
        );

        await store.dispatch(init());

        expect(store.dispatchedActions).toHaveFailed();
        expect(store.dispatchedActions).toHaveBeenDispatchedInOrder(
            'async.app.init',
            'async.authentication.getToken',
            'async.configuration.fetch',
            'async.authentication.getToken.success',
            'async.configuration.fetch.success',
            'async.environments.fetch',
            'async.authentication.clearData',
            'async.environments.fetch.failure',
            'async.app.init.failure'
        );
    });
});
