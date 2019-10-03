export type ServiceWorkerFeatures = {
    useSharedConnection: boolean;
};

export type ServiceWorkerConfiguration = {
    liveShareEndpoint: string;
    features: ServiceWorkerFeatures;
};
