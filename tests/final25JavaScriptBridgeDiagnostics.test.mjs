import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const bridgeRelative = "src/PublisherStudio.Web/Components/Layout/JavaScriptDiagnosticsBridge.razor";

function read(relative) {
  return fs.readFileSync(path.join(root, relative), "utf8");
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

