'use strict';
const assert = require('node:assert/strict');
const test = require('node:test');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const source = relative => fs.readFileSync(path.join(root, relative), 'utf8');
const component = name => source(`src/PublisherStudio.Web/Components/${name}`);

test('PublisherStudio LocalGPT AI visibility requires live reciprocal link; no capability gate', () => {
    const ribbon = component('Editor/PublicationRibbon.razor');
    const story = component('Editor/StoryEditor.razor');
    const editor = component('Pages/Editor.razor');
    const pageSurface = component('Editor/PageSurface.razor');
    assert.match(ribbon, /return LocalGptConnection\.State\.IsConnected && LocalGptConnection\.State\.IsLinked;/);
    assert.match(story, /private bool ShowAiCouncilActions => LocalGptConnection\.State\.IsConnected && LocalGptConnection\.State\.IsLinked;/);
    assert.match(editor, /private bool ShowLocalGptAi => LocalGptConnection\.State\.IsConnected && LocalGptConnection\.State\.IsLinked;/);
    assert.match(editor, /@if \(ShowLocalGptAi\)\s*\{\s*<button[^>]+@onclick="AddAiTextQuick"/);
    assert.match(editor, /AiAvailable="ShowLocalGptAi"/);
    assert.match(pageSurface, /@if \(State\.SelectedElement is not null && LocalGptConnection\.State\.IsConnected && LocalGptConnection\.State\.IsLinked\)/);
    assert.doesNotMatch(ribbon, /return LocalGptConnection\.State\.HasCapability\("council\.run"\);/);
    assert.doesNotMatch(story, /private bool ShowAiCouncilActions => true;/);
});

test('Already queued LocalGPT actions do not open after link loss, but capability protects send', () => {
    const editor = component('Pages/Editor.razor');
    const surface = component('Editor/PageSurface.razor');
    const story = component('Editor/StoryEditor.razor');
    assert.match(editor, /private async Task OpenSelectedAiAssistAsync\(string kind\)[\s\S]*?if \(!ShowLocalGptAi\) return;/);
    assert.match(editor, /private Task OpenMediaAiAsync\(string kind\)[\s\S]*?if \(!ShowLocalGptAi\) return Task\.CompletedTask;/);
    assert.match(editor, /private async Task AddAiTextQuick\(\)[\s\S]*?if \(!ShowLocalGptAi\) return;/);
    assert.match(surface, /if \(!LocalGptConnection\.State\.IsConnected \|\| !LocalGptConnection\.State\.IsLinked\) return;/);
    assert.match(editor, /return LocalGptConnection\.State\.IsLinked && LocalGptConnection\.State\.HasCapability\("council\.run"\);/);
    assert.match(story, /return LocalGptConnection\.State\.IsLinked && LocalGptConnection\.State\.HasCapability\("council\.run"\);/);
});

test('Connection settings and optional direct AI Chat remain accessible independently', () => {
    const ribbon = component('Editor/PublicationRibbon.razor');
    const editor = component('Pages/Editor.razor');
    assert.match(ribbon, /@if \(CanShowAiCouncil \|\| DirectAi\.IsAvailable\(\)\)/);
    assert.match(ribbon, /DxRibbonItem Text='@LT\("AI Chat"\)'/);
    assert.match(editor, /@if \(ShowLocalGptAi \|\| DirectAi\.IsAvailable\(\)\)/);
    assert.match(editor, /@onclick="InsertAiChatQuick"/);
    assert.match(editor, /if \(!ShowLocalGptAi && !DirectAi\.IsAvailable\(\)\) return;/);
    assert.match(editor, /DirectAi\.IsAvailable\(\)/);
    assert.match(source('src/PublisherStudio.Web/Services/OrganicPlugins/LocalGptConnectionService.cs'), /CapabilityKey = "council\.run"/);
});
