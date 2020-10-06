export type ServiceWorkerFeatures = {
    useSharedConnection: boolean;
};

export type ServiceWorkerConfiguration = {
    passthroughUrls: string[];
    liveShareEndpoint: string;
    features: ServiceWorkerFeatures;
};
