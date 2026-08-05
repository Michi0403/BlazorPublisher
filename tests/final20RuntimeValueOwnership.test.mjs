import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const read = relative => fs.readFileSync(path.join(root, relative), "utf8").replace(/^\uFEFF/, "");

const panel = read("src/PublisherStudio.Web/Services/PanelStudioTextService.cs");
for (const forbidden of ["private readonly Regex", "new Regex(", "RegexOptions.", "TimeSpan.FromSeconds(2)", "_shutdownPattern", "_htmlBreakPattern", "_unsafeFileNamePattern"]) {
  assert.equal(panel.includes(forbidden), false, forbidden);
}
for (const required of ["IPanelStudioTextPatternDataService patterns", "_patterns.ShutdownPattern", "_patterns.HtmlBreakPattern", "_patterns.HtmlTagPattern", "_patterns.UnsafeFileNamePattern"]) {
  assert.equal(panel.includes(required), true, required);
}

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
const registrations = read("src/PublisherStudio.Web/PublisherStudioServiceCollectionExtensions.cs");
assert.equal(registrations.includes("AddSingleton<IPanelStudioTextPatternDataService, PanelStudioTextPatternDataService>"), true);
assert.equal(registrations.includes("AddSingleton<PanelStudioTextService, PanelStudioTextService>"), true);

console.log("PublisherStudio runtime-value ownership checks passed.");
