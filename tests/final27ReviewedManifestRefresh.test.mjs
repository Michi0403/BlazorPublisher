import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';

const root = path.resolve(import.meta.dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

test('pattern service owns the injected logger used by instance helpers', () => {
  const source = read('src/PublisherStudio.Web/Services/Configuration/PanelStudioTextPatternDataService.cs');
  assert.match(source, /private readonly ILogger<PanelStudioTextPatternDataService> logger;/);
  assert.match(source, /this\.logger = logger;/);
  assert.doesNotMatch(source, /private static (?:Regex|Dictionary<string, PatternDefinition>|void) (?:RequirePattern|ReadStore|Compile|ValidateOptions)/);
});
