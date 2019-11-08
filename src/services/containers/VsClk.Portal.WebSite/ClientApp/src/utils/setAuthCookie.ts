import { useWebClient } from '../actions/middleware/useWebClient';

export async function setAuthCookie(accessToken: string) {
    const webClient = useWebClient();

    await webClient.post(
        '/authenticate-port-forwarder',
        { accessToken },
        { requiresAuthentication: false, skipParsingResponse: true }
    );
}

export async function deleteAuthCookie() {
    const webClient = useWebClient();

    await webClient.post(
        '/logout-port-forwarder',
        {},
        { requiresAuthentication: false, skipParsingResponse: true }
    );
}
