// Static component/transport regressions for the advanced Council request form.
'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const source = relative => fs.readFileSync(path.join(root, relative), 'utf8');
test('Council form owns submission, notifier and DevExpress validation', () => {
  const view = source('src/PublisherStudio.Web/Components/OrganicPlugins/OrganicCouncilRequestForm.razor');
  const code = source('src/PublisherStudio.Web/Components/OrganicPlugins/OrganicCouncilRequestForm.razor.cs');
  const parent = source('src/PublisherStudio.Web/Components/Pages/OrganicPlugins.razor');
  assert.match(view, /<EditForm Model="@Draft" OnValidSubmit="SubmitAsync" Context="councilEditContext"/);
  assert.match(view, /<DataAnnotationsValidator \/>/);
  assert.match(view, /<DxComboBox/);
  assert.match(view, /<DxSpinEdit/);
  assert.match(source('src/PublisherStudio.Web/BusinessObjects/OrganicCouncilRequestFormModel.cs'), /IValidatableObject/);
  assert.match(code, /Connection\.SendCouncilRequestAsync\(Request\)/);
  assert.match(code, /Notifications\.Error/);
  assert.match(parent, /<OrganicCouncilRequestForm Request="@_councilRequest"/);
  assert.doesNotMatch(parent, /private async Task SendCouncilAsync\(/);
  assert.match(source('src/PublisherStudio.Web/Controllers/OrganicPluginController.cs'), /\[FromBody\] OrganicCouncilPromptRequest request/);
});
test('mandatory guards and audit are shipped', () => {
  assert.match(source('Directory.Build.targets'), /AssertPublisherStudioRazorFormEncapsulation/);
  assert.match(source('Build-Release.ps1'), /Assert-RazorFormEncapsulation\.ps1/);
  assert.match(source('src/PublisherStudio.Web/PublisherStudio.Web.csproj'), /<Version>4\.6\.5<\/Version>/);
  assert.match(source('FORM_AUDIT_ALL_COMPONENTS.md'), /InspectorPanel\.razor/);
});
