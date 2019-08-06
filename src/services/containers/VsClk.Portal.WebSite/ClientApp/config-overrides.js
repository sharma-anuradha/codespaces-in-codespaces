const path = require('path');


module.exports = {
  webpack: function(config, env) {
    
    config.resolve.alias = {
      ...config.resolve.alias,
      net: path.join(__dirname, './src/ts-agent/mocks/net'),
      ['node-rsa']: path.join(__dirname, './src/ts-agent/mocks/net')
    };

    return config;
  },
  devServer: function(configFunction) {
    return function(proxy, allowedHost) {
      const config = configFunction(proxy, allowedHost);

      config.proxy = [
        {
          context: '/api/**',
          target: 'http://online.visualstudio.com',
          logLevel: 'silent',
          secure: false,
          changeOrigin: true,
          ws: true,
          xfwd: true
        },
        {
          context: '/vsls-api/**',
          pathRewrite: (url) => {
            return url.replace('/vsls-api', '');
          },
          target: 'https://prod.liveshare.vsengsaas.visualstudio.com',
          logLevel: 'silent',
          secure: false,
          changeOrigin: true,
          ws: true,
          xfwd: true
        }
      ];

      return config;
    };
  }
};