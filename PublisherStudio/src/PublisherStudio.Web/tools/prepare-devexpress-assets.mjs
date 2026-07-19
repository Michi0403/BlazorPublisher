import { cp, mkdir, readFile, rm, stat, writeFile } from "node:fs/promises";
import { join, resolve } from "node:path";

const project = resolve(import.meta.dirname, "..");
const vendor = join(project, "wwwroot", "vendor");
const packageJson = JSON.parse(await readFile(join(project, "package.json"), "utf8"));
const devExtremeVersion = packageJson.dependencies?.["devextreme-dist"];

if (!devExtremeVersion) {
    throw new Error("package.json does not define dependencies.devextreme-dist.");
}

async function exists(path) {
    try {
        await stat(path);
        return true;
    } catch {
        return false;
    }
}

async function copyPackage(sourceRelative, destinationRelative) {
    const source = join(project, "node_modules", sourceRelative);
    if (!await exists(source)) {
        throw new Error(`Missing npm package content: ${sourceRelative}. Run npm install first.`);
    }

    const destination = join(vendor, destinationRelative);
    await rm(destination, { recursive: true, force: true });
    await mkdir(destination, { recursive: true });
    await cp(source, destination, { recursive: true, force: true });
}

async function validateRuntimeLicense() {
    const licensePath = join(vendor, "devextreme-license.js");
    if (!await exists(licensePath)) {
        throw new Error(
            "The generated DevExtreme runtime license is missing. Run the devextreme-license CLI before preparing the client assets."
        );
    }

    const source = await readFile(licensePath, "utf8");
    const hasConfigCall = /DevExpress\s*\.\s*config\s*\(/.test(source);
    const hasLicenseProperty = /licenseKey\s*:/.test(source);
    const hasNonEmptyQuotedValue = /licenseKey\s*:\s*(["'`])(?:(?!\1).)+\1/.test(source);

    if (!hasConfigCall || !hasLicenseProperty || !hasNonEmptyQuotedValue) {
        throw new Error(
            "The generated DevExtreme runtime license file is empty or malformed. Regenerate it on a licensed build machine."
        );
    }

    const metadata = {
        schemaVersion: 1,
        devExtremeVersion,
        generatedAtUtc: new Date().toISOString(),
        generator: `devextreme-license from devextreme@${devExtremeVersion}`
    };
    await writeFile(
        join(vendor, "devextreme-license.meta.json"),
        `${JSON.stringify(metadata, null, 2)}\n`,
        "utf8"
    );
    await writeFile(join(vendor, "devextreme-license.version"), `${devExtremeVersion}\n`, "utf8");
}

await mkdir(vendor, { recursive: true });
await copyPackage("devextreme-dist", "devextreme-dist");
await copyPackage("devexpress-aspnetcore-spreadsheet", "devexpress-aspnetcore-spreadsheet");
await mkdir(join(vendor, "jquery"), { recursive: true });
await cp(
    join(project, "node_modules", "jquery", "dist", "jquery.min.js"),
    join(vendor, "jquery", "jquery.min.js"),
    { force: true }
);
await validateRuntimeLicense();

console.log(`PublisherStudio DevExpress client assets prepared for DevExtreme ${devExtremeVersion}.`);
