import {
    authenticated,
    createMockMakeRequestFactory,
    MockStore,
    createMockStore,
    getDispatchedAction,
    test_setMockRequestFactory,
    test_setApplicationState,
} from '../../../utils/testUtils';
import { useDispatch } from '../useDispatch';
import { useActionCreator } from '../useActionCreator';
import { wait } from '../../../dependencies';

import {
    useWebClient,
    ServiceAuthenticationError,
    ServiceConnectionError,
    ServiceResponseError,
    ServiceContentError,
} from '../useWebClient';

jest.mock('../../../services/authService', () => {
    return {
        authService: {
            getCachedToken: async () => {
                return 'AAD token value';
            }
        },
    };
});

describe('actionContextProvider', () => {
    let store: MockStore;

    beforeEach(() => {
        store = createMockStore();
    });

    describe('webClient', () => {
        it('can talk to a service', async () => {
            test_setMockRequestFactory(
                createMockMakeRequestFactory({
                    responses: [
                        {
                            body: {
                                allGood: true,
                            },
                        },
                    ],
                })
            );
            test_setApplicationState({
                authentication: authenticated,
            });

            async function act() {
                const dispatch = useDispatch();
                const action = useActionCreator();
                const webClient = useWebClient();

                try {
                    dispatch(action('hello'));
                    await webClient.get('/');
                    dispatch(action('hello.success'));
                } catch (err) {
                    dispatch(action('hello.failure', err));
                }
            }

            await store.dispatch(act());

            expect(store.dispatchedActions).not.toHaveFailed();

            expect(store.dispatchedActions).toBeHaveBeenDispatched('hello');
            expect(store.dispatchedActions).toBeHaveBeenDispatched('hello.success');
            expect(store.dispatchedActions).not.toBeHaveBeenDispatched('hello.failure');
        });

        it('fails on missing token', async () => {
            test_setMockRequestFactory(
                createMockMakeRequestFactory({
                    responses: [
                        {
                            body: {
                                allGood: true,
                            },
                        },
                    ],
                })
            );

            async function act() {
                const dispatch = useDispatch();
                const action = useActionCreator();
                const webClient = useWebClient();

                try {
                    dispatch(action('hello'));
                    await webClient.get('/');
                    dispatch(action('hello.success'));
                } catch (err) {
                    dispatch(action('hello.failure', err));
                }
            }

            try {
                await store.dispatch(act());
            } catch {
                expect(store.dispatchedActions).toBeHaveBeenDispatched('hello');
                expect(store.dispatchedActions).not.toBeHaveBeenDispatched('hello.success');
                expect(store.dispatchedActions).toBeHaveBeenDispatched('hello.failure');
                expect(
                    getDispatchedAction(store.dispatchedActions, 'hello.failure')!.error
                ).toBeInstanceOf(ServiceAuthenticationError);
            }
        });

        it('fails on failed connection request', async () => {
            test_setMockRequestFactory(
                createMockMakeRequestFactory({
                    shouldFailConnection: true,
                })
            );
            test_setApplicationState({
                authentication: authenticated,
            });

            async function act() {
                const dispatch = useDispatch();
                const action = useActionCreator();
                const webClient = useWebClient();

                try {
                    dispatch(action('hello'));
                    await webClient.get('/');
                    dispatch(action('hello.success'));
                } catch (err) {
                    dispatch(action('hello.failure', err));
                }
            }

            try {
                await store.dispatch(act());
            } catch {
                expect(store.dispatchedActions).toBeHaveBeenDispatched('hello');
                expect(store.dispatchedActions).not.toBeHaveBeenDispatched('hello.success');
                expect(store.dispatchedActions).toBeHaveBeenDispatched('hello.failure');
                expect(
                    getDispatchedAction(store.dispatchedActions, 'hello.failure').error
                ).toBeInstanceOf(ServiceConnectionError);
            }
        });

        it('fails on 401', async () => {
            test_setMockRequestFactory(
                createMockMakeRequestFactory({
                    responses: [
                        {
                            status: 401,
                            body: {},
                        },
                    ],
                })
            );
            test_setApplicationState({
                authentication: authenticated,
            });

            async function act() {
                const dispatch = useDispatch();
                const action = useActionCreator();
                const webClient = useWebClient();

                try {
                    dispatch(action('hello'));
                    await webClient.get('/');
                    dispatch(action('hello.success'));
                } catch (err) {
                    dispatch(action('hello.failure', err));
                }
            }

            try {
                await store.dispatch(act());
            } catch {
                expect(store.dispatchedActions).toBeHaveBeenDispatched('hello');
                expect(store.dispatchedActions).not.toBeHaveBeenDispatched('hello.success');
                expect(store.dispatchedActions).toBeHaveBeenDispatched('hello.failure');
                expect(
                    getDispatchedAction(store.dispatchedActions, 'hello.failure').error
                ).toBeInstanceOf(ServiceAuthenticationError);
            }
        });

        it('fails on non 401 error', async () => {
            test_setMockRequestFactory(
                createMockMakeRequestFactory({
                    responses: [
                        {
                            ok: false,
                            body: {},
                        },
                    ],
                })
            );
            test_setApplicationState({
                authentication: authenticated,
            });

            async function act() {
                const dispatch = useDispatch();
                const action = useActionCreator();
                const webClient = useWebClient();

                try {
                    dispatch(action('hello'));
                    await webClient.get('/');
                    dispatch(action('hello.success'));
                } catch (err) {
                    dispatch(action('hello.failure', err));
                }
            }

            try {
                await store.dispatch(act());
            } catch {
                expect(store.dispatchedActions).toBeHaveBeenDispatched('hello');
                expect(store.dispatchedActions).not.toBeHaveBeenDispatched('hello.success');
                expect(store.dispatchedActions).toBeHaveBeenDispatched('hello.failure');
                expect(
                    getDispatchedAction(store.dispatchedActions, 'hello.failure').error
                ).toBeInstanceOf(ServiceResponseError);
            }
        });

        it('fails on invalid content', async () => {
            test_setMockRequestFactory(
                createMockMakeRequestFactory({
                    responses: [
                        {
                            ok: true,
                            body: '<div>Not JSON</div>',
                        },
                    ],
                })
            );
            test_setApplicationState({
                authentication: authenticated,
            });

            async function act() {
                const dispatch = useDispatch();
                const action = useActionCreator();
                const webClient = useWebClient();

                try {
                    dispatch(action('hello'));
                    await webClient.get('/');
                    dispatch(action('hello.success'));
                } catch (err) {
                    dispatch(action('hello.failure', err));
                }
            }

            try {
                await store.dispatch(act());
            } catch {
                expect(store.dispatchedActions).toBeHaveBeenDispatched('hello');
                expect(store.dispatchedActions).not.toBeHaveBeenDispatched('hello.success');
                expect(store.dispatchedActions).toBeHaveBeenDispatched('hello.failure');
                expect(
                    getDispatchedAction(store.dispatchedActions, 'hello.failure').error
                ).toBeInstanceOf(ServiceContentError);
            }
        });
    });

    it('can skip parsing content', async () => {
        test_setMockRequestFactory(
            createMockMakeRequestFactory({
                responses: [
                    {
                        ok: true,
                        body: '',
                    },
                ],
            })
        );
        test_setApplicationState({
            authentication: authenticated,
        });

        async function act() {
            const dispatch = useDispatch();
            const action = useActionCreator();
            const webClient = useWebClient();

            try {
                dispatch(action('hello'));
                await webClient.request('/', { method: 'GET' }, { skipParsingResponse: true });
                dispatch(action('hello.success'));
            } catch (err) {
                dispatch(action('hello.failure', err));
            }
        }

        await store.dispatch(act());

        expect(store.dispatchedActions).not.toHaveFailed();
    });

    it('no retries by default on 500s', async () => {
        jest.setTimeout(8000);

        test_setMockRequestFactory(
            createMockMakeRequestFactory({
                responses: [
                    {
                        status: 500,
                    },
                    {
                        ok: true,
                    },
                ],
            })
        );

        async function act() {
            const dispatch = useDispatch();
            const action = useActionCreator();
            const webClient = useWebClient();

            try {
                dispatch(action('hello'));
                await webClient.request(
                    '/',
                    { method: 'GET' },
                    { skipParsingResponse: true, requiresAuthentication: false }
                );
                dispatch(action('hello.success'));
            } catch (err) {
                dispatch(action('hello.failure', err));
            }
        }

        try {
            await store.dispatch(act());
        } catch {
            expect(store.dispatchedActions).toBeHaveBeenDispatched('hello');
            expect(store.dispatchedActions).not.toBeHaveBeenDispatched('hello.success');
            expect(store.dispatchedActions).toBeHaveBeenDispatched('hello.failure');
            expect(
                getDispatchedAction(store.dispatchedActions, 'hello.failure').error
            ).toBeInstanceOf(ServiceResponseError);
        }
    });

    it('retries on failed connection errors', async () => {
        jest.setTimeout(8000);

        test_setMockRequestFactory(
            createMockMakeRequestFactory({
                responses: [
                    {
                        shouldFailConnection: true,
                    },
                    {
                        ok: true,
                    },
                ],
            })
        );

        async function act() {
            const dispatch = useDispatch();
            const action = useActionCreator();
            const webClient = useWebClient();

            try {
                dispatch(action('hello'));
                await webClient.request(
                    '/',
                    { method: 'GET' },
                    {
                        requiresAuthentication: false,
                        skipParsingResponse: true,
                        retryCount: 2,
                    }
                );
                dispatch(action('hello.success'));
            } catch (err) {
                dispatch(action('hello.failure', err));
            }
        }

        await store.dispatch(act());

        expect(store.dispatchedActions).not.toHaveFailed();
    });

    it('retries on 500s', async () => {
        jest.setTimeout(8000);

        test_setMockRequestFactory(
            createMockMakeRequestFactory({
                responses: [
                    {
                        status: 500,
                    },
                    {
                        ok: true,
                    },
                ],
            })
        );

        async function act() {
            const dispatch = useDispatch();
            const action = useActionCreator();
            const webClient = useWebClient();

            try {
                dispatch(action('hello'));
                await webClient.request(
                    '/',
                    { method: 'GET' },
                    {
                        requiresAuthentication: false,
                        skipParsingResponse: true,
                        retryCount: 2,
                    }
                );
                dispatch(action('hello.success'));
            } catch (err) {
                dispatch(action('hello.failure', err));
            }
        }

        await store.dispatch(act());

        expect(store.dispatchedActions).not.toHaveFailed();
    });

    it('retries 2 times (total 3 requests), then fails', async () => {
        jest.setTimeout(8000);

        test_setMockRequestFactory(
            createMockMakeRequestFactory({
                responses: [
                    {
                        status: 500,
                    },
                    {
                        status: 500,
                    },
                    {
                        status: 500,
                    },
                    {
                        ok: true,
                    },
                ],
            })
        );

        async function act() {
            const dispatch = useDispatch();
            const action = useActionCreator();
            const webClient = useWebClient();

            try {
                dispatch(action('hello'));
                await webClient.request(
                    '/',
                    { method: 'GET' },
                    {
                        requiresAuthentication: false,
                        skipParsingResponse: true,
                        retryCount: 2,
                    }
                );
                dispatch(action('hello.success'));
            } catch (err) {
                dispatch(action('hello.failure', err));
            }
        }

        try {
            await store.dispatch(act());
        } catch {
            expect(store.dispatchedActions).toBeHaveBeenDispatched('hello');
            expect(store.dispatchedActions).not.toBeHaveBeenDispatched('hello.success');
            expect(store.dispatchedActions).toBeHaveBeenDispatched('hello.failure');
            expect(
                getDispatchedAction(store.dispatchedActions, 'hello.failure').error
            ).toBeInstanceOf(ServiceResponseError);
        }
    });

    it('retry count shared between 500s and connection errors', async () => {
        jest.setTimeout(8000);

        test_setMockRequestFactory(
            createMockMakeRequestFactory({
                responses: [
                    {
                        shouldFailConnection: true,
                    },
                    {
                        status: 500,
                    },
                    {
                        shouldFailConnection: true,
                    },
                    {
                        ok: true,
                    },
                ],
            })
        );

        async function act() {
            const dispatch = useDispatch();
            const action = useActionCreator();
            const webClient = useWebClient();

            try {
                dispatch(action('hello'));
                await webClient.request(
                    '/',
                    { method: 'GET' },
                    {
                        requiresAuthentication: false,
                        skipParsingResponse: true,
                        retryCount: 2,
                    }
                );
                dispatch(action('hello.success'));
            } catch (err) {
                dispatch(action('hello.failure', err));
            }
        }

        try {
            await store.dispatch(act());
        } catch {
            expect(store.dispatchedActions).toBeHaveBeenDispatched('hello');
            expect(store.dispatchedActions).not.toBeHaveBeenDispatched('hello.success');
            expect(store.dispatchedActions).toBeHaveBeenDispatched('hello.failure');
            expect(
                getDispatchedAction(store.dispatchedActions, 'hello.failure').error
            ).toBeInstanceOf(ServiceConnectionError);
        }
    });

    it('retries based on provided func', async () => {
        jest.setTimeout(8000);

        test_setMockRequestFactory(
            createMockMakeRequestFactory({
                responses: [
                    {
                        shouldFailConnection: true,
                    },
                    {
                        ok: false,
                    },
                    {
                        ok: true,
                    },
                ],
            })
        );

        async function act() {
            const dispatch = useDispatch();
            const action = useActionCreator();
            const webClient = useWebClient();

            try {
                dispatch(action('hello'));
                await webClient.request(
                    '/',
                    { method: 'GET' },
                    {
                        requiresAuthentication: false,
                        skipParsingResponse: true,
                        shouldRetry() {
                            return true;
                        },
                    }
                );
                dispatch(action('hello.success'));
            } catch (err) {
                dispatch(action('hello.failure', err));
            }
        }

        await store.dispatch(act());

        expect(store.dispatchedActions).not.toHaveFailed();
    });

    it('retries based on provided func', async () => {
        jest.setTimeout(8000);

        test_setMockRequestFactory(
            createMockMakeRequestFactory({
                responses: [
                    {
                        shouldFailConnection: true,
                    },
                    {
                        ok: false,
                    },
                    {
                        ok: true,
                    },
                ],
            })
        );

        async function act() {
            const dispatch = useDispatch();
            const action = useActionCreator();
            const webClient = useWebClient();

            try {
                dispatch(action('hello'));
                await webClient.request(
                    '/',
                    { method: 'GET' },
                    {
                        requiresAuthentication: false,
                        skipParsingResponse: true,
                        async shouldRetry() {
                            await wait(10);
                            return true;
                        },
                    }
                );
                dispatch(action('hello.success'));
            } catch (err) {
                dispatch(action('hello.failure', err));
            }
        }

        await store.dispatch(act());

        expect(store.dispatchedActions).not.toHaveFailed();
    });
});
