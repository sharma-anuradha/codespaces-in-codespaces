import { getDispatchedAction, createMockStore, MockStore } from '../../../utils/testUtils';
import { wait } from '../../../dependencies';
import { useActionCreator, action } from '../useActionCreator';
import { useDispatch } from '../useDispatch';

describe('useActionContext', () => {
    let store: MockStore;

    beforeEach(() => {
        store = createMockStore();
    });

    it('adds correlation id to metadata', () => {
        function act() {
            const dispatch = useDispatch();
            const action = useActionCreator();

            dispatch(action('sample1'));
            dispatch(action('sample2'));
        }

        store.dispatch(act());

        const sample1 = getDispatchedAction(store.dispatchedActions, 'sample1');
        const sample2 = getDispatchedAction(store.dispatchedActions, 'sample2');

        expect(sample1.metadata.correlationId).toBeDefined();
        expect(sample1.metadata.correlationId).toBe(sample2.metadata.correlationId);
    });

    it('shares correlation id between parent and child actions', () => {
        function child() {
            const dispatch = useDispatch();
            const action = useActionCreator();

            dispatch(action('child'));
        }
        function act() {
            const dispatch = useDispatch();
            const action = useActionCreator();

            dispatch(action('act'));
            dispatch(child());
        }

        store.dispatch(act());

        const actionAct = getDispatchedAction(store.dispatchedActions, 'act');
        const actionChild = getDispatchedAction(store.dispatchedActions, 'child');

        expect(actionAct.metadata.correlationId).toBeDefined();
        expect(actionAct.metadata.correlationId).toBe(actionChild.metadata.correlationId);
    });

    it('creates new correlation id between dispatches', () => {
        function child(num: number) {
            const dispatch = useDispatch();
            const action = useActionCreator();

            dispatch(action(`child.${num}`));
        }
        function act(num: number) {
            const dispatch = useDispatch();
            const action = useActionCreator();

            dispatch(action(`act.${num}`));
            dispatch(child(num));
        }

        store.dispatch(act(1));
        store.dispatch(act(2));

        const actionAct1 = getDispatchedAction(store.dispatchedActions, 'act.1');
        const actionAct2 = getDispatchedAction(store.dispatchedActions, 'act.2');
        const actionChild1 = getDispatchedAction(store.dispatchedActions, 'child.1');
        const actionChild2 = getDispatchedAction(store.dispatchedActions, 'child.2');

        expect(actionAct1.metadata.correlationId).toBeDefined();
        expect(actionAct1.metadata.correlationId).toBe(actionChild1.metadata.correlationId);

        expect(actionAct2.metadata.correlationId).toBeDefined();
        expect(actionAct2.metadata.correlationId).toBe(actionChild2.metadata.correlationId);

        expect(actionAct1.metadata.correlationId).not.toBe(actionAct2.metadata.correlationId);
    });

    it('can dispatch async functions', async () => {
        async function act() {
            const dispatch = useDispatch();
            const action = useActionCreator();

            dispatch(action('sample1'));
            await wait(0);
            dispatch(action('sample2'));
        }

        const actExecuted = act();
        await store.dispatch(actExecuted);

        const sample1 = getDispatchedAction(store.dispatchedActions, 'sample1');
        const sample2 = getDispatchedAction(store.dispatchedActions, 'sample2');

        expect(sample1.metadata.correlationId).toBeDefined();
        expect(sample1.metadata.correlationId).toBe(sample2.metadata.correlationId);
    });

    it('async failure is handled properly', async () => {
        async function act() {
            const dispatch = useDispatch();
            const action = useActionCreator();

            dispatch(action('sample'));
            try {
                await wait(0);
                dispatch(action('sample.success'));
                throw new Error('Things went wrong. Very wrong.');
            } catch (err) {
                dispatch(action('sample.failure', err));
            }
        }

        try {
            await store.dispatch(act());
        } catch {
            expect(store.dispatchedActions).toHaveFailed();
        }
    });

    it('sync actions will get new context', () => {
        function act(num: number) {
            const dispatch = useDispatch();
            const action = useActionCreator();

            dispatch(action(`sample${num}`));
        }

        store.dispatch(act(1));
        store.dispatch(act(2));

        const sample1 = getDispatchedAction(store.dispatchedActions, 'sample1');
        const sample2 = getDispatchedAction(store.dispatchedActions, 'sample2');

        expect(sample1.metadata.correlationId).not.toBe(sample2.metadata.correlationId);
    });

    it('async actions will get new context', async () => {
        async function act(num: number) {
            const dispatch = useDispatch();
            const action = useActionCreator();

            dispatch(action(`sample${num}`));
            await wait(0);
            dispatch(action(`sample${num}.success`));
        }

        await store.dispatch(act(1));
        await store.dispatch(act(2));

        const sample1 = getDispatchedAction(store.dispatchedActions, 'sample1');
        const sample1Success = getDispatchedAction(store.dispatchedActions, 'sample1.success');
        const sample2 = getDispatchedAction(store.dispatchedActions, 'sample2');
        const sample2Success = getDispatchedAction(store.dispatchedActions, 'sample2.success');

        expect(sample1.metadata.correlationId).not.toBe(sample2.metadata.correlationId);

        expect(sample1.metadata.correlationId).toBe(sample1Success.metadata.correlationId);
        expect(sample2.metadata.correlationId).toBe(sample2Success.metadata.correlationId);
    });

    it('parallel async actions will get new context', async () => {
        async function act(num: number) {
            const dispatch = useDispatch();
            const action = useActionCreator();

            dispatch(action(`sample${num}`));
            await wait(0);
            dispatch(action(`sample${num}.success`));
        }
        await Promise.all([store.dispatch(act(1)), store.dispatch(act(2))]);

        const sample1 = getDispatchedAction(store.dispatchedActions, 'sample1');
        const sample1Success = getDispatchedAction(store.dispatchedActions, 'sample1.success');
        const sample2 = getDispatchedAction(store.dispatchedActions, 'sample2');
        const sample2Success = getDispatchedAction(store.dispatchedActions, 'sample2.success');

        expect(sample1.metadata.correlationId).not.toBe(sample2.metadata.correlationId);

        expect(sample1.metadata.correlationId).toBe(sample1Success.metadata.correlationId);
        expect(sample2.metadata.correlationId).toBe(sample2Success.metadata.correlationId);
    });

    it('verify context with actions created without contextActionCreator', async () => {
        const syncAction = () => action(`sample1`);
        async function act() {
            const dispatch = useDispatch();
            const action = useActionCreator();
            dispatch(syncAction());
            await wait(0);
            dispatch(action(`sample1.success`));
        }
        await store.dispatch(act());
        const sample1 = getDispatchedAction(store.dispatchedActions, 'sample1');
        const sample1Success = getDispatchedAction(store.dispatchedActions, 'sample1.success');
        expect(sample1.metadata.correlationId).toBe(sample1Success.metadata.correlationId);
    });

    it('verify context with multiple actions created without contextActionCreator', async () => {
        const syncAction = (num: number) => action(`sample.${num}`);
        const syncActionSuccess = (num: number) => action(`sample.${num}.success`);
        async function act(num: number) {
            const dispatch = useDispatch();
            dispatch(syncAction(num));
            await wait(0);
            dispatch(syncActionSuccess(num));
        }
        await store.dispatch(act(1));
        await store.dispatch(act(2));
        const sample1 = getDispatchedAction(store.dispatchedActions, 'sample.1');
        const sample1Success = getDispatchedAction(store.dispatchedActions, 'sample.1.success');
        const sample2 = getDispatchedAction(store.dispatchedActions, 'sample.2');
        const sample2Success = getDispatchedAction(store.dispatchedActions, 'sample.2.success');
        expect(sample1.metadata.correlationId).toBeDefined();
        expect(sample2.metadata.correlationId).toBeDefined();
        expect(sample1.metadata.correlationId).not.toBe(sample2.metadata.correlationId);
        expect(sample1.metadata.correlationId).toBe(sample1Success.metadata.correlationId);
        expect(sample2.metadata.correlationId).toBe(sample2Success.metadata.correlationId);
    });
});
