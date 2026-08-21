import { createHash } from "node:crypto";
import { copyFile, cp, mkdir, readFile, readdir, realpath, rename, rm, stat, writeFile } from "node:fs/promises";
import { dirname, join, relative, resolve, sep } from "node:path";

const project = resolve(import.meta.dirname, "..");
const nodeModules = join(project, "node_modules");
const vendor = join(project, "wwwroot", "vendor");
const packageJson = JSON.parse(await readFile(join(project, "package.json"), "utf8"));
const packageLock = JSON.parse(await readFile(join(project, "package-lock.json"), "utf8"));
const devExtremeVersion = packageJson.dependencies?.["devextreme-dist"];
const lockedDevExtreme = packageLock.packages?.["node_modules/devextreme-dist"];
const lockedSpreadsheet = packageLock.packages?.["node_modules/devexpress-aspnetcore-spreadsheet"];
const authoritativeDevExtremeRoot = String(process.env.PUBLISHERSTUDIO_DEVEXTREME_SOURCE_ROOT || "").trim();

if (!devExtremeVersion) {
    throw new Error("package.json does not define dependencies.devextreme-dist.");
}
if (!lockedDevExtreme || String(lockedDevExtreme.version || "").trim() !== devExtremeVersion) {
    throw new Error(
        `package-lock.json must lock devextreme-dist exactly to ${devExtremeVersion}; ` +
        `found ${String(lockedDevExtreme?.version || "missing")}. Run npm install to refresh the lock file.`
    );
}
if (!String(lockedDevExtreme.integrity || "").trim()) {
    throw new Error("package-lock.json does not contain an integrity hash for devextreme-dist.");
}
if (!lockedSpreadsheet || String(lockedSpreadsheet.version || "").trim() !== devExtremeVersion) {
    throw new Error(
        `package-lock.json must lock devexpress-aspnetcore-spreadsheet exactly to ${devExtremeVersion}; ` +
        `found ${String(lockedSpreadsheet?.version || "missing")}.`
    );
}

async function exists(path) {
    try {
        await stat(path);
        return true;
    } catch {
        return false;
    }
}

async function removeGeneratedDirectory(path) {
    if (!await exists(path)) return;

    let lastError = null;
    for (let attempt = 0; attempt < 8; attempt++) {
        try {
            await rm(path, {
                recursive: true,
                force: true,
                maxRetries: 4,
                retryDelay: 125
            });
            if (!await exists(path)) return;
        } catch (error) {
            lastError = error;
        }
        await new Promise(resolveDelay => setTimeout(resolveDelay, 150 * (attempt + 1)));
    }

    // Antivirus/indexer handles on Windows can briefly block a recursive rmdir.
    // A rename removes the stale package from the live vendor path immediately;
    // a best-effort cleanup then removes the renamed directory.
    const stale = `${path}.stale-${Date.now()}-${process.pid}`;
    try {
        await rename(path, stale);
        try {
            await rm(stale, {
                recursive: true,
                force: true,
                maxRetries: 8,
                retryDelay: 150
            });
        } catch {
            // The renamed stale directory is not addressable by PublisherStudio.
            // A later preparation run can remove it.
        }
        return;
    } catch (error) {
        lastError = error;
    }

    throw new Error(
        `Could not replace generated browser package '${path}'. ` +
        `Close any process that has the generated vendor directory open and retry. ` +
        `Last error: ${lastError?.message || lastError || "unknown error"}`
    );
}

async function packageVersion(packageRoot) {
    const packagePath = join(packageRoot, "package.json");
    if (!await exists(packagePath)) {
        throw new Error(`Missing package metadata: ${packagePath}`);
    }
    const metadata = JSON.parse(await readFile(packagePath, "utf8"));
    return String(metadata.version || "").trim();
}



if (!authoritativeDevExtremeRoot) {
    throw new Error(
        "PUBLISHERSTUDIO_DEVEXTREME_SOURCE_ROOT is missing. Prepare-DevExpressAssets.ps1 must resolve the exact devextreme package used by the license generator before copying browser assets."
    );
}
const authoritativeDevExtremeMetadataVersion = await packageVersion(authoritativeDevExtremeRoot);
if (authoritativeDevExtremeMetadataVersion !== devExtremeVersion) {
    throw new Error(
        `The authoritative devextreme package is ${authoritativeDevExtremeMetadataVersion || "unknown"}, ` +
        `but PublisherStudio requires ${devExtremeVersion}.`
    );
}

