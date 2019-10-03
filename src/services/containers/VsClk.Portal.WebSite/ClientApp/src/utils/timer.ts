import { Disposable, CancellationToken, CancellationTokenSource } from 'vscode-jsonrpc';

function internalTimeout(callback: () => void, timeout: number): Disposable {
    const timeoutHandle = setTimeout(callback, timeout);

    return {
        dispose() {
            clearTimeout(timeoutHandle);
        },
    };
}

export function createCancellationToken(
    timeout: number
): { token: CancellationToken; dispose(): void; cancel(): void } {
    const tokenSource = new CancellationTokenSource();

    const disposable = internalTimeout(() => {
        tokenSource.cancel();
    }, timeout);

    return {
        token: tokenSource.token,
        cancel() {
            tokenSource.dispose();
            disposable.dispose();
        },
        dispose() {
            tokenSource.dispose();
            disposable.dispose();
        },
    };
}
