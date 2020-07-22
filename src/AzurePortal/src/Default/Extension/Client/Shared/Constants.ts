export const sdkResourceProvider = 'Microsoft.VSOnline';
export const rootResource = 'plans';

export module Links {
    export const visualStudioCodespaces =
        'https://visualstudio.microsoft.com/services/visual-studio-codespaces/';
    export const visualStudioCodespacesEnvironments =
        'https://online.visualstudio.com/environments';
    export const learnMoreConsistency = 'https://aka.ms/portalfx/designpatterns';
    export const learnMorePortalDocs = 'https://aka.ms/portalfx/browse';
    export const esentialsAdditionalRightLink1 = 'http://www.bing.com';
}

// menu group IDs must be unique, must not be localized, should not contain spaces and should be lowercase
// for the standard menus - use the constants defined in MsPortalFx.Assets naming convention is <name>GroupId, like MsPortalFx.Assets.SupportGroupId
export module ResourceMenuGroupIds {
    export const resourceSpecificGroup = 'myresourcespecific_group';
    export const dxviewsGroup = 'dxviews_group';
}

// menu IDs must be unique, must not be localized, should not contain spaces and should be lowercase
// for the standard menu items - use the constants defined in MsPortalFx.Assets naming convention is <name>ItemId, like MsPortalFx.Assets.PropertiesItemId
export module ResourceMenuBladeIds {
    export const overview = 'overview';
    export const codespacesItem = 'codespaces';
    export const dxViewItem = 'dxview_item';
}

export function uuid() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        var r = (Math.random() * 16) | 0,
            v = c == 'x' ? r : (r & 0x3) | 0x8;
        return v.toString(16);
    });
}

export const encryptedGitAccessTokenKey = 'encryptedGithubAuthToken';
