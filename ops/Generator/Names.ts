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
      FileHandler.GenerateJson(outputDir, `${componentJson.baseFileName}.names.json`, componentJson);

      for (const envOriginal of environments) {
        if (comp.environmentNames.includes(envOriginal.name)) {
          const env = envOriginal.clone();
          comp.environments.push(env);
          const envJson = env.generateNamesJson(comp.outputNames);
          FileHandler.GenerateJson(outputDir, `${envJson.baseFileName}.names.json`, envJson);

          for (const plane of env.planes) {

            if (plane.name !== "data") {
              const subscriptions = comp.getSubscription(env.name, plane.name);
              const firstSub = subscriptions[0];
              if (firstSub) {
                plane.subscriptionName = firstSub.subscriptionName;
                plane.subscriptionId = firstSub.subscriptionId;
              }
            }

            const planeJson = plane.generateNamesJson(env.outputNames)
            FileHandler.GenerateJson(outputDir, `${planeJson.baseFileName}.names.json`, planeJson);

            for (const instance of plane.instances) {
              const instanceRegions = instance.stamps.flatMap((n) => n.location);
              const instanceJson = instance.generateNamesJson(plane.outputNames, instanceRegions);
              FileHandler.GenerateJson(outputDir, `${instanceJson.baseFileName}.names.json`, instanceJson);

              for (const stamp of instance.stamps) {
                const stampJson = stamp.location.generateNamesJson(instance.outputNames);
                FileHandler.GenerateJson(outputDir, `${stampJson.baseFileName}.names.json`, stampJson);
              }
            }
          }
        }
      }
    }
  }
}
