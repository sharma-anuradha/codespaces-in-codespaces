import { readFileSync, existsSync } from "fs";
import { EnvironmentsDeployment } from "./Parser/Environments";
import { ComponentsDeployment, Component } from "./Parser/Components";
import * as path from "path";
import Names from "./Names";
import Templates from "./Templates";
import FileHandler from "./Helpers/FileHandler";
import * as chokidar from "chokidar";
import { program } from "commander";

program
  .requiredOption("-i, --input <inputDir>", "Input Directory", "../Components")
  .requiredOption(
    "-o, --output <outputDir>",
    "Output Directory",
    "../Components.Generated"
  )
  .option("-h, --hot-reload", "Enable Hot Reload File Watcher")
  .parse();

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
    this.inputDir = path.normalize(inputDir.trim());
    this.outputDir = path.normalize(outputDir.trim());
    this.verifyInputOutputDirs(this.inputDir, this.outputDir);
    const compJson = JSON.parse(
      readFileSync(path.join(this.inputDir, "components.json"), "utf8")
    );
    const envJson = JSON.parse(
      readFileSync(path.join(this.inputDir, "environments.json"), "utf8")
    );

    this.compDep = new ComponentsDeployment(compJson);
    this.envDep = new EnvironmentsDeployment(envJson);
    this.components = this.compDep.components;
  }

  GenerateTemplates() {
    const templates = new Templates(
      this.inputDir,
      this.outputDir,
      this.components
    );
    templates.Generate();
  }

  GenerateNames() {
    Names.Generate(
      this.compDep.components,
      this.envDep.environments,
      this.compDep.globalPrefix,
      this.outputDir
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

const main = new Main(program.input, program.output);
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
