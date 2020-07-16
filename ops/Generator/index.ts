import {
  readFileSync,
  existsSync,
  writeFileSync,
  mkdirSync,
  readdirSync,
  copyFileSync,
  statSync,
} from "fs";
import { EnvironmentsDeployment } from "./Parser/Environments";
import { ComponentsDeployment, Component } from "./Parser/Components";
import * as path from "path";

class Match {
  key: string;
  index: number;
  isStatic: boolean;
}

class StaticMatch {
  env: string;
  plane: string;
  instance: string;
  geo: string;
  region: string;
}

class Main {
  inputDir: string;
  outputDir: string;
  envDep: EnvironmentsDeployment;
  compDep: ComponentsDeployment;
  components: Component[];
  staticItemsDetect = /[^[\\}]+(?=])/g;

  constructor(inputDir: string, outputDir: string) {
    /*
        Verify and/or create the input and output directories.
        If they don't exist or we can't make them, throw.
      */
    this.inputDir = path.normalize(inputDir);
    this.outputDir = path.normalize(outputDir);
    this.verifyInputOutputDirs(inputDir, outputDir);
    const compJson = JSON.parse(
      readFileSync(path.join(this.inputDir, "components.json"), "utf8")
    );
    const envJson = JSON.parse(
      readFileSync(path.join(this.inputDir, "environments.json"), "utf8")
    );

    this.compDep = new ComponentsDeployment(compJson);
    this.envDep = new EnvironmentsDeployment(envJson);

    const componentsDir = this.getDirectories(this.inputDir);
    this.components = this.compDep.components.filter((n) =>
      componentsDir.includes(n.displayName)
    );
  }

  GenerateTemplates() {
    const files = this.getAllFiles(this.inputDir);
    for (const file of files) {
      this.tempFileCopy(file);
    }
  }

  GenerateNames() {
    for (const comp of this.components) {
      this.generateJson(
        this.outputDir,
        `${comp.prefix}.names.json`,
        comp.generateNamesJson()
      );
      for (const env of this.envDep.environments) {
        this.generateJson(
          this.outputDir,
          `${comp.prefix}.${env.name}.names.json`,
          env.generateNamesJson(this.compDep.globalPrefix, comp.prefix)
        );
        for (const sub of comp.subscriptions) {
          this.generateJson(
            this.outputDir,
            `${comp.prefix}.${env.name}-${sub.plane}.names.json`,
            sub.generateNamesJson(
              this.compDep.globalPrefix,
              sub.env ?? env.name,
              comp.prefix
            )
          );

          for (const instance of env.instances) {
            this.generateJson(
              this.outputDir,
              `${comp.prefix}.${env.name}-${sub.plane}-${instance.name}.names.json`,
              instance.generateNamesJson(
                this.compDep.globalPrefix,
                sub.env ?? env.name,
                sub.plane,
                instance.stamps.flatMap(n => n.dataLocations),
                comp.prefix
              )
            );

            for (const stamp of instance.stamps) {
              for (const dl of stamp.dataLocations) {
                this.generateJson(
                  this.outputDir,
                  `${comp.prefix}.${env.name}-${sub.plane}-${instance.name}-${dl.geography.name}-${dl.region.name}.names.json`,
                  dl.generateNamesJson(
                    this.compDep.globalPrefix,
                    sub.env ?? env.name,
                    sub.plane,
                    instance.name,
                    comp.prefix
                  )
                );
              }
            }
          }
        }
      }
    }
  }

  containsMatch(matches: Match[], filters: string[]): boolean {
    return matches.filter(n => filters.includes(n.key) ).length > 0
  }

  // Used to test going over `Template` files and generating replacements
  // As this gets fleshed out more, it will be replaced. 
  tempFileCopy(filePath: string) {
    const file = filePath.split("\\").pop();
    if (!file.includes(".Template.")) {
      return;
    }
    const outputDir = path.join(this.outputDir, filePath.replace(this.inputDir, '').replace(file, ''));
    this.createDirectory(outputDir);
    const compComponentsName = file.split(".")[0];
    const compComponentsTemplate = file.split(".")[1];
    const comp = this.components.find(
      (n) => n.displayName === compComponentsName
    );
    if (comp == null) {
      return;
    }
    /*
      Based on the use of {} and [], we can determine which fields need to be
      auto generated and which are "static". 

      The filename template is {Env}-{Plane}-{Instance}-{Geo}-{Region}.
      
      If we have { } brackets, these are auto generated values
      based on the component and environment json.

      Static fields are based on their index value within that template and use [].
      If we have {Env}-[ctl]-{Instance}-{Geo}-{Region},
      Index 1 will be 'ctl', which is the Plane. Values will pivot based on this value.

      So in this example, 
      - For every Enviromnent (Dev, PPE, Prod)...
      - Where each subscription in the `ctl` plane...
      - In every instance and geo...
      - Create/copy a template for each region...
      
      If we chop off the geo and region ({Env}-[ctl]-{Instance}),
      - For every Enviromnent (Dev, PPE, Prod)...
      - Where each subscription in the `ctl` plane...
      - Join the instance regions...
      - and output one file with an array of each region included inside it. 
    */
    const match = this.generateMatches(compComponentsTemplate.split("-"));
    const staticMatch = this.generateStaticMatch(match.filter((n) => n.isStatic));

    // If we have a static plane, filter the subscriptions to just those with those planes. Otherwise use all.
    const subscriptions = staticMatch.plane != null ? comp.subscriptions.filter(n => n.plane === staticMatch.plane) : comp.subscriptions;
    const environments = staticMatch.env != null ? this.envDep.environments.filter(n => n.name == staticMatch.env) : this.envDep.environments;

    if (subscriptions.length <= 0) {
      throw(`Couldn't find any subscriptions for this component: File - ${file}, Plane - ${staticMatch.plane}`);
    }

    const useInstance = !this.containsMatch(match, ["{Region}", "{Geo}"]) && staticMatch.geo == null && staticMatch.region == null;
    const useSubscriptions = !this.containsMatch(match, ["{Instance}"]) && staticMatch.instance == null;
    const useEnvironments = !this.containsMatch(match, ["{Plane}"]) && staticMatch.plane == null;

    /*
      TODO: This will be _very much_ refactored but the rough idea is in place.
      The `use_` fields are for values that are dropped from the template filename
      `staticMatch` is used for values that are specifically inserted in the template with [].
      We filter items based on the static values, if they exist. Else we use everything within that item.
      If there is no static item and no auto generated value in the template name, then base it
      on that current item and break out.
    */

    for (const env of environments) {
      if (useEnvironments) {
        copyFileSync(
          filePath,
          path.join(
            outputDir,
            `${comp.prefix}.${env.name}.${file.split(".").slice(2).join(".")}`
          )
        );
      } else {
        for (const sub of subscriptions) {
          if (useSubscriptions) {
            copyFileSync(
              filePath,
              path.join(
                outputDir,
                `${comp.prefix}.${env.name}-${sub.plane}.${file.split(".").slice(2).join(".")}`
              )
            );
          } else {
            const instances = staticMatch.instance != null ? env.instances.filter(n => n.name == staticMatch.instance) : env.instances;
            if (instances.length <= 0) {
              throw(`Couldn't find any instances for this component: File - ${file}, Instance - ${staticMatch.instance}`);
            }
            for (const instance of instances) {
              // FIXME: Copying existing file to output directory, using temp names.
              if (useInstance) {
                copyFileSync(
                  filePath,
                  path.join(
                    outputDir,
                    `${comp.prefix}.${env.name}-${sub.plane}-${
                      instance.name
                    }.${file.split(".").slice(2).join(".")}`
                  )
                );
              } else {
                let stamps = staticMatch.geo != null ? instance.stamps.filter(n => n.location.geography.name == staticMatch.geo) : instance.stamps;
                if (stamps.length <= 0) {
                  throw(`Couldn't find any Geo stamps for this component: File - ${file}, Stamp - ${staticMatch.geo}`);
                }
                stamps = staticMatch.region != null ? stamps.filter(n => n.location.region.name == staticMatch.region) : stamps;
                if (stamps.length <= 0) {
                  throw(`Couldn't find any Region stamps for this component: File - ${file}, Stamp - ${staticMatch.region}`);
                }
                for (const stamp of stamps) {
                  copyFileSync(
                    filePath,
                    path.join(
                      outputDir,
                      `${comp.prefix}.${env.name}-${sub.plane}-${
                        instance.name
                      }-${stamp.name}.${file.split(".").slice(2).join(".")}`
                    )
                  );
                }
              }
            }
          }
        }
      }
    }
  }

  generateStaticMatch(matches: Match[]): StaticMatch {
    const staticMatch = new StaticMatch();
    for (const match of matches) {
      const value = match.key.match(this.staticItemsDetect)[0];
      switch (match.index) {
        case 0:
          staticMatch.env = value;
          break;
        case 1:
          staticMatch.plane = value;
          break;
        case 2:
          staticMatch.instance = value;
          break;
        case 3:
          staticMatch.geo = value;
          break;
        case 4:
          staticMatch.region = value;
          break;
      }
    }
    return staticMatch;
  }

  generateMatches(compComponentsTemplate: string[]): Match[] {
    return compComponentsTemplate.map((n) => {
      return {
        key: n,
        isStatic: this.staticItemsDetect.test(n),
        index: compComponentsTemplate.indexOf(`${n}`),
      };
    }) as Match[];
  }

  createDirectory(dir: string) {
    if (!existsSync(dir)) {
      mkdirSync(dir, { recursive: true });
    }
  }

  generateJson(basePath: string, name: string, obj) {
    this.createDirectory(basePath);
    writeFileSync(path.join(basePath, name), JSON.stringify(obj, null, 2));
  }

  getDirectories(inputDir: string, relative = true): string[] {
    const dirs = readdirSync(inputDir).filter((file) =>
      statSync(path.join(inputDir, file)).isDirectory()
    );
    if (relative) {
      return dirs;
    }
    return dirs.map((n) => path.join(inputDir, n));
  }

  getFiles(inputDir: string): string[] {
    return readdirSync(inputDir).filter(
      (file) => !statSync(path.join(inputDir, file)).isDirectory()
    );
  }

  getAllFiles(inputDir: string, fileList = []): string[] {
    const folders: string[] = this.getDirectories(inputDir);
    const files: string[] = this.getFiles(inputDir);
    fileList.push(...files.map((n) => path.join(inputDir, n)));
    folders.forEach((folder) => {
      this.getAllFiles(path.join(inputDir, folder), fileList);
    });
    return fileList;
  }

  verifyInputOutputDirs(inputDir: string, outputDir: string) {
    if (inputDir == null || outputDir == null) {
      throw("ts-node-script index.ts [inputDir] [outputDir]");
    }

    if (!existsSync(inputDir)) {
      throw(`inputDir (${inputDir}) doesn't exist.`);
    }

    if (!existsSync(outputDir)) {
      this.createDirectory(outputDir);
    }
  }
}

const main = new Main(process.argv[2], process.argv[3]);
main.GenerateNames();
main.GenerateTemplates();
console.log(`Done, check ${process.argv[3]}`);
