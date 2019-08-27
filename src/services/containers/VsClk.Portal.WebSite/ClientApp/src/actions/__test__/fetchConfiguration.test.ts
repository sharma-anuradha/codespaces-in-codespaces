import {
    fetchConfiguration,
    fetchConfigurationActionType,
    fetchConfigurationSuccessActionType,
    fetchConfigurationFailureActionType,
} from '../fetchConfiguration';
import {
    createMockStore,
    MockStore,
    test_setMockRequestFactory,
    createMockMakeRequestFactory,
    getDispatchedAction,
} from '../../utils/testUtils';
import { ServiceContentError } from '../middleware/useWebClient';

describe('fetchConfiguration', () => {
    let store: MockStore;

    beforeEach(() => {
        store = createMockStore();
    });

    it('fetches', async () => {
        test_setMockRequestFactory(
            createMockMakeRequestFactory({
                responses: [
                    {
                        body: {
                            theBestService: '/yes/it/is',
                        },
                    },
                ],
            })
        );

        await store.dispatch(fetchConfiguration());

        expect(store.dispatchedActions).toBeHaveBeenDispatched(fetchConfigurationActionType);
        expect(store.dispatchedActions).toBeHaveBeenDispatched(fetchConfigurationSuccessActionType);
        expect(store.dispatchedActions).not.toBeHaveBeenDispatched(
            fetchConfigurationFailureActionType
        );
    });

    it('fails', async () => {
        test_setMockRequestFactory(
            createMockMakeRequestFactory({
                responses: [
                    {
                        body: '<div>Failed</div>',
                    },
                ],
            })
        );

        await store.dispatch(fetchConfiguration());

        expect(store.dispatchedActions).toBeHaveBeenDispatched(fetchConfigurationActionType);
        expect(store.dispatchedActions).toHaveFailed();
        expect(
            getDispatchedAction(store.dispatchedActions, fetchConfigurationFailureActionType)!.error
        ).toBeInstanceOf(ServiceContentError);
    });
});
