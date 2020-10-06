## CSP rules

Repository of the domains and directives allowed in CSP rules for the Codespaces Workbench

### script-src

    - `'unsafe-eval'` - enabled since VSCode uses `onigasm` for document parsing on a separate thread with WASM modules. The WASM considered being `eval` by the browsers. Can be removed when the browsers support improved. more info: https://github.com/WebAssembly/content-security-policy/blob/master/proposals/CSP.md.
    - `'nonce-*'` - for a smooth transition from partners, we need to change the splash screen styles and favicon as soon as possible, hence we run an inline script on `workbench.html`. The `nonce` attribute is added to the inline script to enable its execution.

### style-src

    - `'unsafe-inline'` - used to enable the DOM-binding frameworks (React and Fabric) that use inline styles.

### img-src

    - `data:` - VSCode uses the data URI in the Workbench CSS to create a background of some editor UI elements (like squiggles).
    -  https://*.gallerycdn.vsassets.io - VSCode hosts extensions logo images on the CDN.
    - {PartnerFaviconsEndpoint} - partners can send their `favicons` in the [`CodespaceInfo` payload](https://www.npmjs.com/package/vs-codespaces-authorization) and browser should be able to render those on the workbench page. For now, this endpoint is mapped to the partner domain but we need to find a way to make it more dynamic without sacrificing security.

### connect-src

    - `{PartnerPortForwardingEndpoint}` - Port Forwarding management API endpoint. This is used for instance for "warming up" PF endpoints to improve user first-time connection experience.
    - `{PartnerProxyApiEndpoint}` - Some partners (for instance GitHub), need to have control over some of the Codespace lifecycle methods, hence we proxy those API calls thru the partner endpoint to delegate the control. One such need is to block the startup(`/start` call) of a Codespace in case a user exceeds quota or should be blocked for other reasons.
    - `{WildcardApiEndpoint}` - the endpoint for the VSCS API calls. It has the wildcard subdomain since, if a codespace was created in a different region, the client calls to API can be redirected to other regions (different subdomains) and we cannot know the regional URL in advance.
    - `{LiveShareEndpoint}` - LiveShare API endpoint to get connection information.
    - `{RelayEndpoints}` - LiveShare Relay endpoints to connect to.

### font-src

    - `self` - VSCode Workbench uses some custom fonts.

### frame-src

    - `https://*.vscode-webview-test.com` - The Extensions webviews hosted by VSC team on the domain. Should be changed to the new domain when work on the new webviews endpoint is complete (https://github.com/microsoft/vssaas-planning/issues/390).
