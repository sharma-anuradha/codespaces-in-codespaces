const path = require('path');

module.exports = {
    entry: './src/service-worker/service-worker.ts',
    devtool: 'source-map',
    mode: 'development',
    module: {
        rules: [
            {
                test: /\.(tsx?)$/,
                use: 'ts-loader',
                include: path.resolve(__dirname, '../'),
                exclude: /node_modules/,
            },
        ],
    },
    resolve: {
        extensions: [ '.ts', '.tsx', '.js', '.json' ],
        alias: {
            // SSH lib mocks
            net: path.resolve(__dirname, './mocks/net'),
            'node-rsa': path.resolve(__dirname, './mocks/net'),
        }
    },
    output: {
        filename: 'service-worker.js',
        path: path.resolve(__dirname, '../public/'),
    },
};
