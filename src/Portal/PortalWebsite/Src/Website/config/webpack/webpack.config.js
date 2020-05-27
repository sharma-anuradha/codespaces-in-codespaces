// @ts-check

const fs = require('fs');
const path = require('path');
const webpack = require('webpack');

// const { BundleAnalyzerPlugin } = require('webpack-bundle-analyzer');
const CopyWebpackPlugin = require('copy-webpack-plugin');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const TsconfigPathsPlugin = require('tsconfig-paths-webpack-plugin');

const rootFolder = path.resolve(__dirname, '..', '..');

const paths = {
    tsConfig: path.join(rootFolder, 'tsconfig.json'),
    targetFolder: path.join(rootFolder, 'build'),
    amdconfig: path.join(rootFolder, 'public', 'amdconfig.js'),
    serviceWorker: path.join(
        rootFolder,
        'packages',
        'vso-service-worker',
        'src',
        'service-worker.ts'
    ),
    workbench: path.join(rootFolder, 'packages', 'vso-workbench', 'src', 'app.ts'),
    web: path.join(rootFolder, 'packages', 'website', 'src', 'index.tsx'),
    platformAuth: path.join(rootFolder, 'packages', 'vso-platform-auth', 'src', 'index.ts'),
    platformAuthentication: path.join(
        rootFolder,
        'packages',
        'vso-platform-authentication',
        'src',
        'index.ts'
    ),
    staticContent: path.join(rootFolder, 'public'),
    vscodeDownloads: path.join(rootFolder, 'vscode-downloads'),
    indexHtml: path.join(rootFolder, 'public', 'index.html'),
    workbenchHtml: path.join(rootFolder, 'public', 'workbench.html'),
    platformAuthHtml: path.join(rootFolder, 'public', 'platform-auth.html'),
    platformAuthenticationHtml: path.join(
        rootFolder,
        '../../../../services/containers/VsClk.Portal.WebSite/Views/PlatformAuth/PlatformAuthentication.cshtml'
    ),
    mocks: {
        net: path.join(rootFolder, 'packages', 'vso-ts-agent/mocks/net'),
        nodeRsa: path.join(rootFolder, 'packages', 'vso-ts-agent/mocks/net'),
    },
};

const production = process.env.NODE_ENV === 'production';

const publicPath = '/';

