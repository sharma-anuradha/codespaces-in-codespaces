export async function setAuthCookie(token: string) {
    const response = await fetch('/authenticate-port-forwarder', {
        method: 'POST',
        body: JSON.stringify({
            accessToken: token
        }),
        headers: {
            'Content-Type': 'application/json',
        },
        credentials: 'same-origin',
    });
    console.log(response);
    return response;
}