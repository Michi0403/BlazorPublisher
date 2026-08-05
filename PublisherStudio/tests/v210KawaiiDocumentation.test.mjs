import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';

const root = path.resolve(import.meta.dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

function expectContains(relative, values) {
  const text = read(relative);
  for (const value of values) assert.ok(text.includes(value), `${relative} must contain ${value}`);
  return text;
}

test('2.1.1 application and setup versions are aligned', () => {
  assert.match(read('src/PublisherStudio.Web/PublisherStudio.Web.csproj'), /<Version>2\.1\.1<\/Version>/);
  assert.match(read('src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'), /<Version>2\.1\.1<\/Version>/);
  assert.equal(JSON.parse(read('src/PublisherStudio.Web/package.json')).version, '2.1.1');
  assert.equal(JSON.parse(read('src/PublisherStudio.Web/package-lock.json')).version, '2.1.1');
});

test('compiler XML documentation is generated and guarded', () => {
  for (const project of [
    'src/PublisherStudio.Web/PublisherStudio.Web.csproj',
    'src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'
  ]) {
    expectContains(project, ['<GenerateDocumentationFile>true</GenerateDocumentationFile>', '<NoWarn>$(NoWarn);1591</NoWarn>']);
  }
  expectContains('Directory.Build.targets', [
    'AssertPublisherXmlDocumentationCoverage',
    'BuildPublisherStudioDocumentation',
    'IncludePublisherStudioDocumentationInPublish',
    'PublisherStudio.Web.xml',
    'PublisherStudio-$(Version).pdf'
  ]);
  assert.ok(fs.existsSync(path.join(root, 'build/Assert-XmlDocumentationCoverage.py')));
  assert.ok(fs.existsSync(path.join(root, 'build/Assert-XmlDocumentationCoverage.ps1')));
});

test('documentation runtime stays service-owned and data stays in BusinessObjects', () => {
  const service = expectContains('src/PublisherStudio.Web/Services/Documentation/PublisherDocumentationCatalogService.cs', [
    'IPublisherDocumentationCatalogService',
    'PublisherDocumentationManifest',
    'ILogger<PublisherDocumentationCatalogService>'
  ]);
  assert.doesNotMatch(service, /\bstatic\b/);
  assert.doesNotMatch(service, /GeneratedRegex/);
  expectContains('src/PublisherStudio.Web/BusinessObjects/DocumentationModels.cs', [
    'namespace PublisherStudio.BusinessObjects;',
    'PublisherDocumentationStatus',
    'PublisherDocumentationComment',
    'PublisherDocumentationManifest'
  ]);
});

test('PublisherStudio offers an in-app help hub and ribbon actions', () => {
  expectContains('src/PublisherStudio.Web/Components/Pages/Help.razor', [
    '@page "/help"',
    '@rendermode InteractiveServer',
    '/help-docs/index.html',
    '@Status.PdfUrl',
    '/help-docs/api/index.html'
  ]);
  expectContains('src/PublisherStudio.Web/Controllers/DocumentationController.cs', [
    '[HttpGet("status")]',
    '[HttpGet("comments")]',
    '[HttpGet("pdf")]'
  ]);
  expectContains('src/PublisherStudio.Web/Components/Editor/PublicationRibbon.razor', [
    '<DxRibbonTab Text="Help">',
    'OpenDocumentation',
    'OpenHtmlDocumentation',
    'OpenPdfDocumentation',
    'OpenApiDocumentation'
  ]);
});

test('DocFX website and PDF share the Kawaii design contract', () => {
  expectContains('docs/docfx.json', [
    'PublisherStudio.Web.dll',
    'templates/publisherstudio',
    'pdf/toc.yml',
    'PublisherStudio Documentation'
  ]);
  const css = expectContains('docs/templates/publisherstudio/public/main.css', [
    'publisherstudio-kawaii-docs',
    'publisherstudio-theme-control',
    'publisherstudio-brand-paw',
    'overflow-x: clip',
    'data-bs-theme="dark"'
  ]);
  assert.doesNotMatch(css, /width:\s*100vw/);
  expectContains('docs/templates/publisherstudio/public/main.js', [
    'publisherstudio-docs-theme',
    'hideBuiltInThemePickers',
    'cycleTheme',
    'publisherstudio-brand-paw',
    'applyTheme'
  ]);
  expectContains('docs/pdf-cover.html', ['PublisherStudio', 'Kawaii']);
});

test('GitHub Pages deploys the exact shipped documentation tree', () => {
  expectContains('.github/workflows/publish-shipped-docs.yml', [
    'actions/checkout@v6',
    'actions/upload-pages-artifact@v4',
    'actions/deploy-pages@v4',
    'extract-shipped-docs.py',
    "--pattern '*.zip'"
  ]);
  expectContains('.github/scripts/extract-shipped-docs.py', [
    'build_member_index',
    'zipfile.ZipInfo',
    'publisherstudio-kawaii.css',
    'publisherstudio-kawaii.js',
    'documentation-status.json',
    'projectRelativeAssetsVerified'
  ]);
});

test('the documentation milestone publish warnings remain repaired', () => {
  const wordArt = read('src/PublisherStudio.Web/Components/Editor/SvgWordArtText.cs');
  assert.doesNotMatch(wordArt, /AddAttribute\(builder,\s*sequence/);
  assert.match(wordArt, /builder\.AddAttribute\(1, "class"/);
  expectContains('src/PublisherStudio.Web/Services/Streaming/Capture/WindowsProcessLoopbackCapture.cs', [
    'if (!OperatingSystem.IsWindows())',
    '[SupportedOSPlatform("windows")]'
  ]);
  assert.doesNotMatch(read('src/PublisherStudio.Web/Diagnostics/ControllerRequestLoggingFilter.cs'), /catch \(OperationCanceledException exception\)/);
  assert.doesNotMatch(read('src/PublisherStudio.Web/Controllers/OrganicWireHttpController.cs'), /catch \(OperationCanceledException exception\)/);
});

test('public documentation text is concise and contains no personal work diary', () => {
  const publicMarkdown = fs.readdirSync(path.join(root, 'docs/articles'))
    .filter(name => name.endsWith('.md'))
    .map(name => read(`docs/articles/${name}`))
    .join('\n');
  assert.doesNotMatch(publicMarkdown, /\bMichi\b|Michael Fleischer|I (?:implemented|fixed|changed)|we (?:implemented|fixed|changed)/i);
  expectContains('README.md', ['A gentle first path', 'Documentation', 'Architecture']);
});