async function copyPackage(sourceRelative, destinationRelative) {
    const source = join(nodeModules, sourceRelative);
    if (!await exists(source)) {
        throw new Error(`Missing npm package content: ${sourceRelative}. Run npm install first.`);
    }

    const destination = join(vendor, destinationRelative);
    await removeGeneratedDirectory(destination);
    await mkdir(dirname(destination), { recursive: true });

    // DevExpress packages may contain directory links on Windows. Copy their
    // resolved contents so the published wwwroot tree never contains broken
    // links or depends on node_modules after preparation.
    await cp(source, destination, {
        recursive: true,
        force: true,
        dereference: true,
        errorOnExist: false
    });
}

async function overlayAuthoritativeDevExtremeRuntime() {
    const sourceDist = join(authoritativeDevExtremeRoot, "dist");
    const destinationRoot = join(vendor, "devextreme-dist");
    if (!await exists(sourceDist)) {
        throw new Error(`The authoritative devextreme@${devExtremeVersion} package does not contain its dist directory.`);
    }

    // The same exact devextreme package that owns devextreme-license also owns the
    // browser runtime overlay. This prevents a devextreme-dist packaging-metadata
    // discrepancy from pairing an older dx.all.js with a newer generated key.
    const entries = await readdir(sourceDist, { withFileTypes: true });
    for (const entry of entries) {
        const source = join(sourceDist, entry.name);
        const destination = join(destinationRoot, entry.name);
        if (entry.isDirectory()) {
            await cp(source, destination, {
                recursive: true,
                force: true,
                dereference: true,
                errorOnExist: false
            });
        } else if (entry.isFile()) {
            await mkdir(dirname(destination), { recursive: true });
            await copyFile(source, destination);
        }
    }
}

async function findFileBySuffix(root, suffixParts, depth = 0) {
    if (depth > 6 || !await exists(root)) return null;

    const normalizedSuffix = suffixParts.join(sep).toLowerCase();
    const entries = await readdir(root, { withFileTypes: true });
    entries.sort((left, right) => left.name.localeCompare(right.name));

    for (const entry of entries) {
        const candidate = join(root, entry.name);
        if (entry.isFile() && candidate.toLowerCase().endsWith(normalizedSuffix)) {
            return candidate;
        }
    }

    for (const entry of entries) {
        const candidate = join(root, entry.name);
        if (entry.isDirectory()) {
            const nested = await findFileBySuffix(candidate, suffixParts, depth + 1);
            if (nested) return nested;
            continue;
        }

        if (entry.isSymbolicLink()) {
            try {
                const target = await realpath(candidate);
                const targetState = await stat(target);
                if (targetState.isDirectory()) {
                    const nested = await findFileBySuffix(target, suffixParts, depth + 1);
                    if (nested) return nested;
                }
            } catch {
                // A broken npm link is not a usable source. Continue to the
                // next candidate and report the complete source failure below.
            }
        }
    }

    return null;
}

async function resolveAssetSource(relativeAsset) {
    const relativeParts = relativeAsset.split("/");
    const directCandidates = [
        join(authoritativeDevExtremeRoot, "dist", ...relativeParts),
        join(authoritativeDevExtremeRoot, ...relativeParts),
        join(nodeModules, "devextreme-dist", ...relativeParts),
        join(nodeModules, "devextreme-dist", "dist", ...relativeParts),
        join(nodeModules, "devextreme", "dist", ...relativeParts),
        join(nodeModules, "devextreme", ...relativeParts)
    ];

    for (const candidate of directCandidates) {
        if (await exists(candidate)) return candidate;
    }

    for (const root of [
        authoritativeDevExtremeRoot,
        join(nodeModules, "devextreme-dist"),
        join(nodeModules, "devextreme")
    ]) {
        const discovered = await findFileBySuffix(root, relativeParts);
        if (discovered) return discovered;
    }

    throw new Error(
        `The restored DevExtreme npm packages do not contain '${relativeAsset}'. ` +
        "Remove node_modules and package-lock.json only if the lock file is known to be corrupt, then restore again."
    );
}

