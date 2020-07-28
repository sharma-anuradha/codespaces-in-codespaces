import { AssertionError } from "assert";
import ResourceNames, { ComponentNames } from "./ResourceNames";

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
        comp.subscriptions.push(this.parseSubscriptionJson(subJson));
      }
      this.components.push(comp);
    }
  }

  parseSubscriptionJson(subscription): Subscription {
    const sub = new Subscription();
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
  outputNames: ComponentNames;

  generateNamesJson(prefix: string): ComponentNames {
    const baseComponentName = ResourceNames.makeResourceName(prefix, this.prefix);
    const baseName = baseComponentName;

    return this.outputNames = {
      prefix: prefix,
      baseName: baseName,
      component: this.prefix
    };
  }
}

export class Subscription {
  nameTemplate: string;
  plane: string;
  env: string;
  count: number;
  provisioned: Provisioned[] = [];
  outputName: SubscriptionName;

  generateNamesJson(prefix: string, env: string, compPrefix: string) : SubscriptionName {
    const baseEnvName = ResourceNames.makeResourceName(prefix, compPrefix, env);
    const basePlaneName = ResourceNames.makeResourceName(prefix, compPrefix, env, this.plane);
    const baseName = basePlaneName;

    return this.outputName = {
      baseName: baseName,
      baseEnvName: baseEnvName,
      basePlaneName: basePlaneName,
      component: compPrefix,
      env: env,
      plane: this.plane,
    };
  }
}

export class SubscriptionName {
  baseName: string;
  baseEnvName: string;
  basePlaneName: string;
  component: string;
  env: string;
  plane: string;
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
