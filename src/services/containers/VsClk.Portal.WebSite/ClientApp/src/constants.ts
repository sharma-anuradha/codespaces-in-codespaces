
export const DEFAULT_EXTENSIONS = [
    'vscode.theme-defaults',
    'ms-vsliveshare.vsliveshare',
    'visualstudioexptteam.vscodeintellicode',
];

export const PACKAGE_JSON = require('../package.json');

export interface IProductJSON {
    commit: string;
    quality: 'stable' | 'insider';
}

export const WEB_EMBED_PRODUCT_JSON = require('./product-vscode-web.json') as IProductJSON;

const VSLS_PROD_API_URI = 'http://prod.liveshare.vsengsaas.visualstudio.com';
const VSLS_DEV_API_URI = '/vsls-api';

export const VSLS_API_URI = (process.env.NODE_ENV === 'production')
                            ? VSLS_PROD_API_URI
                            : VSLS_DEV_API_URI;