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

test('2.1.7 application and setup versions are aligned', () => {
  assert.match(read('src/PublisherStudio.Web/PublisherStudio.Web.csproj'), /<Version>2\.1\.7<\/Version>/);
  assert.match(read('src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'), /<Version>2\.1\.7<\/Version>/);
  assert.equal(JSON.parse(read('src/PublisherStudio.Web/package.json')).version, '2.1.7');
  assert.equal(JSON.parse(read('src/PublisherStudio.Web/package-lock.json')).version, '2.1.7');
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
    'publisherstudio-cursor-paw',
    'overflow-x: clip',
    'data-bs-theme="dark"'
  ]);
  assert.doesNotMatch(css, /width:\s*100vw/);
  expectContains('docs/templates/publisherstudio/public/main.js', [
    'publisherstudio-docs-theme',
    'mountThemeControl',
    'persistTheme',
    'publisherstudio-cursor-paw',
    'applyTheme'
  ]);
  expectContains('docs/pdf-cover.html', ['PublisherStudio', 'Kawaii']);
});

test('GitHub Pages uses the same pinned snapshot workflow as LocalGPT', () => {
  expectContains('.github/workflows/publish-shipped-docs.yml', [
    'actions/checkout@v6',
    'actions/configure-pages@v5',
    'actions/upload-pages-artifact@v4',
    'actions/deploy-pages@v4',
    'prepare-pages-artifact.py',
    '.github/pages/publisherstudio-kawaii-docs.zip'
  ]);
  assert.equal(fs.existsSync(path.join(root, '.github/scripts/extract-shipped-docs.py')), false);
  expectContains('.github/scripts/prepare-pages-artifact.py', [
    'publisherstudio-kawaii.css',
    'publisherstudio-kawaii.js',
    'documentation-status.json',
    'favicon.svg',
    'logo.svg',
    'data-publisherstudio-theme-bootstrap'
  ]);
  assert.ok(fs.existsSync(path.join(root, '.github/pages/publisherstudio-kawaii-docs.zip')));
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
