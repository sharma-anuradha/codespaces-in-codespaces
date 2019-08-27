import { MockStore, createMockStore } from '../../../utils/testUtils';
import { useActionCreator } from '../useActionCreator';

describe('useActionCreator', () => {
    let store: MockStore;

    beforeEach(() => {
        store = createMockStore();
    });

    it('simpleAction', () => {
        function act() {
            const action = useActionCreator();

            return action('simpleAction');
        }

        const sut = store.dispatch(act());

        expect(sut.type).toBe('simpleAction');
    });

    it('action with payload', () => {
        function act(payload: { success: boolean }) {
            const action = useActionCreator();
            return action('actionWithPayload', payload);
        }

        const sut = store.dispatch(act({ success: true }));
        expect(sut.type).toBe('actionWithPayload');
        expect(sut.payload.success).toBe(true);
    });

    it('action with error', () => {
        function act(error: Error) {
            const action = useActionCreator();
            return action('actionWithPayload', error);
        }

        const sut = store.dispatch(act(new Error('Sample')));
        expect(sut.type).toBe('actionWithPayload');
        expect(sut.failed).toBe(true);
    });
});
