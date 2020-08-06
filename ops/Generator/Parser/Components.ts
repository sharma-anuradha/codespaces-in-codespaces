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
        comp.subscriptions.push(this.parseSubscriptionJson(subJson, comp.prefix));
      }
      this.components.push(comp);
    }
  }

  parseSubscriptionJson(subscription, component: string): Subscription {
    const sub = new Subscription();
    sub.component = component;
    sub.nameTemplate = subscription.nameTemplate;
    sub.plane = subscription.plane;
    sub.env = subscription.env;
    sub.count = subscription.count;
    const templateNames = subscription.nameTemplate.match(
      this.nameTemplate
    ) as string[];
    const testSplit = subscription.nameTemplate.split("-");
    const splitKeyValue = templateNames.map((n) => {
      return { key: n, value: testSplit.indexOf(`{${n}}`) };
    }) as any[];
    for (const proName in subscription.provisioned) {
      const pro = new Provisioned();
      const proJson = subscription.provisioned[proName];
      pro.name = proName;
      const splitName = pro.name.split("-");
      for (const item of splitKeyValue) {
        pro[item.key] = splitName[item.value];
      }
      pro.subscriptionId = proJson.subscriptionId;
      sub.provisioned.push(pro);
    }

    return sub;
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
    return this.subscriptions.filter(s => s.env === env && s.plane === plane)
  }
}

export class Subscription {
  nameTemplate: string;
  component: string;
  env: string;
  plane: string;
  count: number;
  provisioned: Provisioned[] = [];
}

export class Provisioned {
  name: string;
  globalPrefix: string;
  prefix: string;
  env: string;
  plane: string;
  geo: string;
  subscriptionId: string;
}
