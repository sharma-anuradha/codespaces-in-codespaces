import {
  readFileSync,
  existsSync
} from "fs";
import { EnvironmentsDeployment } from "./Parser/Environments";
import { ComponentsDeployment, Component } from "./Parser/Components";
import * as path from "path";
import Names from "./Names";
import Templates from "./Templates";
import FileHandler from "./Helpers/FileHandler";

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
    this.components = this.compDep.components;
  }

  GenerateTemplates() {
    const templates = new Templates(this.inputDir, this.outputDir, this.components);
    templates.Generate();
  }

  GenerateNames() {
    Names.Generate(this.compDep.components, this.envDep.environments, this.compDep.globalPrefix, this.outputDir);
  }

  verifyInputOutputDirs(inputDir: string, outputDir: string) {
    if (inputDir == null || outputDir == null) {
      throw("ts-node-script index.ts [inputDir] [outputDir]");
    }

    if (!existsSync(inputDir)) {
      throw(`inputDir (${inputDir}) doesn't exist.`);
    }

    if (!existsSync(outputDir)) {
      FileHandler.CreateDirectory(outputDir);
    }
  }
}

const main = new Main(process.argv[2], process.argv[3]);
main.GenerateNames();
main.GenerateTemplates();
console.log(`Done, check ${process.argv[3]}`);
