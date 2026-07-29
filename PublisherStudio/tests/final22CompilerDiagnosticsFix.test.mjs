import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const read = (relative) => fs.readFileSync(path.join(root, relative), "utf8")
  .replace(/^\uFEFF/, "")
  .replace(/\r\n/g, "\n")
  .replace(/\r/g, "\n");
const service = read("src/PublisherStudio.Web/Services/Configuration/PanelStudioTextPatternDataService.cs");
for (const signature of [
  "private Regex RequirePattern(string name)",
  "private Dictionary<string, PatternDefinition> ReadStore(string path)",
  "private Regex Compile(string name, PatternDefinition definition)",
  "private void ValidateOptions(PanelTextPatternStoreOptions options)",
]) {
  assert.equal(service.includes(signature), true, `missing instance method: ${signature}`);
  assert.equal(service.includes(signature.replace("private ", "private static ")), false, `static helper retained: ${signature}`);
}

const methodRanges = [
  ["RequirePattern", "private Regex RequirePattern(string name)", "private Dictionary<string, PatternDefinition> ReadStore(string path)"],
  ["ReadStore", "private Dictionary<string, PatternDefinition> ReadStore(string path)", "private Regex Compile(string name, PatternDefinition definition)"],
  ["Compile", "private Regex Compile(string name, PatternDefinition definition)", "private void ValidateOptions(PanelTextPatternStoreOptions options)"],
  ["ValidateOptions", "private void ValidateOptions(PanelTextPatternStoreOptions options)", "\n\n}"],
];
for (const [methodName, signature, nextSignature] of methodRanges) {
  const start = service.indexOf(signature);
  assert.notEqual(start, -1, `method missing: ${methodName}`);
  const end = service.indexOf(nextSignature, start + signature.length);
  assert.notEqual(end, -1, `method end missing: ${methodName}`);
  const body = service.slice(start, end);
  assert.equal(body.includes("try"), true, `try missing: ${methodName}`);
  assert.equal(body.includes("catch (Exception exception)"), true, `catch missing: ${methodName}`);
  assert.equal(body.includes("logger.Log"), true, `logging missing: ${methodName}`);
  assert.equal(body.includes('$"'), true, `interpolated diagnostic missing: ${methodName}`);
  assert.equal(body.includes("throw;"), true, `fail-closed rethrow missing: ${methodName}`);
}

assert.equal(service.includes("ReadStore(seedPath)"), true);
assert.equal(service.includes("JsonSerializer.Deserialize<PatternStoreDocument>"), true);
assert.equal(service.includes("TimeSpan.FromMilliseconds(definition.TimeoutMilliseconds)"), true);

console.log("PublisherStudio final22 compiler/diagnostics regression checks passed.");
