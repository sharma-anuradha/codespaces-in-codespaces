/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// @ts-check

'use strict';

const path = require('path');
const webpack = require('webpack');

const rootFolder = path.resolve(__dirname, '../');

const isProduction = process.env.NODE_ENV === 'production';

const paths = {
    tsConfig: path.join(rootFolder, 'tsconfig.json'),
};

/**@type {import('webpack').Configuration}*/
const config = {
    stats: 'errors-only',
    mode: isProduction ? 'production' : 'development',
    target: 'web',
    entry: './src/index.ts', // the entry point of this extension, 📖 -> https://webpack.js.org/configuration/entry-context/
    output: {
        // the bundle is stored in the 'dist' folder (check package.json), 📖 -> https://webpack.js.org/configuration/output/
        path: path.resolve(__dirname, '../dist'),
        filename: 'index.js',
        libraryTarget: "umd",
        devtoolModuleFilenameTemplate: '../[resource-path]',
    },
    devtool: 'source-map',
    externals: {},
    resolve: {
        // support reading TypeScript and JavaScript files, 📖 -> https://github.com/TypeStrong/ts-loader
        extensions: ['.ts', '.js', '.json'],
    },
    module: {
        rules: [
            {
                test: /\.ts$/,
                exclude: /node_modules/,
                use: [
                    {
                        loader: 'ts-loader',
                        options: {
                            context: rootFolder,
                            configFile: paths.tsConfig,
                            projectReferences: true,
                        },
                    },
                ],
            },
        ],
    },
    plugins: [
        new webpack.SourceMapDevToolPlugin({
            test: /\.ts$/,
            noSources: false,
            module: true,
            columns: true,
        }),
    ],
};

module.exports = config;
