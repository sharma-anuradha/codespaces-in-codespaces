import * as ssh from '@vs/vs-ssh';
import * as vsls from './contracts/VSLS';
import { openSshChannel } from './openSshChannel';

/**
 * Opens SSH channel on stream.
 */
export class SshChannelOpenner {
    private localPortNumber: number | undefined;

    public constructor(
        public readonly sharedServer: vsls.SharedServer,
        private readonly sshSession: ssh.SshSession,
        private readonly streamManagerClient: vsls.StreamManagerService,
    ) {}

    /**
     * Open a new SSH channel that is remotely connected to the shared server.
     */
    public async openChannel(): Promise<ssh.SshChannel> {
        const streamId = await this.streamManagerClient.getStreamAsync(
            this.sharedServer.streamName,
            this.sharedServer.streamCondition,
        );

        return await openSshChannel(this.sshSession, streamId);
    }
}
