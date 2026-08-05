import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';

const root = path.resolve(import.meta.dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

test('development first-chance diagnostics are service-owned and bounded', () => {
  const service = read('src/PublisherStudio.Web/Diagnostics/DebugFirstChanceExceptionLoggingHostedService.cs');
  const options = read('src/PublisherStudio.Web/BusinessObjects/Diagnostics/DebugExceptionDiagnosticsOptions.cs');
  const registration = read('src/PublisherStudio.Web/PublisherStudioServiceCollectionExtensions.cs');
  const settings = JSON.parse(read('src/PublisherStudio.Web/appsettings.json'));

  assert.match(service, /sealed class DebugFirstChanceExceptionLoggingHostedService/);
  assert.match(service, /IHostedService, IDisposable/);
  assert.match(service, /AppDomain\.CurrentDomain\.FirstChanceException \+= HandleFirstChanceException/);
  assert.match(service, /AppDomain\.CurrentDomain\.FirstChanceException -= HandleFirstChanceException/);
  assert.match(service, /ConcurrentDictionary<string, ExceptionOccurrence>/);
  assert.match(service, /DetailedOccurrencesPerCallSite/);
  assert.match(service, /SummaryEveryOccurrences/);
  assert.match(service, /LogLevel\.Debug/);
  assert.match(service, /LogLevel\.Warning/);
  assert.doesNotMatch(service, /static\s+(?:readonly\s+)?(?:ConcurrentDictionary|Dictionary|HashSet|ILogger)/);

  assert.match(options, /namespace PublisherStudio\.BusinessObjects\.Diagnostics/);
  assert.match(registration, /Configure<DebugExceptionDiagnosticsOptions>/);
  assert.match(registration, /AddHostedService<DebugFirstChanceExceptionLoggingHostedService>/);
  assert.equal(settings.PublisherStudio.DebugExceptionDiagnostics.Enabled, true);
  assert.equal(settings.PublisherStudio.DebugExceptionDiagnostics.DetailedOccurrencesPerCallSite, 3);
  assert.equal(settings.PublisherStudio.DebugExceptionDiagnostics.SummaryEveryOccurrences, 25);
});

test('host termination and endpoint cleanup failures have explicit logging', () => {
  const program = read('src/PublisherStudio.Web/Program.cs');
  assert.match(program, /hostLogger\.LogDebug\(exception, "PublisherStudio host shutdown was canceled/);
  assert.match(program, /hostLogger\.LogCritical\(exception, "PublisherStudio host terminated unexpectedly/);
  assert.match(program, /hostLogger\.LogError\(exception, "PublisherStudio could not remove its owned runtime endpoint/);
});

test('2.1.9 remains independent from the LocalGPT wire protocol version', () => {
  assert.match(read('src/PublisherStudio.Web/PublisherStudio.Web.csproj'), /<Version>2\.1\.9<\/Version>/);
  assert.match(read('src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'), /<Version>2\.1\.9<\/Version>/);
  assert.equal(JSON.parse(read('src/PublisherStudio.Web/package.json')).version, '2.1.9');
  assert.match(read('Directory.Build.props'), /<LocalGptWireProtocolVersion>2\.1\.1<\/LocalGptWireProtocolVersion>/);
});
