import assert from "node:assert/strict";
import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const read = (relative) => fs.readFileSync(path.join(root, relative), "utf8")
  .replace(/^\uFEFF/, "")
  .replace(/\r\n/g, "\n")
  .replace(/\r/g, "\n");
const normalizedHash = (relative) => crypto.createHash("sha256").update(read(relative), "utf8").digest("hex");

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

const expectedGuardHashes = new Map([
  ["build/Assert-MethodDiagnostics.ps1", "a8d484c5794306b165c58c33cc895938d424543abbdad707fc36e421335f86cb"],
  ["build/method-diagnostics-baseline.json", "787f5f9014f560d811842adf9f6ec885ca31c98e5cdcb49f67c8cf7cc7f2ce97"],
  ["build/Assert-SecurityRulePreservation.ps1", "7761ced2be171534fba99e71afe2e5ecda004ae7ba961bde5f9c4d5313dc7d19"],
  ["build/security-rules-final19.sha256", "73f2d4efede172ce3fbeb14a56739af6643ddf534a9ea7140a9acda13cb850b5"],
  ["build/Assert-RuntimeValueOwnership.ps1", "8f55f90d95a75d817923b784e57e589fe7e4413763938e78eb6f76567ba2e6f0"],
  ["build/runtime-value-ownership-baseline.json", "f4a2423f226cba280d7106c61dbc0f45dcae3631f2c038e5332a7c55cc2442d5"],
]);
for (const [relative, expected] of expectedGuardHashes) {
  assert.equal(normalizedHash(relative), expected, `guard changed: ${relative}`);
}

for (const manifest of ["build/protected-architecture-files.sha256", "build/security-rules-final19.sha256"]) {
  for (const raw of read(manifest).split(/\n/)) {
    const line = raw.trim();
    if (!line || line.startsWith("#")) continue;
    const separator = line.indexOf("  ");
    assert.notEqual(separator, -1, line);
    const expected = line.slice(0, separator);
    const relative = line.slice(separator + 2);
    assert.equal(normalizedHash(relative), expected, `manifest mismatch: ${relative}`);
  }
}

console.log("PublisherStudio final22 compiler/diagnostics regression checks passed.");
