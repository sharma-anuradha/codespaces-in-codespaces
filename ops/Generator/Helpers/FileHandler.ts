import {
  readFileSync,
  existsSync,
  writeFileSync,
  mkdirSync,
  readdirSync,
  statSync,
} from "fs";
import * as path from "path";

export default abstract class FileHandler {

  public static GenerateJson(basePath: string, name: string, obj: any): void {
    this.CreateDirectory(basePath);
    writeFileSync(path.join(basePath, name), JSON.stringify(obj, null, 2));
  }

  public static CreateDirectory(dir: string): string {
    if (!existsSync(dir)) {
      return mkdirSync(dir, { recursive: true });
    }
    return "";
  }

  public static GetDirectories(inputDir: string, relative = true): string[] {
    const dirs = readdirSync(inputDir).filter((file) =>
      statSync(path.join(inputDir, file)).isDirectory()
    );
    if (relative) {
      return dirs;
    }
    return dirs.map((n) => path.join(inputDir, n));
  }

  public static GetFile(filePath: string, options?: any): Buffer {
      return readFileSync(filePath, options);
  }

  public static GetFiles(inputDir: string): string[] {
    return readdirSync(inputDir).filter(
      (file) => !statSync(path.join(inputDir, file)).isDirectory()
    );
  }

  public static GetAllFiles(inputDir: string, fileList = []): string[] {
    const folders: string[] = this.GetDirectories(inputDir);
    const files: string[] = this.GetFiles(inputDir);
    fileList.push(...files.map((n) => path.join(inputDir, n)));
    folders.forEach((folder) => {
      this.GetAllFiles(path.join(inputDir, folder), fileList);
    });
    return fileList;
  }

  public static SaveFile(path: string, buffer: Buffer): void {
      writeFileSync(path, buffer);
  }
}
