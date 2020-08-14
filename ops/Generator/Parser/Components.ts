import ResourceNames, { ComponentNames } from "./ResourceNames";
import { Environment } from "./Environments";

export class ComponentsDeployment {
  globalPrefix: string;
  components: Component[] = [];
  nameTemplate = /[^{\\}]+(?=})/g;

  constructor(components: any) {
    this.globalPrefix = components.globalPrefix;
    this.parseComponentsJson(components.components);
  }

  parseComponentsJson(components) {
    for (const compName in components) {
      const comp = new Component();
      const jsonComp = components[compName];
      comp.prefix = jsonComp.prefix;
      comp.displayName = jsonComp.displayName;
      comp.dependsOn = jsonComp.dependsOn;
      for (const subJson of jsonComp.subscriptions) {
        const subscriptions = this.parseSubscriptionsJson(subJson, comp.prefix);
        comp.subscriptions = comp.subscriptions.concat(subscriptions);
      }
      this.components.push(comp);
    }
  }

  parseSubscriptionsJson(subJson: any, component: string): Subscription[] {
    const templateNames = subJson.nameTemplate.match(
      this.nameTemplate
    ) as string[];
    const testSplit = subJson.nameTemplate.split("-");
    const splitKeyValue = templateNames.map((n) => {
      return { key: n, value: testSplit.indexOf(`{${n}}`) };
    }) as any[];

    const subs = [];

    for (const proName in subJson.provisioned) {
      const proJson = subJson.provisioned[proName];
      const sub = new Subscription();
      subs.push(sub);
      sub.component = component;
      sub.name = proName;
      sub.subscriptionId = proJson.subscriptionId;
      const splitName = proName.split("-");
      for (const item of splitKeyValue) {
        sub[item.key] = splitName[item.value];
      }
    }

    return subs;
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
    return this.subscriptions.filter(s => s.env === env && s.plane === plane && !s.type)
  }
}

export class Subscription {
  name: string;
  subscriptionId: string;
  prefix: string;
  component: string;
  env: string;
  plane: string;
  type: string;
  geo: string;
}
