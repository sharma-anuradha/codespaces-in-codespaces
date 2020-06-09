## VS Codespaces Authorization library

The `npm` module that makes life easier when dealing with the VS Codespaces platform authorization calls. Includes:

- Typescript types for the authorization info payload.
- Javascript utilities for performing cross-domain top-level HTTP POST request to the platform.
- JSON schemas to validate the JSON authorization payloads.

### Installation

Yarn:
```shell
yarn add vs-codespaces-authorization
```

Npm:
```shell
npm install vs-codespaces-authorization
```

### Using Javascript util

```typescript
import { authorizePlatform } from 'vs-codespaces-authorization';

await authorizePlatform(
    'https://online.visualstudio.com/platform-authentication',
    {
        // platform authorization payload data (see below)
    }
);

```

<details>
  <summary>Example for authorization payload data</summary>
  
```json
{
  "partnerName": "github",
  "managementPortalUrl": "https://github.com/codespaces",
  "codespaceToken": "<codespace JWT>",
  "credentials": [
    {
      "expiration": 10000000000000,
      "token": "<github token>",
      "host": "github.com",
      "path": "/"
    }
  ],
  "codespaceId": "<codespace guid>",
  "vscodeSettings": {
    "vscodeChannel": "insider",
    "defaultSettings": {
      "workbench.colorTheme": "GitHub Light",
      "workbench.startupEditor": "welcomePageInEmptyWorkbench"
    },
    "defaultExtensions": [
      {
        "id": "GitHub.vscode-pull-request-github",
        "kind": "workspace"
      },
      {
        "id": "ms-vsliveshare.vsliveshare"
      }
    ],
    "defaultAuthSessions": [
      {
        "type": "github",
        "id": "a0446d79-9ec8-4373-ba94-df7bc46a9acf",
        "accessToken": "<github token>",
        "scopes": [
          "read:user",
          "user:email",
          "repo"
        ]
      }
    ]
  }
}
```
</details>

### Using Schemas

The lastest JSON schema defined in `src/schemas/` and published at https://aka.ms/vscs-platform-json-schema.
(The extended schema https://aka.ms/vscs-platform-json-schema-extended can be used internally).

### License

See [LICENSE.md](./LICENSE.md)
