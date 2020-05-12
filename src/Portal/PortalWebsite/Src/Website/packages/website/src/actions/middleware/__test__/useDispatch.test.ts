import { wait } from '../../../dependencies';
import { MockStore, createMockStore } from '../../../utils/testUtils';

import { action, useActionCreator } from '../useActionCreator';
import { useDispatch } from '../useDispatch';

describe('useDispatch', () => {
    let store: MockStore;

    beforeEach(() => {
        store = createMockStore();
    });

    describe('dispatch order', () => {
        it('dispatches single action', () => {
            store.dispatch(syncAction(1));

            expect(store.dispatchedActions).toBeHaveBeenDispatched('step.1');
        });

        it('sync, sync', () => {
            let step = 1;
            function act() {
                const dispatch = useDispatch();

                dispatch(syncAction(step++));
                dispatch(syncAction(step++));
            }

            store.dispatch(act());

            expect(store.dispatchedActions).toHaveBeenDispatchedInOrder('step.1', 'step.2');
        });

        it('sync, async', async () => {
            let step = 1;
            async function act() {
                const dispatch = useDispatch();

                dispatch(syncAction(step++));
                await dispatch(asyncAction(step++));
            }

            await store.dispatch(act());

            expect(store.dispatchedActions).toHaveBeenDispatchedInOrder('step.1', 'step.2');
        });

        it('async, sync', async () => {
            let step = 1;
            async function act() {
                const dispatch = useDispatch();

                await dispatch(asyncAction(step++));
                dispatch(syncAction(step++));
            }

            await store.dispatch(act());

            expect(store.dispatchedActions).toHaveBeenDispatchedInOrder('step.1', 'step.2');
        });

        it('async, async', async () => {
            let step = 1;
            async function act() {
                const dispatch = useDispatch();

                await dispatch(asyncAction(step++));
                await dispatch(asyncAction(step++));
            }

            await store.dispatch(act());

            expect(store.dispatchedActions).toHaveBeenDispatchedInOrder('step.1', 'step.2');
        });

        it('async, async, async', async () => {
            let step = 1;
            async function act() {
                const dispatch = useDispatch();

                await dispatch(asyncAction(step++));
                await dispatch(asyncAction(step++));
                await dispatch(asyncAction(step++));
            }

            await store.dispatch(act());

            expect(store.dispatchedActions).toHaveBeenDispatchedInOrder(
                'step.1',
                'step.2',
                'step.3'
            );
        });

        it('parallel(async, async), async', async () => {
            let step = 1;
            async function act() {
                const dispatch = useDispatch();

                await Promise.all([dispatch(asyncAction(step++)), dispatch(asyncAction(step++))]);
                await dispatch(asyncAction(step++));
            }

            await store.dispatch(act());

            expect(store.dispatchedActions).toHaveBeenDispatchedInOrder(
                'step.1',
                'step.2',
                'step.3'
            );
        });

        it('parallel(async, async) - with reverse order, async', async () => {
            async function act() {
                const dispatch = useDispatch();

                await Promise.all([dispatch(asyncAction(2)), dispatch(asyncAction(1))]);
                await dispatch(asyncAction(3));
            }

            await store.dispatch(act());

            expect(store.dispatchedActions).toHaveBeenDispatchedInOrder(
                'step.1',
                'step.2',
                'step.3'
            );
        });

        it('parallel(async, async), async - shortest', async () => {
            async function act() {
                const dispatch = useDispatch();

                await Promise.all([dispatch(asyncAction(3)), dispatch(asyncAction(2))]);
                await dispatch(asyncAction(1));
            }

            await store.dispatch(act());

            expect(store.dispatchedActions).toHaveBeenDispatchedInOrder(
                'step.2',
                'step.3',
                'step.1'
            );
        });

        it('sync, parallel(async, series(async, async)), sync', async () => {
            let step = 1;
            async function act() {
                const dispatch = useDispatch();

                syncAction(step++);

                await Promise.all([dispatch(asyncAction(step++)), dispatch(asyncAction(step++))]);
                dispatch(syncAction(step++));
            }

            await store.dispatch(act());

            expect(store.dispatchedActions).toHaveBeenDispatchedInOrder(
                'step.1',
                'step.2',
                'step.3',
                'step.4'
            );
        });
    });

    describe('error handling', () => {
        it('throws error when child action fails', async () => {
            async function act() {
                const dispatch = useDispatch();
                const action = useActionCreator();

                try {
                    await dispatch(failedAsyncAction());
                    dispatch(action('no.not.this.one'));
                } catch (err) {
                    dispatch(action('error.handler.without.fail'));
                }
            }

            await store.dispatch(act());

            expect(store.dispatchedActions).toHaveBeenDispatchedInOrder(
                'async.failed',
                'error.handler.without.fail'
            );
        });

        it('does not throw when parent action has error handling', async () => {
            async function act() {
                const dispatch = useDispatch();
                const action = useActionCreator();

                try {
                    await dispatch(failedAsyncAction());
                    dispatch(action('no.not.this.one'));
                } catch (err) {
                    dispatch(action('error.handler', new Error('this failed')));
                }
            }

            try {
                await store.dispatch(act());
            } catch {
                expect(store.dispatchedActions).toHaveBeenDispatchedInOrder(
                    'async.failed',
                    'error.handler'
                );
            }
        });

        it('throws an error only once', async () => {
            async function successfulAsyncAction() {
                const dispatch = useDispatch();

                await wait(0);

                dispatch(action('successfulAsyncAction.success'));
            }

            async function act() {
                const dispatch = useDispatch();
                const action = useActionCreator();

                dispatch(action('act'));

                try {
                    const succeeding = dispatch(successfulAsyncAction());
                    const failing = dispatch(failedAsyncComposedAction()).then(() =>
                        dispatch(action('act.update'))
                    );

                    await Promise.all([succeeding, failing]);

                    dispatch(action('act.success'));
                } catch (err) {
                    dispatch(action('act.failure', err));
                }
            }
            try {
                await store.dispatch(act());
            } catch {
                expect(store.dispatchedActions).toHaveBeenDispatchedInOrder(
                    'act',
                    'failedAsyncAction',
                    'successfulAsyncAction.success',
                    'failedAsyncAction.failed',
                    'failedAsyncAction.failed',
                    'act.failure'
                );
            }
        });

        it('handles reducer throwing error', () => {
            function act() {
                const dispatch = useDispatch();

                try {
                    dispatch(action('throw.reducer.failure'));
                } catch (err) {
                    dispatch(action('act.failure', err));
                }
            }

            try {
                store.dispatch(act());
            } catch {
                expect(store.dispatchedActions).toHaveBeenDispatchedInOrder('act.failure');
            }
        });

        // TODO: Fix creation of async context
        xit('handles longer chain on actions', async () => {
            async function act() {
                const dispatch = useDispatch();

                dispatch(syncAction(1));
                try {
                    const first = dispatch(asyncAction(2));
                    const second = dispatch(asyncAction(3));

                    await Promise.all([first, second]);

                    await dispatch(failedAsyncComposedAction());
                    dispatch(syncAction(4));
                } catch (err) {
                    dispatch(action('act.failure', err));
                }
            }

            await store.dispatch(act());

            expect(store.dispatchedActions).toHaveBeenDispatchedInOrder(
                'step.1',
                'step.2',
                'step.3',
                'failedAsyncAction',
                'failedAsyncAction.failed',
                'act.failure'
            );
        });

        it('handles async action with reducer throwing error', async () => {
            async function child() {
                const dispatch = useDispatch();

                await wait(0);
                dispatch(action('throw.reducer.failure'));
            }

            async function act() {
                const dispatch = useDispatch();

                try {
                    await dispatch(child());
                } catch (err) {
                    dispatch(action('act.failure', err));
                }
            }

            try {
                await store.dispatch(act());
            } catch {
                expect(store.dispatchedActions).toHaveBeenDispatchedInOrder('act.failure');
            }
        });
    });
});

async function failAsynchronously() {
    await wait(0);
    return Promise.reject(new Error('expected fail'));
}

function syncAction(num: number) {
    const dispatch = useDispatch();
    const action = useActionCreator();

    dispatch(action(`step.${num}`));
}

async function asyncAction(num: number) {
    const dispatch = useDispatch();
    const action = useActionCreator();

    await wait(10 * num);
    dispatch(action(`step.${num}`));
}

async function failedAsyncAction() {
    const dispatch = useDispatch();
    const action = useActionCreator();

    try {
        await failAsynchronously();
    } catch (err) {
        dispatch(action('async.failed', err));
    }
}

async function failedAsyncComposedAction() {
    const dispatch = useDispatch();

    try {
        dispatch(action('failedAsyncAction'));

        await wait(100);
        dispatch(action('failedAsyncAction.failed', new Error('failedAsyncAction')));
    } catch (err) {
        dispatch(action('failedAsyncAction.failed', err));
    }
}
