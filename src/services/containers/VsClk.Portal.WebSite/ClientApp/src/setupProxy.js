const proxy = require('http-proxy-middleware');

// For development purposes, do not import
module.exports = function (app) {
    
    // Environment registration service
    app.use(proxy('/api/registration', { target: 'http://localhost:6000/' }));

    // Portal service
    app.use(proxy('/api/authorize', { target: 'http://localhost:5000/' }));
    app.use(proxy('/signin', { target: 'http://localhost:5000/' }));
    app.use(proxy('/signout', { target: 'http://localhost:5000/' }));
    
    // app.use(proxy('/api/registration', { target: 'https://demo.dev.core.vsengsaas.visualstudio.com/' }));
    // app.use(proxy('/api/authorize', { target: 'https://demo.dev.core.vsengsaas.visualstudio.com/' }));
    // app.use(proxy('/signin', { target: 'https://demo.dev.core.vsengsaas.visualstudio.com/' }));
    // app.use(proxy('/signout', { target: 'https://demo.dev.core.vsengsaas.visualstudio.com/' }));
};