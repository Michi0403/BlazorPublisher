import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const source = fs.readFileSync(path.join(root, "src/PublisherStudio.Web/BusinessObjects/PanelTextPatternStoreOptions.cs"), "utf8");
assert.equal(source.includes("class PanelTextPatternStoreOptions"), true);
assert.equal(source.includes("ILogger"), false);
assert.equal(source.includes(".Log"), false);
assert.equal(source.includes("catch"), false);
console.log("PublisherStudio panel text-pattern options remain a pure configuration type.");
