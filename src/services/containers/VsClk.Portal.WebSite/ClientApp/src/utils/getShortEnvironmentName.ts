export const getShortEnvironmentName = (env: string): 'dev' | 'dev-stg' | 'ppe' | 'prod' | 'unknown' => {
    const envUrl = new URL(env.trim());

    switch(envUrl.hostname) {
        case 'online.dev.core.vsengsaas.visualstudio.com': {
            return 'dev';
        }

        case 'online-ppe.core.vsengsaas.visualstudio.com': {
            return 'ppe';
        }

        case 'online-stg.dev.core.vsengsaas.visualstudio.com': {
            return 'dev-stg';
        }

        case 'online.visualstudio.com': {
            return 'prod';
        }

        default: {
            return 'unknown';
        }
    }
};

export const isDevEnvironment = (env: string): boolean => {
    const currentEnv = getShortEnvironmentName(env);
    return (currentEnv === 'dev') && !!window.localStorage.getItem('debugLocalExtension');
};