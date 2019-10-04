import { useWebClient } from '../actions/middleware/useWebClient';

export async function setAuthCookie(accessToken: string) {
    const webClient = useWebClient();

    await webClient.post(
        '/authenticate-port-forwarder',
        { accessToken },
        { requiresAuthentication: false, skipParsingResponse: true }
    );
}
