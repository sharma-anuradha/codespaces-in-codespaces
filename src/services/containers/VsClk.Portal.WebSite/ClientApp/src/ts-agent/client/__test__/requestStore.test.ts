import { CancellationError } from '../../../utils/signal';
import { CancellationTokenSource } from 'vscode-jsonrpc';
import { RequestStore } from '../RequestStore';

describe('RequestStore', () => {
    let store: RequestStore<number>;

    beforeEach(() => {
        jest.useFakeTimers();

        store = new RequestStore<number>({ defaultTimeout: 100 * 10000 });
    });

    afterEach(() => {
        jest.runAllTimers();

        store.dispose();
    });

    it('gives you a promise to response', async () => {
        store.setResponse('1', 1);
        const response = store.getResponse('1');

        expect(await response).toBe(1);
    });

    it('gives you a promise to response that can be set later', async () => {
        const response = store.getResponse('1');
        store.setResponse('1', 1);

        expect(await response).toBe(1);
    });

    it('gives you a promise which gets cancelled', async () => {
        const tokenSource = new CancellationTokenSource();
        const response = store.getResponse('1', tokenSource.token);

        tokenSource.cancel();

        expect(response).rejects.toBeInstanceOf(CancellationError);
    });

    it('gives you a promise which gets cancelled after 10ms', async () => {
        const tokenSource = new CancellationTokenSource();
        const response = store.getResponse('1', tokenSource.token);

        expect(response).rejects.toBeInstanceOf(CancellationError);
    });

    it('uses default timeout when no cancellation token provided', () => {
        const store = new RequestStore({ defaultTimeout: 5 });

        const response = store.getResponse('1');

        expect(response).rejects.toBeInstanceOf(CancellationError);

        store.dispose();
    });

    it('returns the same request when asked twice', async () => {
        const store = new RequestStore({ defaultTimeout: 5 });

        const res1 = store.getResponse('1');
        const res2 = store.getResponse('1');

        expect(res1).toBe(res2);

        // We want to compare promises.
        store.setResponse('1', 2);
    });

    it('returns the same value when asked twice', async () => {
        const store = new RequestStore({ defaultTimeout: 5 });

        const res1 = store.getResponse('1');
        const res2 = store.getResponse('1');
        store.setResponse('1', 2);

        expect(await res1).toBe(await res2);
    });

    it('times out in default timeout', async () => {
        const store = new RequestStore();
        const res = store.getResponse('1');

        expect(res).rejects.toBeTruthy();

        jest.runTimersToTime(30 * 1000);

        return res.catch(() => {});
    });
});
