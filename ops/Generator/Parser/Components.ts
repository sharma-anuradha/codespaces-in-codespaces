import ResourceNames, { ComponentNames } from "./ResourceNames";
import { Environment } from "./Environments";

export class ComponentsDeployment {
  globalPrefix: string;
  components: Component[] = [];
  nameTemplate = /[^{\\}]+(?=})/g;

  constructor(components: any, subscriptions: Subscription[]) {
    this.globalPrefix = components.globalPrefix;
    this.parseComponentsJson(components.components, subscriptions);
  }

  parseComponentsJson(components: any, subscriptions: Subscription[]): void {
    for (const compName in components) {
      const comp = new Component();
      const jsonComp = components[compName];
      comp.prefix = jsonComp.prefix;
      comp.displayName = jsonComp.displayName;
      comp.dependsOn = jsonComp.dependsOn;
      comp.subscriptions = subscriptions.filter(s => s.component === comp.prefix);
      this.components.push(comp);
    }
  }
}

export class Component {
  prefix: string;
  displayName: string;
  dependsOn: string[];
  subscriptions: Subscription[] = [];
  environments: Environment[] = [];
  outputNames: ComponentNames;

  generateNamesJson(prefix: string): ComponentNames {
    this.outputNames = ResourceNames.sortKeys(new ComponentNames(prefix, this.prefix));
    return this.outputNames;
  }

  getSubscription(env: string, plane:string): Subscription[] {
    env = env.toLowerCase()
    plane = plane.toLowerCase()
    return this.subscriptions.filter(s => s.environment === env && s.plane === plane && !s.serviceType)
  }
}

export class Subscription {
  ame: boolean;
  component: string;
  environment: string;
  generation: string;
  ordinal?: number;
  plane: string;
  region: string;
  subscriptionID: string;
  subscriptionName: string;
  serviceType: string;
}