async function sha256(path) {
    const digest = createHash("sha256");
    digest.update(await readFile(path));
    return digest.digest("hex");
}

async function ensureDevExtremeAsset(relativeAsset, minimumBytes) {
    const destination = join(vendor, "devextreme-dist", ...relativeAsset.split("/"));
    let source = destination;

    if (!await exists(destination) || (await stat(destination)).size < minimumBytes) {
        source = await resolveAssetSource(relativeAsset);
        await mkdir(dirname(destination), { recursive: true });
        await copyFile(source, destination);
    }

    const destinationState = await stat(destination);
    if (!destinationState.isFile() || destinationState.size < minimumBytes) {
        throw new Error(
            `Prepared DevExtreme asset '${relativeAsset}' is incomplete (${destinationState.size} bytes).`
        );
    }

    return {
        path: `devextreme-dist/${relativeAsset}`,
        source: relative(project, source).replaceAll("\\", "/"),
        bytes: destinationState.size,
        sha256: await sha256(destination)
    };
}

const restoredDevExtremeMetadataVersion = await packageVersion(join(nodeModules, "devextreme-dist"));
if (restoredDevExtremeMetadataVersion !== devExtremeVersion) {
    console.warn(
        `PublisherStudio: devextreme-dist package.json reports ${restoredDevExtremeMetadataVersion || "unknown"} ` +
        `while package-lock.json restores ${devExtremeVersion}. Continuing because npm ci validates the exact ` +
        `locked archive integrity (${lockedDevExtreme.integrity}); the internal package metadata is diagnostic only.`
    );
}

const restoredSpreadsheetMetadataVersion = await packageVersion(join(nodeModules, "devexpress-aspnetcore-spreadsheet"));
if (restoredSpreadsheetMetadataVersion !== devExtremeVersion) {
    console.warn(
        `PublisherStudio: devexpress-aspnetcore-spreadsheet package.json reports ` +
        `${restoredSpreadsheetMetadataVersion || "unknown"} while the lock file requires ${devExtremeVersion}.`
    );
}

await mkdir(vendor, { recursive: true });
await copyPackage("devextreme-dist", "devextreme-dist");
await overlayAuthoritativeDevExtremeRuntime();
await copyPackage("devexpress-aspnetcore-spreadsheet", "devexpress-aspnetcore-spreadsheet");

const copiedDevExtremeMetadataVersion = await packageVersion(join(vendor, "devextreme-dist"));
if (copiedDevExtremeMetadataVersion !== devExtremeVersion) {
    console.warn(
        `PublisherStudio: copied devextreme-dist package.json reports ${copiedDevExtremeMetadataVersion || "unknown"}; ` +
        `the prepared manifest will retain the authoritative lock version and content hashes.`
    );
}

await removeGeneratedDirectory(join(vendor, "jquery"));
await mkdir(join(vendor, "jquery"), { recursive: true });
await copyFile(
    join(nodeModules, "jquery", "dist", "jquery.min.js"),
    join(vendor, "jquery", "jquery.min.js")
);

const preparedAssets = [
    await ensureDevExtremeAsset("js/dx.all.js", 512 * 1024),
    await ensureDevExtremeAsset("css/dx.light.css", 64 * 1024)
];

await writeFile(
    join(vendor, "devextreme-assets.meta.json"),
    `${JSON.stringify({
        schemaVersion: 4,
        devExtremeVersion,
        authoritativeRuntimePackage: "devextreme",
        authoritativeRuntimePackageVersion: authoritativeDevExtremeMetadataVersion,
        restoredPackageVersion: devExtremeVersion,
        lockedPackageVersion: String(lockedDevExtreme.version || "").trim(),
        lockedPackageResolved: String(lockedDevExtreme.resolved || "").trim(),
        lockedPackageIntegrity: String(lockedDevExtreme.integrity || "").trim(),
        restoredPackageMetadataVersion: restoredDevExtremeMetadataVersion,
        copiedPackageMetadataVersion: copiedDevExtremeMetadataVersion,
        preparedAtUtc: new Date().toISOString(),
        package: "devextreme-dist",
        assets: preparedAssets
    }, null, 2)}\n`,
    "utf8"
);

console.log(`PublisherStudio DevExpress client assets prepared for DevExtreme ${devExtremeVersion}.`);
