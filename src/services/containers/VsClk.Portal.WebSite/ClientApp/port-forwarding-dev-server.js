const httpProxy = require('http-proxy');
const fs = require('fs');
const https = require('https');
const path = require('path');

const pfxPath = path.resolve('../dev-cert.pfx');

if (!fs.existsSync(pfxPath)) {
    console.error('SSL Certificate not available.');
    console.log('To get one, run command:');
    return process.exit(1);
}

if (!process.env.VSO_PF_SESSION_ID) {
    console.error(
        'No session to connect to. Please set the VSO_PF_SESSION_ID environment variable.'
    );
}

const agentOptions = {
    host: 'online.dev.core.vsengsaas.visualstudio.com',
    port: '443',
    path: '/',
    rejectUnauthorized: false,
};

const agent = new https.Agent(agentOptions);

var proxy = httpProxy.createProxy({
    // agent,
    secure: false,
    changeOrigin: true,
    ws: true,
    xfwd: true,
});

const targetBaseUrl = 'https://online.dev.core.vsengsaas.visualstudio.com/portforward';

require('https')
    .createServer(
        {
            pfx: fs.readFileSync(pfxPath),
            agent,
        },
        (req, res) => {
            const sessionId = process.env.VSO_PF_SESSION_ID;

            if (req.url.startsWith('/vsls-api')) {
                req.url = req.url.substr('/vsls-api'.length);
                proxy.web(req, res, {
                    target: 'https://prod.liveshare.vsengsaas.visualstudio.com',
                });
                return;
            }

            let target;
            if (req.url.length === 1) {
                target = `${targetBaseUrl}?devSessionId=${encodeURIComponent(sessionId)}`;
            } else {
                target = `${targetBaseUrl}?path=${encodeURIComponent(
                    req.url.substr(1)
                )}&devSessionId=${encodeURIComponent(sessionId)}`;
            }

            console.log(`\n\n\n${req.url} => ${target}`);
            proxy.web(req, res, { target });
        }
    )
    .listen(4000, () => {
        console.log('Port forwarding proxy listening on port', 4000);
    });
