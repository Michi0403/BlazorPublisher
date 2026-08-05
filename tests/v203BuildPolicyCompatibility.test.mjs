import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';

const root = path.resolve(import.meta.dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

const architecture = read('build/Invoke-ArchitectureAudit.ps1');
const iteratorGuard = read('build/Assert-IteratorExceptionPolicy.ps1');
const iteratorBaseline = read('build/iterator-exception-baseline.json');
const traversal = read('src/PublisherStudio.Web/Services/PublicationElementTraversal.cs');
const ffmpeg = read('src/PublisherStudio.Web/Services/Streaming/Encoding/FfmpegLocator.cs');
const publishGuard = read('build/Assert-PublishConfiguration.ps1');
const installerGuard = read('build/Assert-InstallerWorkflow.ps1');

test('Python architecture output is visible but cannot become the exit-code value', () => {
  assert.match(architecture, /\$auditOutput\s*=\s*@\(&\s*\$python\.Source[\s\S]*?2>&1\)/);
  assert.match(architecture, /\$auditExitCode\s*=\s*\[int\]\$LASTEXITCODE/);
  assert.match(architecture, /foreach \(\$line in \$auditOutput\) \{ Write-Host \(\[string\]\$line\) \}/);
  assert.match(architecture, /return \$auditExitCode/);
  assert.doesNotMatch(architecture, /& \$python\.Source[^\n]+\n\s*return \$LASTEXITCODE/);
});

test('iterator inspection ignores type declarations and repaired methods do not yield', () => {
  assert.match(iteratorGuard, /\?:class\|struct\|record\|interface\|enum/);
  assert.doesNotMatch(iteratorBaseline, /PublicationElementTraversal|FfmpegLocator|VideoProjectImportService/);
  assert.doesNotMatch(traversal, /\byield\s+(?:return|break)\b/);
  assert.match(traversal, /var descendants = new List<PublicationElement>\(\);/);
  assert.match(traversal, /logger\.LogError\(exception, "Could not collect descendant publication elements/);

  const method = ffmpeg.match(/private IEnumerable<string> FindWinGetPackageExecutables[\s\S]*?\n    }\n\n    private bool TryResolveCommand/)?.[0] ?? '';
  assert.ok(method.length > 0, 'FindWinGetPackageExecutables method was not found.');
  assert.doesNotMatch(method, /\byield\s+(?:return|break)\b/);
  assert.match(method, /var matches = new List<string>\(\);/);
  assert.match(method, /logger\.LogError\(exception, "Could not collect WinGet FFmpeg package executables\."\)/);
});

test('all application and setup profiles explicitly own their publish contract', () => {
  const profileSets = [
    ['src/PublisherStudio.Web/Properties/PublishProfiles', 'AnyCPU', 'false'],
    ['src/PublisherStudio.InstallerConsole/Properties/PublishProfiles', 'Any CPU', 'true'],
  ];
  for (const [directory, platform, singleFile] of profileSets) {
    for (const name of fs.readdirSync(path.join(root, directory)).filter(value => value.endsWith('.pubxml'))) {
      const profile = read(`${directory}/${name}`);
      for (const marker of [
        '<Configuration>Release</Configuration>',
        `<Platform>${platform}</Platform>`,
        '<PublishProtocol>FileSystem</PublishProtocol>',
        `<PublishSingleFile>${singleFile}</PublishSingleFile>`,
      ]) assert.ok(profile.includes(marker), `${directory}/${name} is missing ${marker}`);
      assert.match(profile, /<PublishDir>\.\.\\\.\.\\artifacts\\release\\[^<]+\\<\/PublishDir>/);
    }
  }
  assert.match(publishGuard, /must define PublishDir or PublishUrl so release scripts can consume profile-owned output/);
});

test('installer launch profiles are enumerated by exact name without fragile property Count access', () => {
  assert.match(installerGuard, /\$launchProfileNames\s*=\s*@\(\$launchSettings\.profiles\.PSObject\.Properties \| ForEach-Object \{ \$_\.Name \}\)/);
  assert.match(installerGuard, /foreach \(\$profileName in \$requiredLaunchProfiles\)/);
  assert.doesNotMatch(installerGuard, /profiles\.PSObject\.Properties\.Count/);
  for (const profile of [
    'PublisherStudio Install',
    'PublisherStudio Update',
    'PublisherStudio Start',
  ]) assert.ok(installerGuard.includes(`'${profile}'`), profile);
  assert.match(installerGuard, /Visual Studio launch profiles must expose only Install, Update, and Start/);
});
