'use strict';
const assert = require('node:assert/strict');
const test = require('node:test');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const source = relative => fs.readFileSync(path.join(root, relative), 'utf8');
const component = name => source(`src/PublisherStudio.Web/Components/${name}`);

test('AI ribbon and object entry controls need a live approved link, not a capability', () => {
    const ribbon = component('Editor/PublicationRibbon.razor');
    const editor = component('Pages/Editor.razor');
    const surface = component('Editor/PageSurface.razor');
    assert.match(ribbon, /return LocalGptConnection\.State\.IsConnected && LocalGptConnection\.State\.IsLinked;/);
    assert.match(ribbon, /DxRibbonTab Text='@LT\("LocalGPT AI"\)'/);
    assert.match(editor, /@onclick="AddAiTextQuick"/);
    assert.match(editor, /AiAvailable="ShowLocalGptAi"/);
    assert.doesNotMatch(editor, /if \(!CanUseAiCouncil\) return;\s*switch \(kind\)/);
    assert.match(surface, /@if \(State\.SelectedElement is not null && LocalGptConnection\.State\.IsConnected && LocalGptConnection\.State\.IsLinked\)/);
    assert.doesNotMatch(surface, /if \(!LocalGptConnection\.State\.HasCapability\("council\.run"\)\) return;/);
});

test('Story, spreadsheet and media assistance stay accessible but sending retains capability guard', () => {
    const story = component('Editor/StoryEditor.razor');
    const sheet = component('Editor/SpreadsheetEditor.razor');
    const editor = component('Pages/Editor.razor');
    assert.match(story, /@if \(ShowAiCouncilActions\)/);
    assert.match(story, /_aiStoryPanelVisible = OpenAiOnShow;/);
    assert.match(story, /if \(!CanUseAiCouncil\)/);
    assert.match(sheet, /@if \(OpenAiOnShow\)/);
    assert.match(sheet, /if \(!LocalGptConnection\.State\.HasCapability\("council\.run"\)\)/);
    assert.match(editor, /_mediaAiBusy \|\| !_mediaAiVisible \|\| !CanUseAiCouncil/);
    assert.match(source('src/PublisherStudio.Web/Services/OrganicPlugins/LocalGptConnectionService.cs'), /CapabilityKey = "council\.run"/);
});

test('Authenticated capability response updates current session without relying on UDP discovery', () => {
    const connection = source('src/PublisherStudio.Web/Services/OrganicPlugins/LocalGptConnectionService.cs');
    const start = connection.indexOf('case OrganicWireMessageType.SkillStateUpdate:');
    const end = connection.indexOf('case OrganicWireMessageType.Invoke:', start);
    assert.ok(start > 0 && end > start);
    const handler = connection.slice(start, end);
    assert.match(handler, /if \(connectionId != connectedId \|\| !State\.IsConnected\)/);
    assert.match(handler, /State\.RemoteCapabilities = remoteCapabilities \?\? \[\];/);
    assert.match(handler, /var currentPeer = discovery\.GetPeer\(connectedPeerId\);/);
    assert.match(handler, /if \(currentPeer is not null\)/);
    assert.ok(handler.indexOf('State.RemoteCapabilities =') < handler.indexOf('if (currentPeer is not null)'), 'authoritative session should be updated even with no registry entry');
});

test('Public ticket is renewed on security panel open without automatic MFA seed exposure', () => {
    const security = component('OrganicPlugins/OrganicSecurityPanel.razor');
    const start = security.indexOf('protected override async Task OnInitializedAsync()');
    const end = security.indexOf('protected override async Task OnAfterRenderAsync(', start);
    const initialize = security.slice(start, end);
    assert.match(initialize, /Security\.CreatePairingTicketAsync\(TimeSpan\.FromMinutes\(15\)\)/);
    assert.doesNotMatch(initialize, /GetOtpAuthUriAsync/);
    assert.match(security, /if \(!string\.IsNullOrWhiteSpace\(_otpUri\)\)/);
    assert.match(security, /if \(!string\.IsNullOrWhiteSpace\(_pairingTicketJson\)\)/);
});
