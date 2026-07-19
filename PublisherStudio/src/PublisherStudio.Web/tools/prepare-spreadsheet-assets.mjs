import { cp, mkdir, rm, stat } from "node:fs/promises";
import { join, resolve } from "node:path";

const project = resolve(import.meta.dirname, "..");
const vendor = join(project, "wwwroot", "vendor");

async function exists(path) {
    try { await stat(path); return true; } catch { return false; }
}

async function copyPackage(sourceRelative, destinationRelative) {
    const source = join(project, "node_modules", sourceRelative);
    if (!await exists(source)) throw new Error(`Missing npm package content: ${sourceRelative}. Run npm install first.`);
    const destination = join(vendor, destinationRelative);
    await rm(destination, { recursive: true, force: true });
    await mkdir(destination, { recursive: true });
    await cp(source, destination, { recursive: true, force: true });
}

await mkdir(vendor, { recursive: true });
await copyPackage("devextreme-dist", "devextreme-dist");
await copyPackage("devexpress-aspnetcore-spreadsheet", "devexpress-aspnetcore-spreadsheet");
await mkdir(join(vendor, "jquery"), { recursive: true });
await cp(join(project, "node_modules", "jquery", "dist", "jquery.min.js"), join(vendor, "jquery", "jquery.min.js"), { force: true });
console.log("PublisherStudio spreadsheet client assets prepared.");
