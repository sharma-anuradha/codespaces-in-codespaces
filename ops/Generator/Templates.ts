import { Component } from "./Parser/Components";
import FileHandler from "./Helpers/FileHandler";
import { Environment, Plane, Instance, Stamp } from "./Parser/Environments";
import * as path from "path";
import { writeFileSync } from "fs";
import { cloneDeep } from "lodash";

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
  folderName: string;
  outputFolderName: string;
  names: any;
}

class TemplateFileInfo {
  readonly originalFilePath: string;
  readonly contentFilePath: string;
  readonly templateFileName: string;
  readonly fileName: string;
  readonly filetype: string;
  readonly isTemplatedContent: boolean;

  static getTemplateFile(originalFilePath: string, templatesDir: string): TemplateFileInfo {
    let fileName = path.basename(originalFilePath);
    const filetype = path.extname(originalFilePath);
    let contentFilePath = originalFilePath;
    let templateFileName = "";
    let isTemplatedContent = false;

    const fileNameParts = fileName.split('@');
    if (fileNameParts.length === 2) {
      fileName = fileNameParts[0] + filetype;
      templateFileName = fileNameParts[1];
      contentFilePath = path.join(templatesDir, templateFileName);
      isTemplatedContent = true;
    }

    return {
      originalFilePath: originalFilePath,
      contentFilePath: contentFilePath,
      fileName: fileName,
      filetype: filetype,
      isTemplatedContent: isTemplatedContent,
      templateFileName: templateFileName
    }
  }
}

