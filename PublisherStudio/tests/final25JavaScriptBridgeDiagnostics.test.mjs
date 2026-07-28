import assert from "node:assert/strict";
import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const bridgeRelative = "src/PublisherStudio.Web/Components/Layout/JavaScriptDiagnosticsBridge.razor";

function read(relative) {
  return fs.readFileSync(path.join(root, relative), "utf8");
}

function normalizedHash(relative) {
  const normalized = read(relative).replace(/\r\n?/g, "\n");
  return crypto.createHash("sha256").update(normalized, "utf8").digest("hex");
}

function loadManifest(relative) {
  const entries = new Map();
  for (const rawLine of read(relative).split(/\r?\n/)) {
    const line = rawLine.trim();
    if (!line || line.startsWith("#")) continue;
    const separator = line.indexOf("  ");
    entries.set(line.slice(separator + 2).replaceAll("\\", "/"), line.slice(0, separator).toLowerCase());
  }
  return entries;
}

test("JavaScript diagnostics bridge has catch, log, and user notification boundary", () => {
  const bridge = read(bridgeRelative);
  assert.match(bridge, /catch \(Exception exception\)/);
  assert.match(bridge, /Logger\.LogError\(exception,/);
  assert.match(bridge, /OperationalNotifications\.Error\(/);
  assert.match(bridge, /nameof\(JavaScriptDiagnosticsBridge\)/);
});

test("component diagnostics policy recognizes the notification boundary", () => {
  const policy = read("build/Assert-ComponentDiagnostics.ps1");
  assert.match(policy, /OperationalNotifications/);
  assert.match(policy, /notificationCount/);
});

test("protected architecture manifest contains the reviewed bridge hash", () => {
  const manifest = loadManifest("build/protected-architecture-files.sha256");
  assert.equal(manifest.get(bridgeRelative), normalizedHash(bridgeRelative));
});
