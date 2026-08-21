import { readFile, stat } from "node:fs/promises";
import { delimiter, dirname, join, resolve } from "node:path";

const expectedVersion = String(process.argv[2] || "").trim();
if (!expectedVersion) {
    throw new Error("Expected DevExtreme version argument is required.");
}

async function exists(path) {
    try {
        await stat(path);
        return true;
    } catch {
        return false;
    }
}

const candidates = [];
for (const pathEntry of String(process.env.PATH || "").split(delimiter)) {
    const entry = pathEntry.trim();
    if (!entry) continue;

    // npm exec / npx prepends its temporary node_modules/.bin directory to PATH.
    // The exact devextreme package selected by --package sits beside that .bin folder.
    candidates.push(resolve(entry, "..", "devextreme"));

    // Also tolerate a direct node_modules path if an npm implementation exposes it.
    candidates.push(resolve(entry, "devextreme"));
}

const seen = new Set();
for (const candidate of candidates) {
    const normalized = resolve(candidate);
    if (seen.has(normalized)) continue;
    seen.add(normalized);

    const packagePath = join(normalized, "package.json");
    const licenseCliPath = join(normalized, "bin", "devextreme-license.js");
    const distRoot = join(normalized, "dist");
    if (!await exists(packagePath) || !await exists(licenseCliPath) || !await exists(distRoot)) continue;

    const metadata = JSON.parse(await readFile(packagePath, "utf8"));
    if (String(metadata.version || "").trim() !== expectedVersion) continue;

    process.stdout.write(`${normalized}\n`);
    process.exit(0);
}

throw new Error(
    `npx did not expose the requested devextreme@${expectedVersion} package root. ` +
    "The license generator and browser runtime cannot be proven to come from the same package."
);
