import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';

const root = path.resolve(import.meta.dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');
const hash = relative => crypto.createHash('sha256').update(read(relative).replace(/\r\n?/g, '\n'), 'utf8').digest('hex');

test('final27 pattern service owns the injected logger used by instance helpers', () => {
  const source = read('src/PublisherStudio.Web/Services/Configuration/PanelStudioTextPatternDataService.cs');
  assert.match(source, /private readonly ILogger<PanelStudioTextPatternDataService> logger;/);
  assert.match(source, /this\.logger = logger;/);
  assert.doesNotMatch(source, /private static (?:Regex|Dictionary<string, PatternDefinition>|void) (?:RequirePattern|ReadStore|Compile|ValidateOptions)/);
});

test('final27 reviewed manifest refresher cannot rewrite final19 security hashes', () => {
  const script = read('build/Update-ReviewedProtectionManifest.ps1');
  for (const token of ['SupportsShouldProcess', 'ConfirmImpact', 'ReviewCurrentChanges', 'ReviewedFiles', 'Assert-SecurityRulePreservation.ps1', 'security-rules-final19.sha256', 'Security or 1-Wire preservation file cannot be refreshed', 'WriteAllBytes']) {
    assert.ok(script.includes(token), token);
  }
  assert.ok(script.includes("Invoke-RequiredSafeguard 'build/Assert-JavaScriptDiagnostics.ps1'"));
  assert.ok(script.includes("Invoke-RequiredSafeguard 'build/Assert-ProtectedArchitectureFiles.ps1'"));
});

test('final27 refresher, documentation and test are protected by the current manifest', () => {
  const guard = read('build/Assert-ProtectedArchitectureFiles.ps1');
  const manifest = read('build/protected-architecture-files.sha256');
  for (const relative of ['build/Update-ReviewedProtectionManifest.ps1', 'docs/REVIEWED_MANIFEST_REFRESH.md', 'tests/final27ReviewedManifestRefresh.test.mjs']) {
    assert.ok(guard.includes(`'${relative}'`), relative);
    assert.match(manifest, new RegExp(`^${hash(relative)}  ${relative.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}$`, 'm'));
  }
});
