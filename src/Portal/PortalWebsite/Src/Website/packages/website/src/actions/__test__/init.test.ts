import { init, initActionType, initActionSuccessType } from '../init';
import {
    createMockStore,
    MockStore,
    createMockMakeRequestFactory,
    test_setMockRequestFactory,
    testMsalToken,
} from '../../utils/testUtils';

import * as acquireTokenModule from '../../services/acquireToken';
import { authService } from '../../services/authService';

import { defaultConfig, configurationEndpoint } from '../../services/configurationService';
import { getAuthToken } from '../getAuthToken';
import { defaultLocations } from '../../reducers/locations';

jest.mock('../getUserInfo', () => {
    return {
        getUserInfo: jest.fn().mockReturnValue({ mail: 'test@test.com', displayName: 'test' }),
    };
});

jest.mock('vso-service-worker-client', () => {
    return {
        registerServiceWorker: jest.fn(),
        onMessage: jest.fn(),
    };
});

jest.mock('../../utils/setAuthCookie', () => {
    return {
        setAuthCookie: jest.fn().mockReturnValue(Promise.resolve()),
        deleteAuthCookie: jest.fn(),
    };
});

jest.mock('../../services/ExperimentationService');

describe('actions - init', () => {
    let store: MockStore;

    beforeEach(() => {
        store = createMockStore();
    });

    it('succeeds', async () => {
        jest.spyOn(authService, 'getCachedToken').mockReturnValue(Promise.resolve(testMsalToken));
        jest.spyOn(acquireTokenModule, 'acquireTokenSilentWith2FA').mockReturnValue(
            Promise.resolve(testMsalToken)
        );

        test_setMockRequestFactory(
            createMockMakeRequestFactory({
                responses: [
                    // don't care about config
                    {
                        body: defaultConfig,
                    },
                    {
                        body: defaultLocations,
                    },
                    // get some environments (or empty ¯\_(ツ)_/¯)
                    {
                        body: [],
                    },
                    {
                        body: [],
                    },
                ],
            })
        );
        await store.dispatch(init(getAuthToken));

        expect(store.dispatchedActions).not.toHaveFailed();
        expect(store.dispatchedActions).toBeHaveBeenDispatched(initActionType);
        expect(store.dispatchedActions).toBeHaveBeenDispatched(initActionSuccessType);
    });

    it('uses the right configuration', async () => {
        const environmentRegistrationEndpoint = 'https://random.com/api/v1/environments';
        const apiEndpoint = 'https://random.com/api/v1';
        jest.spyOn(authService, 'getCachedToken').mockReturnValue(Promise.resolve(testMsalToken));
        jest.spyOn(acquireTokenModule, 'acquireTokenSilentWith2FA').mockReturnValue(
            Promise.resolve(testMsalToken)
        );

        const mockFetch = jest.fn().mockImplementation((url: string) => {
            if (url === configurationEndpoint) {
                return {
                    ok: true,
                    json: () =>
                        Promise.resolve({
                            environmentRegistrationEndpoint,
                            apiEndpoint,
                        }),
                    headers: new Headers(),
                };
            }

            if (url === `${apiEndpoint}/plans`) {
                return {
                    ok: true,
                    json: () => Promise.resolve([]),
                    headers: new Headers(),
                };
            }

            return {
                ok: true,
                json: () => Promise.resolve({}),
                headers: new Headers(),
            };
        });
        test_setMockRequestFactory(mockFetch);

        await store.dispatch(init(getAuthToken));

        expect(store.dispatchedActions).not.toHaveFailed();
        expect(mockFetch).toHaveBeenCalledWith(
            environmentRegistrationEndpoint,
            expect.objectContaining({ method: 'GET' })
        );
    });

    it('fails to get auth token', async () => {
        jest.spyOn(authService, 'getCachedToken').mockReturnValue(Promise.resolve(undefined));
        jest.spyOn(acquireTokenModule, 'acquireTokenSilentWith2FA').mockReturnValue(
            Promise.resolve(undefined!)
        );

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

        try {
            await store.dispatch(init(getAuthToken));
        } catch {
            expect(store.dispatchedActions).toHaveFailed();
            expect(store.dispatchedActions).toHaveBeenDispatchedInOrder(
                'async.app.init',
                'async.authentication.getToken',
                'async.configuration.fetch',
                'async.authentication.getToken.failure',
                'async.app.init.failure'
            );
        }
    });

    it('fails to fetch environments', async () => {
        jest.spyOn(authService, 'getCachedToken').mockReturnValue(Promise.resolve(testMsalToken));
        jest.spyOn(acquireTokenModule, 'acquireTokenSilentWith2FA').mockReturnValue(
            Promise.resolve(testMsalToken)
        );

        test_setMockRequestFactory(
            createMockMakeRequestFactory({
                responses: [
                    // don't care about config
                    {
                        body: defaultConfig,
                    },
                    {
                        body: defaultLocations,
                        requestDelay: 10,
                    },
                    {
                        body: [],
                    },
                    {
                        // get some environments (or empty ¯\_(ツ)_/¯)
                        status: 401,
                    },
                ],
            })
        );

        try {
            await store.dispatch(init(getAuthToken));
        } catch {
            expect(store.dispatchedActions).toHaveFailed();
            expect(store.dispatchedActions).toHaveBeenDispatchedInOrder(
                'async.app.init',
                'async.authentication.getToken',
                'async.configuration.fetch',
                'async.authentication.getToken.success',
                'async.configuration.fetch.success',
                'async.locations.getLocations',
                'async.locations.getLocations.success',
                'async.plans.getPlans',
                'async.plans.getPlans.success',
                'async.environments.fetch',
                'async.environments.fetch.failure',
                'async.app.init.failure'
            );
        }
    });
});
