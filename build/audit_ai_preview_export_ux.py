#!/usr/bin/env python3
"""Static source audit for PublisherStudio 2.6.5 preview/AI/export UX compatibility boundary."""
from pathlib import Path
import sys
root=Path(__file__).resolve().parents[1]

def text(rel):
    p=root/rel
    if not p.is_file(): raise AssertionError(f"missing {rel}")
    return p.read_text(encoding='utf-8')

def require(rel,*needles):
    value=text(rel)
    missing=[n for n in needles if n not in value]
    if missing: raise AssertionError(f"{rel} missing: {', '.join(missing)}")

try:
    require('src/PublisherStudio.Web/Services/Configuration/PanelStudioPreviewPresetService.cs',
            'IPanelStudioPreviewPresetService', 'PanelStudioPreviewPresetService', 'Width = 390', 'Width = 1280', 'variables.Set')
    require('src/PublisherStudio.Web/Services/Configuration/ISystemVariableStoreService.cs',
            'PanelStudioPreviewPresetsVariableName')
    require('src/PublisherStudio.Web/appsettings.json',
            '"PanelStudio.PreviewViewportPresets": "[]"', '"PublicationFormatVersion": "1.58"')
    require('src/PublisherStudio.Web/Components/Editor/PanelStudio.razor',
            '_previewPresets', '+ Preset', 'Save preset', 'panel-studio-inspector-components', 'panel-studio-footer-actions')
    require('src/PublisherStudio.Web/wwwroot/js/tooltipRuntime.js',
            "document.addEventListener('scroll'", 'hide(true)')
    require('src/PublisherStudio.Web/Components/Pages/Editor.razor',
            'title-bar-language', '_singleWebsiteExportVisible', 'AI Text', 'AI Chat', 'InsertAiChatQuick', 'OpenAiOnShow')
    require('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js',
            'optimizeSingleFileMedia', 'structuredEncodeImage', 'structuredTranscodeVideo', 'exportWebsite(fileName, title, options = {})')
    require('src/PublisherStudio.Web/BusinessObjects/PublicationComponentModels.cs',
            'PublicationChatAiMode', 'LocalGptCouncil', 'ChatAiTeamKey', 'ChatAiSystemPrompt', 'ChatAiMaxOutputTokens')
    require('src/PublisherStudio.Web/Services/OrganicPlugins/PublisherAiBridgeService.cs',
            'IPublisherAiBridgeService', 'council.run', 'SendCouncilRequestAsync', 'WaitForResultAsync')
    require('src/PublisherStudio.Web/Controllers/PublisherAiController.cs',
            '[Route("api/publisher-ai")]', '[HttpPost("chat")]', 'IPublisherAiBridgeService')
    require('src/PublisherStudio.Web/Components/Editor/DevExtremeComponentEditor.razor',
            'AI assistant', 'LocalGptCouncil', 'Council team', 'same-origin AI bridge')
    require('src/PublisherStudio.Web/wwwroot/js/componentRuntime.js',
            'chatUsesLocalGptAi', 'publishLocalGptAiMessage', 'PublisherStudioAiEndpoint', '/api/publisher-ai/chat')
    require('src/PublisherStudio.Web/Components/Editor/StoryEditor.razor',
            'Edit selection', 'AiProofreadSelection', 'AiTranslateSelection', 'GetTextSpanAsync', 'ReplaceSelectionWithAiProposalAsync', '_aiStoryPrompt = $"{instruction}\\n\\nSelected story text:')
    require('src/PublisherStudio.Web/BusinessObjects/PublicationModels.cs', 'FormatVersion { get; set; } = "1.58"')
    require('src/PublisherStudio.Web/Components/Layout/MainLayout.razor', '@inject NavigationManager Navigation', '@key="Navigation.Uri"')
    require('src/PublisherStudio.Web/PublisherStudio.Web.csproj','<Version>2.7.6</Version>')
    require('src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj','<Version>2.7.6</Version>')
    print('PublisherStudio 2.6.5 preview/AI/export UX compatibility source audit passed: custom preview presets, inspector UX, embedded language selection, compressed single HTML exports, StoryEditor AI editing, and LocalGPT-backed DevExtreme Chat are wired.')
except AssertionError as exc:
    print(f'PublisherStudio 2.6.5 preview/AI/export UX compatibility source audit failed: {exc}', file=sys.stderr)
    sys.exit(1)
