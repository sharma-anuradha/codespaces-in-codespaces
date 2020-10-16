import { readFileSync, existsSync } from "fs";
import { EnvironmentsDeployment } from "./Parser/Environments";
import { ComponentsDeployment, Component, Subscription } from "./Parser/Components";
import * as path from "path";
import Names from "./Names";
import Templates from "./Templates";
import FileHandler from "./Helpers/FileHandler";
import * as chokidar from "chokidar";
import { program } from "commander";

program
  .requiredOption("-i, --input <inputDir>", "Input Directory", "../Components")
  .requiredOption("-t --templates <templatedir>", "Templates Directory", "../Components/Templates")
  .requiredOption(
    "-o, --output <outputDir>",
    "Output Directory",
    "../Components.Generated"
  )
  .option("-h, --hot-reload", "Enable Hot Reload File Watcher")
  .parse();

class Main {
  readonly inputDir: string;
  readonly templatesDir: string;
  readonly outputDir: string;
  readonly envDep: EnvironmentsDeployment;
  readonly compDep: ComponentsDeployment;
  readonly components: Component[];

  constructor(inputDir: string, templatesDir: string, outputDir: string) {
    /*
        Verify and/or create the input and output directories.
        If they don't exist or we can't make them, throw.
      */
    this.inputDir = path.normalize(inputDir.trim());
    this.templatesDir = path.normalize(templatesDir.trim())
    this.outputDir = path.normalize(outputDir.trim());
    this.verifyInputOutputDirs(this.inputDir, this.outputDir);
    const compJson = JSON.parse(
      readFileSync(path.join(this.inputDir, "components.json"), "utf8")
    );
    const envJson = JSON.parse(
      readFileSync(path.join(this.inputDir, "environments.json"), "utf8")
    );
    const subsJson = JSON.parse(
      readFileSync(path.join(this.inputDir, "subscriptions.json"), "utf8")
    ) as Subscription[];

    this.compDep = new ComponentsDeployment(compJson, subsJson);
    this.envDep = new EnvironmentsDeployment(envJson);
    this.components = this.compDep.components;
  }

  GenerateTemplates() {
    const templates = new Templates(
      this.inputDir,
      this.templatesDir,
      this.outputDir,
      this.components
    );
    templates.Generate();
  }

  GenerateNames() {
    const namesOutputDir = path.join(this.outputDir, "_names");
    Names.Generate(
      this.compDep.components,
      this.envDep.environments,
      this.compDep.globalPrefix,
      namesOutputDir
    );
  }

  verifyInputOutputDirs(inputDir: string, outputDir: string) {
    if (!existsSync(inputDir)) {
      throw `inputDir (${inputDir}) doesn't exist.`;
    }

    if (!existsSync(outputDir)) {
      FileHandler.CreateDirectory(outputDir);
    }
  }
}

const main = new Main(program.input, program.templates, program.output);
main.GenerateNames();
main.GenerateTemplates();
console.info(`info: Done, check ${main.outputDir}`);

// If running JSON Hot Reload command, set up file watch to check for new changes.
if (program.hotReload) {
  console.info(
    `info: Hot Reload Enabled, File Watcher set up on '${main.inputDir}'...`
  );
  const watcher = chokidar.watch(main.inputDir, {
    persistent: true,
  });
  watcher.on("change", (path) => {
    console.info(`info: Updated ${path}`);
    main.GenerateNames();
    main.GenerateTemplates();
    console.info(`info: Done, check ${main.outputDir}`);
  });
}
