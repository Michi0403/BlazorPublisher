'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

test('Council subcomponent retains asynchronous lifecycle and transport operation', () => {
  const file = read('src/PublisherStudio.Web/Components/OrganicPlugins/OrganicCouncilRequestForm.razor.cs');
  assert.match(file, /protected override Task OnParametersSetAsync\(\)/);
  assert.doesNotMatch(file, /protected override void OnParametersSet\(/);
  assert.match(file, /return base.OnParametersSetAsync\(\)/);
  assert.match(file, /Connection\.SendCouncilRequestAsync\(Request\)/);
  assert.match(file, /Notifications\.Error\(/);
});

test('Council BusinessObject validation is eager, diagnostic-safe, and preserves member errors', () => {
  const file = read('src/PublisherStudio.Web/BusinessObjects/OrganicCouncilRequestFormModel.cs');
  assert.match(file, /IValidatableObject/);
  assert.match(file, /var errors = new List<ValidationResult>\(\)/);
  assert.match(file, /return errors;/);
  assert.doesNotMatch(file, /\byield\s+return\b/);
  assert.match(file, /catch \(JsonException\)/);
  for (const field of ['Prompt','TeamKey','ExternalProjectContextJson']) {
    assert.match(file, new RegExp(`errors\\.Add\\(new ValidationResult\\([^;]*nameof\\(${field}\\)`));
  }
});

test('original build-blocking guards and latest project version are retained', () => {
  const targets = read('Directory.Build.targets');
  assert.match(targets, /Assert-ComponentSafety\.ps1/);
  assert.match(targets, /Assert-IteratorExceptionPolicy\.ps1/);
  assert.match(read('build/Assert-ComponentSafety.ps1'), /Synchronous component lifecycle overrides are forbidden/);
  assert.match(read('build/Assert-IteratorExceptionPolicy.ps1'), /iterator contains catch/);
  assert.match(read('src/PublisherStudio.Web/PublisherStudio.Web.csproj'), /<Version>\d+\.\d+\.\d+<\/Version>/);
});
