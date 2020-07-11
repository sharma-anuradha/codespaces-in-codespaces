## VS Codespaces Authorization library

The `npm` module that makes life easier when dealing with the VS Codespaces platform authorization calls. Includes:

- Typescript types for the authorization info payload.
- Javascript utilities for performing cross-domain top-level HTTP POST request to the platform.
- JSON schemas to validate the JSON authorization payloads.

### Installation

```shell
npm install vs-codespaces-authorization
```

### Using Javascript util

```typescript
import { authorizePlatform } from 'vs-codespaces-authorization';

await authorizePlatform(
    'https://online.visualstudio.com/platform-authentication',
    {
      "partnerName": "github",
      "managementPortalUrl": "https://github.com/codespaces",
      "codespaceToken": "<codespace JWT>",
      "credentials": [],
      "codespaceId": "<codespace guid>",
      "vscodeSettings": {}
    }
);

```

<details>
  <summary>Example for authorization payload data</summary>

```json
{
    "partnerName": "github",
    "managementPortalUrl": "https://github.com/codespaces",
    "codespaceToken": "<codespaces JWT>",
    "credentials": [{
        "expiration": 10000000000000,
        "token": "<github token>",
        "host": "github.com",
        "path": "/"
    }],
    "codespaceId": "<codespace guid>",
    "featureFlags": {
        "example-pfs-name": "not real feature flag",
        "example-enable-pfs": true,
        "example-multithreading": 5
    },
    "vscodeSettings": {
        "vscodeChannel": "insider",
        "loadingScreenThemeColor": "dark",
        "defaultSettings": {
           "workbench.colorTheme": "GitHub Light",
           "workbench.startupEditor": "welcomePageInEmptyWorkbench"
        },
        "defaultExtensions": [{
            "id": "GitHub.vscode-pull-request-github"
        }, {
            "id": "ms-vsliveshare.vsliveshare"
        }],
        "defaultAuthSessions": [{
            "type": "github",
            "id": "github-session-github-pr",
            "accessToken": "<github token>",
            "scopes": ["read:user", "user:email", "repo"]
        }]
    }
}
```
</details>

### Using Schemas

Online playground: https://www.jsonschemavalidator.net/s/kGYLyowr

The lastest JSON schema defined in `src/schemas/` and published at https://aka.ms/vscs-platform-json-schema.
(The extended schema https://aka.ms/vscs-platform-json-schema-extended can be used internally).

### License

See [LICENSE.md](./LICENSE.md)
