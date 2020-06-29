# Codespaces Service DevOps

All things devops.

## Service Tree References

[Visual Studio Codespaces](https://servicetree.msftcloudes.com/main.html#/ServiceModel/Home/8fa58105-2fc7-4ffb-8d9e-5654c301864b) Service Tree.

## Environment Configuration

Rollout environments and geographies are defined in [environments.json](./Components/environments.json).

Individual service component configurations and aszure subscriptions are defined in [components.json](./Components/components.json).

## Component Definitions

We use Ev2 to buildout and deploy each of the service components.

- [Core (shared) Component](./Components/Core/README.md)
- [Communication Component (SignalR)](./Components/Communications/ServiceGroupRoot/README.md)
- [Collaboration Component (Live Share)](./Components/Collaboration/README.md)
- [Codespaces Component](./Components/Codespaces/README.md)
- [Port Forwarding Component](./Components/PortForwarding/README.md)
- [Web Editor Component (Portal)](./Components/WebEditor/README.md)
