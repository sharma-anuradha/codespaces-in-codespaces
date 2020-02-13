import { Event, CancellationToken } from 'vscode-jsonrpc';

export class ChannelClosedEventArgs {
    public readonly exitStatus?: number;
    public readonly exitSignal?: string;
    public readonly errorMessage?: string;
    public readonly error?: Error;
}

export interface IDataChannel {
    readonly onClosed: Event<ChannelClosedEventArgs>;
    readonly onDataReceived: Event<Buffer>;

    send(data: Buffer, cancellation?: CancellationToken): Promise<void>;
}