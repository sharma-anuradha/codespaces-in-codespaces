import { Component } from "./Parser/Components";
import FileHandler from "./Helpers/FileHandler";
import { Environment, Plane, Instance, Stamp } from "./Parser/Environments";
import * as path from "path";
import { writeFileSync } from "fs";

class Match {
  key: string;
  index: number;
  isStatic: boolean;
}

class StaticMatch {
  env: string;
  plane: string;
  instance: string;
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
  components: Component[];
  staticItemsDetect = /[^[\\}]+(?=])/g;
  staticCommentHeader = "Auto-Generated From Template";
  staticCommentFooter =
    "Do not edit this generated file. Edit the source file and rerun the generator instead.";

  constructor(
    inputDir: string,
    outputDir: string,
    components: Component[]
  ) {
    this.inputDir = path.normalize(inputDir);
    this.outputDir = path.normalize(outputDir);
    this.components = components;
  }

  public Generate(): void {
    const templateFiles = this.getTemplateList(this.inputDir);

    for (const templatePath of templateFiles) {
      const fileName = templatePath.split("\\").pop();
      const filetype = fileName.split(".").pop();
      const orgBuffer = FileHandler.GetFile(templatePath);
      const outputDir = templatePath
        .replace(this.inputDir, this.outputDir)
        .replace(fileName, "");

      const names = this.getNames(fileName);
      if (!names?.length) {
        const name = {
          fileName: fileName,
          outputName: fileName,
          names: null
        };
        names.push(name);
        // console.log(`info: ${name.fileName} -> ${name.outputName} -> *`);
      }

      for (const name of names) {
        let buffer = orgBuffer;
        const commentHeader = [
          this.staticCommentHeader,
          `${templatePath} (${name.names?.baseName ?? '*'})`,
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
        FileHandler.CreateDirectory(outputDir);
        writeFileSync(path.join(outputDir, name.outputName), buffer);
      }
    }
  }

  private getNames(fileName: string): NameMatch[] {
    const compComponentsName = fileName.split(".")[0]?.toLowerCase();
    if (!compComponentsName) {
      // console.log(`warning: filename contains no component name: ${fileName}`);
      return [];
    }

    const comp = this.components.find(
      (n) => n.prefix?.toLowerCase() === compComponentsName
    );
    if (!comp) {
      // console.log(`warning: non-matching component name: ${fileName}`);
      return [];
    }

    const compComponentsTemplate = fileName.split(".")[1];
    if (!compComponentsTemplate) {
      // console.log(`warning: filename contains no component template: ${fileName}`);
      return [];
    }

    const match = this.generateMatches(compComponentsTemplate.split("-"));
    if (!match?.length) {
      return [];
    }
    const staticMatch = this.generateStaticMatch(
      match.filter((n) => n.isStatic)
    );

    const hasEnvironments = staticMatch.env || this.containsMatch(match, ["{Env}"]);
    const envFilter = function (e: Environment) {
      return hasEnvironments && (staticMatch.env == null || e.name == staticMatch.env);
    }
    const hasPlanes = staticMatch.plane || this.containsMatch(match, ["{Plane}"]);
    const planeFilter = function (p: Plane) {
      return hasPlanes && (staticMatch.plane == null || p.name == staticMatch.plane);
    }
    const hasInstances = staticMatch.instance || this.containsMatch(match, ["{Instance}"]);
    const instanceFilter = function (i: Instance) {
      return hasInstances && (staticMatch.instance == null || i.name == staticMatch.instance);
    }
    const hasRegions = (staticMatch.region) || this.containsMatch(match, ["{Region}"]);
    const staticRegionName = staticMatch.region ? staticMatch.region : null;
    const stampFilter = function (s: Stamp) {
      return hasRegions && (staticRegionName == null || s.name == staticRegionName);
    }

    if (!hasEnvironments && !hasPlanes && !hasInstances && !hasRegions) {
      // no replacement pattern match;
      console.log(`warning: the file pattern ${compComponentsTemplate} is not valid: ${fileName}`)
      return [];
    }

    const names: NameMatch[] = [];

    const environments: Environment[] = comp.environments.filter(envFilter);
    for (const env of environments) {
      const planes = env.planes.filter(planeFilter);
      if (!planes?.length) {
        // {env}
        const staticEnv = new RegExp(`\\[${env.name}\\]`, "gi");
        const outputName = fileName.replace(/{Env}/gi, env.name).replace(staticEnv, env.name);
        names.push({
          fileName: fileName,
          outputName: outputName,
          names: env.outputNames,
        });
      }
      else {
        for (const plane of planes) {
          const instances = plane.instances.filter(instanceFilter);
          if (!instances?.length) {
            // {env}-{plane}
            const staticEnv = new RegExp(`\\[${env.name}\\]`, "gi");
            const staticPlane = new RegExp(`\\[${plane.name}\\]`, "gi");
            const outputName = fileName
              .replace(/{Env}/gi, env.name).replace(staticEnv, env.name)
              .replace(/{Plane}/gi, plane.name).replace(staticPlane, plane.name);
            names.push({
              fileName: fileName,
              outputName: outputName,
              names: plane.outputNames,
            });
          }
          else {
            for (const instance of instances) {
              const stamps = instance.stamps.filter(stampFilter);
              if (!stamps?.length) {
                // {env}-{plane}-{instance}
                const staticEnv = new RegExp(`\\[${env.name}\\]`, "gi");
                const staticPlane = new RegExp(`\\[${plane.name}\\]`, "gi");
                const staticInstance = new RegExp(`\\[${instance.name}\\]`, "gi");
                const outputName = fileName
                  .replace(/{Env}/gi, env.name).replace(staticEnv, env.name)
                  .replace(/{Plane}/gi, plane.name).replace(staticPlane, plane.name)
                  .replace(/{Instance}/gi, instance.name).replace(staticInstance, instance.name);
                names.push({
                  fileName: fileName,
                  outputName: outputName,
                  names: instance.outputNames,
                });
              }
              else {
                for (const stamp of stamps) {
                  // {env}-{plane}-{instance}-{stamp}
                  const staticEnv = new RegExp(`\\[${env.name}\\]`, "gi");
                  const staticPlane = new RegExp(`\\[${plane.name}\\]`, "gi");
                  const staticInstance = new RegExp(`\\[${instance.name}\\]`, "gi");
                  const staticRegion = new RegExp(`\\[${stamp.name}\\]`, "gi");
                  const outputName = fileName
                    .replace(/{Env}/gi, env.name).replace(staticEnv, env.name)
                    .replace(/{Plane}/gi, plane.name).replace(staticPlane, plane.name)
                    .replace(/{Instance}/gi, instance.name).replace(staticInstance, instance.name)
                    .replace(/{Region}/gi, stamp.name).replace(staticRegion, stamp.name)
                  names.push({
                    fileName: fileName,
                    outputName: outputName,
                    names: stamp.location.outputNames,
                  });
                }
              }
            }
          }
        }
      }
    }

    for (const name of names) {
      console.log(`info: ${name.fileName} -> ${name.outputName} (${name.names?.baseName})`);
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

    if (names) {
      const variablePattern = /[^{{{\\}]+(?=}}})/g;
      const matches = text.match(variablePattern);

      if (matches?.length > 0) {
        for (const match of matches) {
          if (!Object.keys(names).includes(match)) {
            throw `error: property '${match}' does not exist in names object '${names.baseName}': template file '${namesObj.fileName}'`;
          }
        }

        for (const name in names) {
          const regex = new RegExp("{{{" + name + "}}}", "g");
          const value = namesObj.names[name];
          if (!value) {
            throw `error: property '${name}' is undefined in names object '${names.baseName}': template file '${namesObj.fileName}'`;
          }
          const replaceValue = typeof value === 'string' ? value : JSON.stringify(value)?.replace(/\n/g, '\r\n');
          text = text.replace(regex, replaceValue);
        }
      } else {
        console.log(`warning: No template values found in ${namesObj.fileName}`);
        // throw (`No template values found in ${namesObj.fileName}`);
      }
    }

    return Buffer.from(text, "utf8");
  }

  private getTemplateList(inputDir: string): string[] {
    const files: string[] = [];
    FileHandler.GetAllFiles(inputDir, files);
    return files;
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
        // case 3:
        //   staticMatch.geo = value;
        //   break;
        case 3:
          staticMatch.region = value;
          break;
      }
    }
    return staticMatch;
  }
}
