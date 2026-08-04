import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

test('2.0.5 keeps SpreadsheetDocumentService constructor-owned and DI-resolvable', () => {
  const source = read('src/PublisherStudio.Web/Services/SpreadsheetDocumentService.cs');
  assert.match(source, /public sealed class SpreadsheetDocumentService\s*\{/);
  assert.match(source, /public SpreadsheetDocumentService\(IPublicationMarkupService markup\)/);
  assert.doesNotMatch(source, /public sealed class SpreadsheetDocumentService\s*\([^)]/);
  assert.equal((source.match(/public SpreadsheetDocumentService\s*\(/g) ?? []).length, 1);
  assert.match(source, /_markup = markup \?\? throw new ArgumentNullException\(nameof\(markup\)\)/);
});

test('2.0.5 release script delimits variables before colon for Windows PowerShell 5.1', () => {
  const release = read('Build-Release.ps1');
  assert.match(release, /Release archive is missing \$\{expectedManifest\}: \$ArchivePath/);
  assert.doesNotMatch(release, /Release archive is missing \$expectedManifest: \$ArchivePath/);
});

test('2.0.5 logging guard classifies deterministic instance services without statics', () => {
  const helpers = [
    'src/PublisherStudio.Web/Services/ConnectorGeometry.cs',
    'src/PublisherStudio.Web/Services/PublicationAnimationData.cs',
    'src/PublisherStudio.Web/Services/PublicationMediaData.cs',
    'src/PublisherStudio.Web/Services/RichTextDocumentFactory.cs',
    'src/PublisherStudio.Web/Services/Streaming/UseCases/Chat/StreamingChatResultFactory.cs',
    'src/PublisherStudio.Web/Services/WordArtPathGeometry.cs'
  ];
  for (const helper of helpers) {
    const source = read(helper);
    assert.match(source, /logging-policy:\s*pure-helper/);
    assert.match(source, /public sealed class/);
    assert.doesNotMatch(source, /public\s+static\s+class/);
  }
});

test('2.0.5 active version surfaces are aligned', () => {
  assert.match(read('src/PublisherStudio.Web/PublisherStudio.Web.csproj'), /<Version>2\.0\.5<\/Version>/);
  assert.match(read('src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'), /<Version>2\.0\.5<\/Version>/);
  assert.equal(JSON.parse(read('src/PublisherStudio.Web/package.json')).version, '2.0.5');
  assert.equal(JSON.parse(read('src/PublisherStudio.Web/package-lock.json')).version, '2.0.5');
});
