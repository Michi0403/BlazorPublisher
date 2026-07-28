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

const expectedGuardHashes = new Map([
  ["build/Assert-LoggingIntegrity.ps1", "44f664184f9ce620d10040e8f8327ca3504ed62d38604fcc0d0dc4a81a94e3f4"],
  ["build/logging-baseline.json", "680e7aebe7d24f69018ff94344a2282dd16bd4d2679a4f526c4598d5253ef44e"],
  ["build/Assert-SecurityRulePreservation.ps1", "7761ced2be171534fba99e71afe2e5ecda004ae7ba961bde5f9c4d5313dc7d19"],
  ["build/security-rules-final19.sha256", "73f2d4efede172ce3fbeb14a56739af6643ddf534a9ea7140a9acda13cb850b5"],
]);
for (const [relative, expected] of expectedGuardHashes) {
  assert.equal(normalizedHash(relative), expected, `guard changed: ${relative}`);
}

const options = read("src/PublisherStudio.Web/Services/Configuration/PanelTextPatternStoreOptions.cs");
assert.equal(options.includes("// logging-policy: pure-helper"), true);
assert.equal(options.includes("ILogger"), false);
assert.equal(options.includes(".Log"), false);
assert.equal(options.includes("catch"), false);
assert.equal(options.includes("class PanelTextPatternStoreOptions"), true);

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

console.log("PublisherStudio final21 build-guard regression checks passed.");
