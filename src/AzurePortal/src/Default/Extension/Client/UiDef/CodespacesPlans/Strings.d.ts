declare module "UiDef/CodespacesPlans/Strings" {
    export = Strings;
    const Strings: {
        readonly Basic: {
            readonly CodespacePlanName: {
                readonly label: string;
            };
        };
        readonly Networking: {
            readonly AddVirtualNetwork: {
                readonly label: string;
                readonly no: string;
                readonly yes: string;
            };
            readonly subnetLabel: string;
            readonly subnetsLabel: string;
            readonly virtualNetworkLabel: string;
        };
        readonly networking: string;
    };
}