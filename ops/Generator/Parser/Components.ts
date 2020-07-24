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
  outputName: ComponentName;

  generateNamesJson(): ComponentName {
    return this.outputName = {
      baseName: this.prefix,
      component: this.prefix,
      id: this.prefix,
      nameFile: `${this.prefix}.names.json`,
      outputPath: this.prefix,
    };
  }
}

export class ComponentName {
    baseName: string;
    component: string;
    id: string;
    nameFile: string;
    outputPath: string;
}

export class Subscription {
  nameTemplate: string;
  plane: string;
  env: string;
  count: number;
  provisioned: Provisioned[] = [];
  outputName: SubscriptionName;

  generateNamesJson(globalPrefix: string, env: string, compPrefix: string) {
    return this.outputName = {
      baseEnvName: `${globalPrefix}-${compPrefix}-${env}`,
      baseName: compPrefix,
      basePlaneName: `${globalPrefix}-${compPrefix}-${env}-${this.plane}`,
      component: compPrefix,
      env: env,
      id: `${globalPrefix}-${compPrefix}-${env}-${this.plane}`,
      nameFile: `${compPrefix}.${env}-${this.plane}-names.json`,
      outputPath: `${compPrefix}.${env}.${this.plane}`,
      plane: this.plane,
    };
  }
}

export class SubscriptionName {
  baseEnvName: string;
  baseName: string;
  basePlaneName: string;
  component: string;
  env: string;
  id: string;
  nameFile: string;
  outputPath: string;
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
