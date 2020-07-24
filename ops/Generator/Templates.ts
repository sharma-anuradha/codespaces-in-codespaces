import { ComponentsDeployment } from "./Parser/Components";
import FileHandler from "./Helpers/FileHandler";
import { EnvironmentsDeployment } from "./Parser/Environments";
import * as path from "path";
import { writeFileSync } from "fs";
import { Console } from "console";

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

class NameMatch {
  fileName: string;
  outputName: string;
  names: any;
}

export default class Templates {
  inputDir: string;
  outputDir: string;
  envDep: EnvironmentsDeployment;
  compDep: ComponentsDeployment;
  staticItemsDetect = /[^[\\}]+(?=])/g;
  staticCommentHeader = "Auto-Generated From Template";
  staticCommentFooter =
    "Do not edit this generated file. Edit the source file and rerun the generator instead.";

  constructor(
    inputDir: string,
    outputDir: string,
    envDep: EnvironmentsDeployment,
    compDep: ComponentsDeployment
  ) {
    this.inputDir = path.normalize(inputDir);
    this.outputDir = path.normalize(outputDir);
    this.envDep = envDep;
    this.compDep = compDep;
  }

  public Generate(): void {
    const templateFiles = this.getTemplateList(this.inputDir);

    // TODO: Message if no templates found?
    for (const templatePath of templateFiles) {
      const fileName = templatePath.split("\\").pop();
      const filetype = fileName.split(".").pop();
      const orgBuffer = FileHandler.GetFile(templatePath);
      const outputDir = templatePath
        .replace(this.inputDir, this.outputDir)
        .replace(fileName, "");
      FileHandler.CreateDirectory(outputDir);
      const names = this.getNames(fileName);
      for (const name of names) {
        let buffer = orgBuffer;
        const commentHeader = [
          this.staticCommentHeader,
          `${templatePath}`,
          this.staticCommentFooter,
        ];
        switch (filetype) {
          case "jsonc":
            buffer = this.textHeader(buffer, commentHeader);
            buffer = this.textReplacement(buffer, name);
            break;
          default:
            buffer = this.textReplacement(buffer, name);
            break;
        }
        writeFileSync(path.join(outputDir, name.outputName), buffer);
      }
    }
  }

  private getNames(fileName: string): NameMatch[] {
    const compComponentsName = fileName.split(".")[0];
    const compComponentsTemplate = fileName.split(".")[1];
    const comp = this.compDep.components.find(
      (n) => n.displayName === compComponentsName
    );
    if (comp == null) {
      // TODO: Component missing, tell user?
      return [];
    }

    const match = this.generateMatches(compComponentsTemplate.split("-"));
    const staticMatch = this.generateStaticMatch(
      match.filter((n) => n.isStatic)
    );

    // If we have a static plane, filter the subscriptions to just those with those planes. Otherwise use all.
    const subscriptions =
      staticMatch.plane != null
        ? comp.subscriptions.filter((n) => n.plane === staticMatch.plane)
        : comp.subscriptions;
    const environments =
      staticMatch.env != null
        ? this.envDep.environments.filter((n) => n.name == staticMatch.env)
        : this.envDep.environments;

    if (subscriptions.length <= 0) {
      throw `Couldn't find any subscriptions for this component: File - ${fileName}, Plane - ${staticMatch.plane}`;
    }

    const useInstance =
      !this.containsMatch(match, ["{Region}", "{Geo}"]) &&
      staticMatch.geo == null &&
      staticMatch.region == null;
    const useSubscriptions =
      !this.containsMatch(match, ["{Instance}"]) &&
      staticMatch.instance == null;
    const useEnvironments =
      !this.containsMatch(match, ["{Plane}"]) && staticMatch.plane == null;
    const names = [];

    for (const env of environments) {
      if (useEnvironments) {
        // environment
        names.push({
          fileName: fileName,
          outputName: `${comp.prefix}.${env.name}.${fileName
            .split(".")
            .slice(2)
            .join(".")}`,
          names: env.outputNames,
        });
      } else {
        for (const sub of subscriptions) {
          if (useSubscriptions) {
            names.push({
              fileName: fileName,
              outputName: `${comp.prefix}.${env.name}-${
                sub.plane
              }.${fileName.split(".").slice(2).join(".")}`,
              names: sub.outputName,
            });
          } else {
            const instances =
              staticMatch.instance != null
                ? env.instances.filter((n) => n.name == staticMatch.instance)
                : env.instances;
            if (instances.length <= 0) {
              throw `Couldn't find any instances for this component: File - ${fileName}, Instance - ${staticMatch.instance}`;
            }
            for (const instance of instances) {
              // FIXME: Copying existing file to output directory, using temp names.
              if (useInstance) {
                names.push({
                  fileName: fileName,
                  outputName: `${comp.prefix}.${env.name}-${sub.plane}-${
                    instance.name
                  }.${fileName.split(".").slice(2).join(".")}`,
                  names: instance.outputNames,
                });
              } else {
                let stamps =
                  staticMatch.geo != null
                    ? instance.stamps.filter(
                        (n) => n.location.geography.name == staticMatch.geo
                      )
                    : instance.stamps;
                if (stamps.length <= 0) {
                  throw `Couldn't find any Geo stamps for this component: File - ${fileName}, Stamp - ${staticMatch.geo}`;
                }
                stamps =
                  staticMatch.region != null
                    ? stamps.filter(
                        (n) => n.location.region.name == staticMatch.region
                      )
                    : stamps;
                if (stamps.length <= 0) {
                  throw `Couldn't find any Region stamps for this component: File - ${fileName}, Stamp - ${staticMatch.region}`;
                }
                for (const stamp of stamps) {
                  names.push({
                    fileName: fileName,
                    outputName: `${comp.prefix}.${env.name}-${sub.plane}-${
                      instance.name
                    }-${stamp.name}.${fileName.split(".").slice(2).join(".")}`,
                    names: stamp.location.outputNames,
                  });
                }
              }
            }
          }
        }
      }
    }

    return names;
  }

  private containsMatch(matches: Match[], filters: string[]): boolean {
    return matches.filter((n) => filters.includes(n.key)).length > 0;
  }

  private textHeader(buffer: Buffer, injectStrings: string[] = []): Buffer {
    const text = buffer.toString("utf8");
    // Add single comment to strings, add new lines.
    // If nothing in the array, return the same text.
    const generatedText =
      injectStrings.length > 0
        ? `${injectStrings.map((n) => `// ${n}`).join("\r\n")}\r\n\r\n${text}`
        : text;
    return Buffer.from(generatedText, "utf8");
  }

  private textReplacement(buffer: Buffer, namesObj: NameMatch): Buffer {
    let text = buffer.toString("utf8");
    const names = namesObj.names;
    const nameReplaceMatches = /[^{{{\\}]+(?=}}})/g.exec(text);

    if (nameReplaceMatches) {
      for (const match of nameReplaceMatches) {
        if (!Object.keys(names).includes(match)) {
          // TODO: Throw better error, line number? Other values?
          throw `Property ${match} not found in names object. ${namesObj}`;
        }
      }
  
      for (const name in names) {
        const regex = new RegExp("{{{" + name + "}}}", "g");
        text = text.replace(regex, namesObj.names[name]);
      }
    } else {
      throw (`No template values found in ${namesObj.fileName}`);
    }

    return Buffer.from(text, "utf8");
  }

  private getTemplateList(inputDir: string): string[] {
    const files: string[] = [];
    FileHandler.GetAllFiles(inputDir, files);
    return files.filter((n) => n.includes(".Template."));
  }

  private generateMatches(compComponentsTemplate: string[]): Match[] {
    return compComponentsTemplate.map((n) => {
      return {
        key: n,
        isStatic: this.staticItemsDetect.test(n),
        index: compComponentsTemplate.indexOf(`${n}`),
      };
    }) as Match[];
  }

  private generateStaticMatch(matches: Match[]): StaticMatch {
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
}
