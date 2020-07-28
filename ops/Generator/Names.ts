import { Component } from "./Parser/Components";
import FileHandler from "./Helpers/FileHandler";
import { Environment } from "./Parser/Environments";

export default abstract class Names {

  public static Generate(
    components: Component[],
    environments: Environment[],
    globalPrefix: string,
    outputDir: string
  ): void {
    for (const comp of components) {
      const componentJson = comp.generateNamesJson(globalPrefix);
      FileHandler.GenerateJson(outputDir, `${componentJson.baseName}.names.json`, componentJson);

      for (const envOriginal of environments) {
        const env = envOriginal.clone();
        comp.environments.push(env);
        const envJson = env.generateNamesJson(comp.outputNames);
        FileHandler.GenerateJson(outputDir, `${envJson.baseName}.names.json`, envJson);

        for (const plane of env.planes) {
          const planeJson = plane.generateNamesJson(env.outputNames)
          FileHandler.GenerateJson(outputDir, `${planeJson.baseName}.names.json`, planeJson);

          for (const instance of plane.instances) {
            const instanceRegions = instance.stamps.flatMap((n) => n.location);
            const instanceJson = instance.generateNamesJson(plane.outputNames, instanceRegions);
            FileHandler.GenerateJson(outputDir, `${instanceJson.baseName}.names.json`, instanceJson);

            for (const stamp of instance.stamps) {
              const stampJson = stamp.location.generateNamesJson(instance.outputNames);
              FileHandler.GenerateJson(outputDir, `${stampJson.baseName}.names.json`, stampJson);
            }
          }
        }
      }
    }
  }
}