module.exports = [
    {
        mode: production ? 'production' : 'development',
        devtool: 'source-map',
        entry: {
            'amdconfig': paths.amdconfig,
            'platform-auth': paths.platformAuth,
            'platform-authentication': paths.platformAuthentication,
            'web': paths.web,
            'workbench': paths.workbench,
        },
        output: {
            path: paths.targetFolder,
            publicPath,
            filename: production ? 'static/js/[name].[contenthash:8].js' : 'static/js/[name].js',
            chunkFilename: production
                ? 'static/js/[name].[contenthash:8].chunk.js'
                : 'static/js/[name].chunk.js',
        },
        optimization: {
            runtimeChunk: {
                name: 'runtime',
            },
            splitChunks: {
                chunks: 'all',
            },
        },
        watchOptions: {
            ignored: [/packages\/[^\/](lib|commonjs)/, /node_modules\//, /vscode-downloads/],
        },
        resolve: {
            extensions: ['.ts', '.tsx', '.js', '.jsx', '.json'],
            // @ts-ignore
            plugins: [new TsconfigPathsPlugin({ configFile: paths.tsConfig })],
            modules: ['node_modules', 'vscode-downloads/workbench-page/web-standalone'],
            alias: {
                'net': paths.mocks.net,
                'node-rsa': paths.mocks.nodeRsa,
            },
        },
        module: {
            rules: [
                {
                    oneOf: [
                        {
                            test: [/\.bmp$/, /\.gif$/, /\.jpe?g$/, /\.png$/],
                            loader: require.resolve('url-loader'),
                            options: {
                                limit: 10000,
                                name: 'static/media/[name].[hash:8].[ext]',
                            },
                        },
                        {
                            test: /\.css$/,
                            use: [
                                'style-loader',
                                production && MiniCssExtractPlugin.loader,
                                'css-loader',
                            ].filter(Boolean),
                        },
                        {
                            test: /\.(jsx?|tsx?)$/,
                            exclude: /node_modules|public|web-standalone/,
                            use: {
                                loader: 'ts-loader',
                                options: {
                                    context: rootFolder,
                                    configFile: paths.tsConfig,
                                    projectReferences: true,
                                },
                            },
                        },
                        {
                            loader: require.resolve('file-loader'),
                            // Exclude `js` files to keep "css" loader working as it injects
                            // its runtime that would otherwise be processed through "file" loader.
                            // Also exclude `html` and `json` extensions so they get processed
                            // by webpacks internal loaders.
                            exclude: [/\.(js|ejs|mjs|jsx|ts|tsx)$/, /\.html$/, /\.json$/],
                            options: {
                                name: 'static/media/[name].[hash:8].[ext]',
                            },
                        },
                    ],
                },
            ],
        },
        plugins: [
            new webpack.DefinePlugin({
                'process.env.NODE_ENV': production ? '"production"' : '"development"',
                'process.env.PUBLIC_URL': `"${publicPath}"`,
                'process.env.PORTAL_VERSION': `"${Date.now()}"`,
                'process.env.VSCS_WORKBENCH_VERSION': `"${Date.now()}"`,
            }),
            new HtmlWebpackPlugin({
                inject: true,
                filename: 'index.html',
                template: paths.indexHtml,
                templateParameters: {
                    PUBLIC_URL: publicPath,
                },
                chunks: ['amdconfig', 'web'],
            }),
            new HtmlWebpackPlugin({
                inject: true,
                filename: 'workbench.html',
                template: paths.workbenchHtml,
                templateParameters: {
                    PUBLIC_URL: publicPath,
                },
                chunks: ['amdconfig', 'workbench'],
            }),
            new HtmlWebpackPlugin({
                inject: true,
                filename: 'platform-auth.html',
                template: paths.platformAuthHtml,
                templateParameters: {
                    PUBLIC_URL: publicPath,
                },
                chunks: ['platform-auth'],
            }),
            production &&
                new HtmlWebpackPlugin({
                    inject: true,
                    minify: false,
                    filename: paths.platformAuthenticationHtml,
                    templateContent: () => {
                        return fs.readFileSync(paths.platformAuthenticationHtml, 'utf8');
                    },
                    chunks: ['platform-authentication'],
                }),
            new MiniCssExtractPlugin({
                filename: 'static/css/[name].[contenthash:8].css',
                chunkFilename: 'static/css/[name].[contenthash:8].chunk.css',
            }),
            new CopyWebpackPlugin([
                path.join(paths.staticContent, 'favicon.ico'),
                path.join(paths.staticContent, 'ms-logo.svg'),
                path.join(paths.staticContent, 'site.css'),
                path.join(paths.staticContent, 'manifest.json'),
                path.join(paths.staticContent, 'spinner-dark.svg'),
            ]),
            production &&
                new CopyWebpackPlugin([
                    {
                        from: path.join(paths.vscodeDownloads, 'workbench-page'),
                        to: 'workbench-page',
                    },
                ]),
            // Moment.js is an extremely popular library that bundles large locale files
            // by default due to how Webpack interprets its code. This is a practical
            // solution that requires the user to opt into importing specific locales.
            // https://github.com/jmblog/how-to-optimize-momentjs-with-webpack
            // You can remove this if you don't use Moment.js:
            new webpack.IgnorePlugin(/^\.\/locale$/, /moment$/),
            // new BundleAnalyzerPlugin(),
        ].filter(Boolean),
        devServer: {
            host: '0.0.0.0',
            port: 3030,
            hot: false,
            compress: true,
            liveReload: false,
            inline: false,
            contentBase: [paths.staticContent, paths.vscodeDownloads],
            disableHostCheck: true,
            overlay: false,
            historyApiFallback: {
                disableDotRule: true,
            },
            progress: true,
            proxy: {
                '/api/v1/**': {
                    target: 'https://westus2-ci-online.dev.core.vsengsaas.visualstudio.com',
                    logLevel: 'debug',
                    secure: false,
                    changeOrigin: true,
                    ws: true,
                },
            },
        },
        stats: 'errors-only',
        performance: {
            /**
             * @param {string} filename
             */
            assetFilter(filename) {
                return !filename.includes('workbench-page/web-standalone');
            },
        },
    },
    {
        mode: production ? 'production' : 'development',
        devtool: 'source-map',
        target: 'webworker',
        entry: {
            'service-worker': paths.serviceWorker,
        },
        output: {
            path: paths.targetFolder,
            filename: production ? 'static/js/[name].[contenthash:8].js' : 'static/js/[name].js',
            chunkFilename: production
                ? 'static/js/[name].[contenthash:8].chunk.js'
                : 'static/js/[name].chunk.js',
        },
        resolve: {
            extensions: ['.ts', '.tsx', '.js', '.jsx', '.json'],
            // @ts-ignore
            plugins: [new TsconfigPathsPlugin({ configFile: paths.tsConfig })],
            modules: ['node_modules', 'vscode-downloads/workbench-page/web-standalone'],
            alias: {
                'net': paths.mocks.net,
                'node-rsa': paths.mocks.nodeRsa,
            },
        },
        module: {
            rules: [
                {
                    oneOf: [
                        {
                            test: /\.(jsx?|tsx?)$/,
                            exclude: /node_modules|public|web-standalone/,
                            use: {
                                loader: 'ts-loader',
                                options: {
                                    context: rootFolder,
                                    configFile: paths.tsConfig,
                                    projectReferences: true,
                                },
                            },
                        },
                        {
                            loader: require.resolve('file-loader'),
                            // Exclude `js` files to keep "css" loader working as it injects
                            // its runtime that would otherwise be processed through "file" loader.
                            // Also exclude `html` and `json` extensions so they get processed
                            // by webpacks internal loaders.
                            exclude: [/\.(js|ejs|mjs|jsx|ts|tsx)$/, /\.html$/, /\.json$/],
                            options: {
                                name: 'static/media/[name].[hash:8].[ext]',
                            },
                        },
                    ],
                },
            ],
        },
        plugins: [
            new webpack.DefinePlugin({
                'process.env.NODE_ENV': production ? '"production"' : '"development"',
                'process.env.PUBLIC_URL': `"${publicPath}"`,
                'process.env.PORTAL_VERSION': `"${Date.now()}"`,
            }),
            new HtmlWebpackPlugin({
                inject: false,
                filename: 'service-worker.js',
                templateContent: ({ htmlWebpackPlugin }) =>
                    `importScripts('${htmlWebpackPlugin.files.publicPath}${htmlWebpackPlugin.files.js}');`,
                chunks: ['service-worker'],
            }),
            // Moment.js is an extremely popular library that bundles large locale files
            // by default due to how Webpack interprets its code. This is a practical
            // solution that requires the user to opt into importing specific locales.
            // https://github.com/jmblog/how-to-optimize-momentjs-with-webpack
            // You can remove this if you don't use Moment.js:
            new webpack.IgnorePlugin(/^\.\/locale$/, /moment$/),
        ],
        stats: 'errors-only',
        performance: {
            /**
             * @param {string} filename
             */
            assetFilter(filename) {
                return !filename.includes('workbench-page/web-standalone');
            },
        },
    },
];
