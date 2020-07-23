import FxBase = MsPortalFx.Base;

export const codespacesBaseUri = MsPortalFx.getEnvironmentValue('codespacesEndpoint');
export const codespacesApiVersion = MsPortalFx.getEnvironmentValue('codespacesApiVersion');

export const githubClientId = MsPortalFx.getEnvironmentValue('gitHubAzurePortalClientId');

const codespacesUriBuilder = new FxBase.UriBuilder(codespacesBaseUri);
codespacesUriBuilder.setRelativePath(`api/${codespacesApiVersion}`);

function constructUri(uriBuilder: FxBase.UriBuilder, path?: string) {
    path = path || '';
    if (path.length > 0 && !path.startsWith('/')) {
        path = `/${path}`;
    }

    return uriBuilder.toString() + path;
}

export function getArmUri(id: string): string {
    const uriBuilder = new FxBase.UriBuilder(MsPortalFx.getEnvironmentValue('armEndpoint'));
    uriBuilder.setRelativePath(id);
    uriBuilder.query.setParameter('api-version', MsPortalFx.getEnvironmentValue('armApiVersion'));

    return uriBuilder.toString();
}

export function getCodespacesUri(path?: string): string {
    return constructUri(codespacesUriBuilder, path);
}
