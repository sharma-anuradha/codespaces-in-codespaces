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
      FileHandler.GenerateJson(
        outputDir,
        `${comp.prefix}.names.json`,
        comp.generateNamesJson()
      );
      for (const env of environments) {
        FileHandler.GenerateJson(
          outputDir,
          `${comp.prefix}.${env.name}.names.json`,
          env.generateNamesJson(globalPrefix, comp.prefix)
        );
        for (const sub of comp.subscriptions) {
          FileHandler.GenerateJson(
            outputDir,
            `${comp.prefix}.${env.name}-${sub.plane}.names.json`,
            sub.generateNamesJson(
              globalPrefix,
              sub.env ?? env.name,
              comp.prefix
            )
          );

          for (const instance of env.instances) {
            FileHandler.GenerateJson(
              outputDir,
              `${comp.prefix}.${env.name}-${sub.plane}-${instance.name}.names.json`,
              instance.generateNamesJson(
                globalPrefix,
                sub.env ?? env.name,
                sub.plane,
                instance.stamps.flatMap((n) => n.dataLocations),
                comp.prefix
              )
            );

            for (const stamp of instance.stamps) {
              FileHandler.GenerateJson(
                outputDir,
                `${comp.prefix}.${env.name}-${sub.plane}-${instance.name}-${stamp.location.geography.name}-${stamp.location.region.name}.names.json`,
                stamp.location.generateNamesJson(
                  globalPrefix,
                  sub.env ?? env.name,
                  sub.plane,
                  instance.name,
                  comp.prefix
                )
              );
              for (const dl of stamp.dataLocations) {
                FileHandler.GenerateJson(
                  outputDir,
                  `${comp.prefix}.${env.name}-${sub.plane}-${instance.name}-${dl.geography.name}-${dl.region.name}.names.json`,
                  dl.generateNamesJson(
                    globalPrefix,
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
}
