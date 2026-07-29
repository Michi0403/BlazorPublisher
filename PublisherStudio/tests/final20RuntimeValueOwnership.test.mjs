import assert from "node:assert/strict";
import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const app = path.join(root, "src", "PublisherStudio.Web");
const read = (relative) => fs.readFileSync(path.join(root, relative), "utf8").replace(/^\uFEFF/, "");
const normalizedHash = (relative) => crypto.createHash("sha256")
  .update(read(relative).replace(/\r\n/g, "\n").replace(/\r/g, "\n"), "utf8")
  .digest("hex");

const panel = read("src/PublisherStudio.Web/Services/PanelStudioTextService.cs");
for (const forbidden of ["private readonly Regex", "new Regex(", "RegexOptions.", "TimeSpan.FromSeconds(2)", "_shutdownPattern", "_htmlBreakPattern", "_unsafeFileNamePattern"]) {
  assert.equal(panel.includes(forbidden), false, forbidden);
}
for (const required of ["IPanelStudioTextPatternDataService patterns", "_patterns.ShutdownPattern", "_patterns.HtmlBreakPattern", "_patterns.HtmlTagPattern", "_patterns.UnsafeFileNamePattern"]) {
  assert.equal(panel.includes(required), true, required);
}

const ownershipGuard = read("build/Assert-RuntimeValueOwnership.ps1");
assert.equal(ownershipGuard.includes("GetRelativePath"), false);
assert.equal(ownershipGuard.includes(".Contains("), false);
assert.equal(ownershipGuard.includes(".IndexOf("), true);

const dataService = read("src/PublisherStudio.Web/Services/Configuration/PanelStudioTextPatternDataService.cs");
for (const required of ["ReadStore(seedPath)", "File.Exists(overridePath)", "JsonSerializer.Deserialize<PatternStoreDocument>", "TimeSpan.FromMilliseconds(definition.TimeoutMilliseconds)"]) {
  assert.equal(dataService.includes(required), true, required);
}

const store = JSON.parse(read("src/PublisherStudio.Web/Configuration/panel-text-patterns.json"));
assert.equal(store.schemaVersion, 1);
for (const name of ["ShutdownPattern", "HtmlBreakPattern", "HtmlTagPattern", "UnsafeFileNamePattern"]) {
  assert.ok(store.patterns[name], name);
  assert.equal(store.patterns[name].timeoutMilliseconds > 0, true, `${name} timeout`);
}

const settings = JSON.parse(read("src/PublisherStudio.Web/appsettings.json"));
assert.equal(settings.PublisherStudio.RuntimeValueStores.PanelTextPatterns.SeedPath, "Configuration/panel-text-patterns.json");
const project = read("src/PublisherStudio.Web/PublisherStudio.Web.csproj");
assert.equal(project.includes('Configuration\\panel-text-patterns.json'), true);
const registrations = read("src/PublisherStudio.Web/PublisherStudioServiceCollectionExtensions.cs");
assert.equal(registrations.includes("AddSingleton<IPanelStudioTextPatternDataService, PanelStudioTextPatternDataService>"), true);
assert.equal(registrations.includes("AddSingleton<PanelStudioTextService, PanelStudioTextService>"), true);

const baseline = JSON.parse(read("build/runtime-value-ownership-baseline.json"));
assert.equal(baseline.some((item) => item.includes("PanelStudioTextService.cs|") && item.includes("Regex")), false);
const targets = read("Directory.Build.targets");
assert.equal(targets.includes("AssertPublisherProtectedArchitectureFiles"), true);
assert.equal(targets.includes("AssertPublisherSecurityRulePreservation"), true);
assert.equal(targets.includes("AssertPublisherRuntimeValueOwnership"), true);


const protectedManifest = read("build/protected-architecture-files.sha256");
for (const raw of protectedManifest.split(/\r?\n/)) {
  const line = raw.trim();
  if (!line || line.startsWith("#")) continue;
  const separator = line.indexOf("  ");
  assert.notEqual(separator, -1, line);
  const expected = line.slice(0, separator);
  const relative = line.slice(separator + 2);
  assert.equal(normalizedHash(relative), expected, `protected architecture file changed: ${relative}`);
}

for (const raw of read("build/security-rules-final19.sha256").split(/\r?\n/)) {
  const line = raw.trim();
  if (!line || line.startsWith("#")) continue;
  const separator = line.indexOf("  ");
  assert.notEqual(separator, -1, line);
  const expected = line.slice(0, separator);
  const relative = line.slice(separator + 2);
  assert.equal(normalizedHash(relative), expected, `security rule changed: ${relative}`);
}

console.log("PublisherStudio final20 runtime-value ownership checks passed.");
