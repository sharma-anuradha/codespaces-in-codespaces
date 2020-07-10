export const KNOWN_PARTNERS = ['github', 'salesforce', 'azureportal'];

export const validatePartnerInfoPostmessage = (info: any) => {
    const token =
        'cascadeToken' in info
            ? info.cascadeToken // new partner info format
            : info.token; // old postMessage partner info format

    if (!token) {
        throw new Error('No cascadeToken token set.');
    }

    const codespaceId =
        'cascadeToken' in info
            ? info.codespaceId // new partner info format
            : info.environmentId; // old postMessage handshake format

    if (
        !codespaceId &&
        !(info as any).workspaceId // return by GH postMessage handshake
    ) {
        throw new Error('No environmentId set.');
    }
};