export default class Templates {
  readonly inputDir: string;
  readonly templatesDir: string;
  readonly outputDir: string;
  readonly components: Component[];
  private readonly staticItemsDetect = /[^[\\}]+(?=])/; // don't use global flag, it makes subsequent tests stateful.
  private readonly staticCommentHeader = "Auto-Generated From Template";
  private readonly staticCommentFooter =
    "Do not edit this generated file. Edit the source file and rerun the generator instead.";

  constructor(
    inputDir: string,
    templatesDir: string,
    outputDir: string,
    components: Component[]
  ) {
    this.inputDir = path.normalize(inputDir);
    this.templatesDir = path.normalize(templatesDir);
    this.outputDir = path.normalize(outputDir);
    this.components = components;
  }

  public Generate(): void {
    // Components template structure must be
    // Level 1: root:       ./*
    // Level 2: component   ./{component}/*
    // Level 3: templates   ./{components}/{template}/**

    // Level 1
    const rootDir = path.resolve(this.inputDir);
    this.GenerateFolder(rootDir, false);

    // Level 2
    const componentFolders = FileHandler.GetDirectories(rootDir);
    for (const componentFolderName of componentFolders) {
      const componentFolderDir = path.join(rootDir, componentFolderName);
      this.GenerateFolder(componentFolderDir, false);

      // Level 3
      const componentTemplateFolders = FileHandler.GetDirectories(componentFolderDir);
      for (const componentTemplateFolderName of componentTemplateFolders) {
        const componentTemplateDir = path.join(componentFolderDir, componentTemplateFolderName);
        this.GenerateFolder(componentTemplateDir, true);
      }
    }
  }

  private GenerateFolder(folderPath: string, recurse: boolean): void {

    const folderName = folderPath.split(path.sep).pop();
    const names = recurse ? this.getNames(folderName) : [];
    if (!names?.length) {
      const name = {
        folderName: folderName,
        outputFolderName: folderName,
        names: null
      };
      names.push(name);
    }

    const templateFiles = this.getTemplateList(folderPath, recurse);
    for (const templatePath of templateFiles) {
      const templateFileInfo = TemplateFileInfo.getTemplateFile(templatePath, this.templatesDir);
      const fileName = templateFileInfo.fileName;
      const filetype = templateFileInfo.filetype;
      const contentFileBuffer = FileHandler.GetFile(templateFileInfo.contentFilePath);
      let additionalNames = null;

      if (templateFileInfo.isTemplatedContent) {
        const orgFileText = FileHandler.GetFileText(templateFileInfo.originalFilePath);
        if (orgFileText.length > 0) {
          try {
            additionalNames = JSON.parse(orgFileText);
          }
          catch (error) {
            throw `${error} : '${templateFileInfo.originalFilePath}'`;
          }
        }
      }

      for (const name of names) {
        let buffer = contentFileBuffer;
        const commentHeader = [
          this.staticCommentHeader,
          `"${fileName}"`
        ];
        if (name.names?.baseFileName) {
          const withNamesFileName = `with names file "${name.names.baseFileName}.names.json"`;
          commentHeader.push(withNamesFileName);
        }
        if (templateFileInfo.isTemplatedContent) {
          const fromTemplateFile = `from template file "${templateFileInfo.templateFileName}"`;
          commentHeader.push(fromTemplateFile);
        }
        commentHeader.push(this.staticCommentFooter);

        switch (filetype) {
          case ".jsonc":
            buffer = this.textHeader(buffer, commentHeader);
            break;
          case ".ps1":
            buffer = this.textHeader(buffer, commentHeader, '#');
            break;
        }

        buffer = this.textReplacement(buffer, name.names, additionalNames, templateFileInfo.originalFilePath);

        let outputFile = path.resolve(templateFileInfo.originalFilePath)
          .replace(path.resolve(this.inputDir), path.resolve(this.outputDir))
          .replace(folderName, name.outputFolderName);

        if (templateFileInfo.isTemplatedContent) {
          outputFile = path.join(path.dirname(outputFile), templateFileInfo.fileName);
        }

        FileHandler.CreateDirectory(path.dirname(outputFile));
        writeFileSync(outputFile, buffer);
      }
    }
  }

  private getNames(folderName: string): NameMatch[] {
    const compComponentsName = folderName.split(".")[0]?.toLowerCase();
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

    const compComponentsTemplate = folderName.split(".")[1];
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
      console.log(`warning: the file pattern ${compComponentsTemplate} is not valid: ${folderName}`)
      return [];
    }

    const names: NameMatch[] = [];

    const environments: Environment[] = comp.environments.filter(envFilter);
    for (const env of environments) {
      const planes = env.planes.filter(planeFilter);
      if (!planes?.length) {
        // {env}
        const staticEnv = new RegExp(`\\[${env.name}\\]`, "gi");
        const outputName = folderName.replace(/{Env}/gi, env.name).replace(staticEnv, env.name);
        names.push({
          folderName: folderName,
          outputFolderName: outputName,
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
            const outputName = folderName
              .replace(/{Env}/gi, env.name).replace(staticEnv, env.name)
              .replace(/{Plane}/gi, plane.name).replace(staticPlane, plane.name);
            names.push({
              folderName: folderName,
              outputFolderName: outputName,
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
                const outputName = folderName
                  .replace(/{Env}/gi, env.name).replace(staticEnv, env.name)
                  .replace(/{Plane}/gi, plane.name).replace(staticPlane, plane.name)
                  .replace(/{Instance}/gi, instance.name).replace(staticInstance, instance.name);
                names.push({
                  folderName: folderName,
                  outputFolderName: outputName,
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
                  const outputName = folderName
                    .replace(/{Env}/gi, env.name).replace(staticEnv, env.name)
                    .replace(/{Plane}/gi, plane.name).replace(staticPlane, plane.name)
                    .replace(/{Instance}/gi, instance.name).replace(staticInstance, instance.name)
                    .replace(/{Region}/gi, stamp.name).replace(staticRegion, stamp.name)
                  names.push({
                    folderName: folderName,
                    outputFolderName: outputName,
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
      console.log(`info: ${name.folderName} -> ${name.outputFolderName} (${name.names?.baseFileName}.names.json)`);
    }

    return names;
  }

  private containsMatch(matches: Match[], filters: string[]): boolean {
    return matches.filter((n) => filters.includes(n.key)).length > 0;
  }

  private textHeader(buffer: Buffer, injectStrings: string[] = [], commentDelimiter = '//'): Buffer {
    const text = buffer.toString("utf8");
    // Add single comment to strings, add new lines.
    // If nothing in the array, return the same text.
    const generatedText =
      injectStrings.length > 0
        ? `${injectStrings.map((n) => `${commentDelimiter} ${n}`).join("\r\n")}\r\n\r\n${text}`
        : text;
    return Buffer.from(generatedText, "utf8");
  }

  private textReplacement(buffer: Buffer, names: any, additionalNames: any, templateFile: string): Buffer {
    let text = buffer.toString("utf8");

    if (names) {
      const variablePattern = /[^{{{\\}]+(?=}}})/g;
      const matches = text.match(variablePattern);

      if (matches?.length > 0) {
        // Inject additional names to the original names
        if (additionalNames) {
          names = cloneDeep(names);
          Object.assign(names, additionalNames);
        }

        for (const match of matches) {
          // At least one file returns a false-positivle match '"";
          // just ignore it rather than risk breaking the regex variablePattern.
          if (match !== '"') {
            if (!Object.keys(names).includes(match)) {
              throw `error: property '${match}' does not exist in names object '${names.baseFileName}.names.json': template file '${templateFile}'`;
            }
          }
        }

        // Process each name
        for (const name in names) {
          const regex = new RegExp("{{{" + name + "}}}", "g");

          if (text.match(regex)) {
            let value = names[name];
            if (!value) {
              if (name !== "subscriptionId" && name !== 'subscriptionName') {
                throw `error: property '${name}' is undefined in names object '${names.baseFileName}.names.json': template file '${templateFile}'`;
              }
            }

            // Late invocation of 'name()'
            if (typeof value === 'function') {
              value = value.call(name);
            }

            const replaceValue = typeof value === 'string' ? value : JSON.stringify(value, null, 2)?.replace(/\n/g, '\r\n');
            text = text.replace(regex, replaceValue);
          }
        }
      }
    }

    return Buffer.from(text, "utf8");
  }

  private getTemplateList(folder: string, isNamesFolder: boolean): string[] {

    if (isNamesFolder) {
      // Names folders process in depth
      const files: string[] = [];
      FileHandler.GetAllFiles(folder, files);
      return files;
    }
    else {
      // Non-names folders only process the current directory
      const fileNames = FileHandler.GetFiles(folder);
      const files = fileNames.map(f => path.join(folder, f));
      return files;
    }
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
