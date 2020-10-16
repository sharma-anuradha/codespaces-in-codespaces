import ResourceNames, { ComponentNames } from "../Values/ResourceNames";
import { Environment } from "./Environments";

export class ComponentsDeployment {
  globalPrefix: string;
  serviceTreeId: string;
  components: Component[] = [];
  nameTemplate = /[^{\\}]+(?=})/g;

  constructor(components: any, subscriptions: Subscription[]) {
    this.globalPrefix = components.globalPrefix;
    this.serviceTreeId = components.serviceTreeId;
    this.parseComponentsJson(components.components, subscriptions);
  }

  parseComponentsJson(components: any, subscriptions: Subscription[]): void {
    for (const compName in components) {
      const comp = new Component();
      const jsonComp = components[compName];
      comp.prefix = jsonComp.prefix;
      comp.serviceTreeId = jsonComp.serviceTreeId || this.serviceTreeId;
      comp.displayName = jsonComp.displayName;
      comp.dependsOn = jsonComp.dependsOn;
      comp.environmentNames = jsonComp.environments;
      comp.subscriptions = subscriptions.filter(s => s.component === comp.prefix);
      this.components.push(comp);
    }
  }
}

export class Component {
  prefix: string;
  serviceTreeId: string;
  displayName: string;
  dependsOn: string[];
  subscriptions: Subscription[] = [];
  environmentNames : string[];
  environments: Environment[] = [];
  outputNames: ComponentNames;

  generateNamesJson(prefix: string): ComponentNames {
    this.outputNames = ResourceNames.sortKeys(new ComponentNames(this.displayName, prefix, this.prefix, this.serviceTreeId));
    return this.outputNames;
  }

  getSubscription(env: string, plane:string): Subscription[] {
    env = env.toLowerCase()
    plane = plane.toLowerCase()
    return this.subscriptions.filter(s => s.environment === env && s.plane === plane && !s.serviceType)
  }

  getDataSubscriptions(env: string, region: string): Subscription[] {
    env = env.toLowerCase();
    region = region.toLowerCase();
    const dataSubs = this.subscriptions.filter(s =>
      s.environment === env &&
      s.plane === 'data' &&
      s.region === region &&
      s.generation !== 'v1-legacy');
    return dataSubs
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
  subscriptionId: string;
  subscriptionName: string;
  serviceType: string;
}
