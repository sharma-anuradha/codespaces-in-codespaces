const fs = require('fs');
const path = require('path');

var exec = require('child_process').exec;

function puts(error, stdout, stderr) {
    console.log(stdout);
}

const WebpackShellPlugin = require('webpack-shell-plugin');

WebpackShellPlugin.prototype.apply = function(compiler) {
    const options = this.options;
  
    compiler.plugin('compilation', (compilation) => {
      if(options.onBuildStart.length){
          console.log("Executing pre-build scripts");
          options.onBuildStart.forEach(script => exec(script, puts));
      }
    });
  
    compiler.plugin('emit', (compilation, callback) => {
      if (options.onBuildEnd.length){
          console.log("Executing post-build scripts");
          options.onBuildEnd.forEach(script => exec(script, puts));
      }

      callback();
    });
  };
  

let includePaths = [
    fs.realpathSync(__dirname + '/src'),
    fs.realpathSync(__dirname + '/node_modules/vso-ts-agent'),
];

module.exports = {
    entry: './src/app.ts',
    devtool: 'source-map',
    mode: 'development',
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                use: 'ts-loader',
                include: includePaths,
                exclude: /node_modules/,
            },
            {
                test: /\.css$/i,
                use: ['style-loader', 'css-loader'],
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
        filename: 'vso-workbench.js',
        path: path.resolve(__dirname, './public/dist/'),
    },
    plugins: [
        new WebpackShellPlugin({
            onBuildEnd:['yarn copy-build-files']
        })
    ]
};
