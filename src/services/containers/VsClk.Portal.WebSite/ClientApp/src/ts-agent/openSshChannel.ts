import * as ssh from '@vs/vs-ssh';

export async function openSshChannel(
    sshSession: ssh.SshSession,
    streamId: string,
): Promise<ssh.SshChannel> {
    const channel = await sshSession.openChannel();
    const channelRequestType = `stream-transport-${streamId}`;
    const result = await channel.request(new ssh.ChannelRequestMessage(channelRequestType));
    if (!result) {
        throw new Error(`Failed to create stream transport channel with streamId - ${streamId}`);
    }
    return channel;
}
