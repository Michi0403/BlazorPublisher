import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import assert from "node:assert/strict";

const root = path.resolve(import.meta.dirname, "..");
const read = relative => fs.readFileSync(path.join(root, relative), "utf8");

test("browser-measured selection frame follows complex rendered objects", () => {
  const page = read("src/PublisherStudio.Web/Components/Editor/PageSurface.razor");
  const js = read("src/PublisherStudio.Web/wwwroot/js/publisherInterop.js");
  const css = read("src/PublisherStudio.Web/wwwroot/css/site.css");
  assert.match(page, /data-selection-visual-frame/);
  assert.match(js, /function renderedSelectionBounds/);
  assert.match(js, /scheduleSelectionVisualFrame\(state\)/);
  assert.match(js, /visualElementId/);
  assert.match(css, /publication-page\.selection-visual-active/);
});

test("component diagnostics are globally available and exceptions are surfaced", () => {
  const imports = read("src/PublisherStudio.Web/Components/_Imports.razor");
  const layout = read("src/PublisherStudio.Web/Components/Layout/MainLayout.razor");
  const boundary = read("src/PublisherStudio.Web/Components/Shared/OperationalErrorBoundary.cs");
  const targets = read("Directory.Build.targets");
  const guard = read("build/Assert-OperationalDiagnostics.ps1");
  assert.match(imports, /OperationalLoggerFactory/);
  assert.match(imports, /OperationalNotifications/);
  assert.match(layout, /OperationalErrorBoundary/);
  assert.match(boundary, /LogError/);
  assert.match(boundary, /Notifications\.Error/);
  assert.match(guard, /<OperationalErrorBoundary\(\?:\\s\|>\)/);
  assert.match(targets, /Assert-OperationalDiagnostics\.ps1/);
});
