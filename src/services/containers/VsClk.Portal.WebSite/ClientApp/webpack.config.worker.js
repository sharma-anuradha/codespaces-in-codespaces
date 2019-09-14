// @ts-check

const path = require('path');
const ForkTsCheckerNotifierWebpackPlugin = require('fork-ts-checker-notifier-webpack-plugin');
const ForkTsCheckerWebpackPlugin = require('fork-ts-checker-webpack-plugin');

const isDevelopment = true;
const paths = {
    staticAssetsWorker: './src/service-worker/static-assets-worker.ts',
    tsconfig: 'tsconfig.worker.json',
    output: {
        staticAssetsWorker: 'static-assets-worker.js',
        dir: path.resolve(__dirname, 'public'),
    },
};

const webpackConfig = {
    context: process.cwd(),
    mode: isDevelopment ? 'development' : 'production',

    devtool: 'source-map',

    target: 'webworker',

    entry: paths.staticAssetsWorker,
    output: {
        path: paths.output.dir,
        filename: paths.output.staticAssetsWorker,
    },

    resolve: {
        extensions: ['.tsx', '.ts', '.js', '.json'],
        alias: {
            net: path.join(__dirname, './src/ts-agent/mocks/net'),
            'node-rsa': path.join(__dirname, './src/ts-agent/mocks/net'),
        },
    },

    plugins: [
        new ForkTsCheckerWebpackPlugin({
            tsconfig: paths.tsconfig,
            useTypescriptIncrementalApi: true,
        }),
        new ForkTsCheckerNotifierWebpackPlugin({
            title: 'TypeScript',
            excludeWarnings: false,
        }),
    ],

    module: {
        rules: [
            {
                test: /.tsx?$/,
                loader: 'ts-loader',
                options: { configFile: paths.tsconfig, transpileOnly: true },
            },
        ],
    },
};

module.exports = webpackConfig;
