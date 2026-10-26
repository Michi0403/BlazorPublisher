# Build-blocking guard: the stable per-user 1-Wire identity wins over an obsolete portable copy.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$source = Get-Content -LiteralPath (Join-Path $root 'src/PublisherStudio.Web/Services/OrganicPlugins/OrganicRuntimeSecurityService.cs') -Raw
$start = $source.IndexOf('private string ResolveSecretPath()')
if ($start -lt 0) { throw 'WIREID001: missing stable identity path resolver.' }
$resolve = $source.Substring($start)
$preferred = $resolve.IndexOf('if (File.Exists(preferred))')
$portable = $resolve.IndexOf('if (File.Exists(portable))')
if ($preferred -lt 0 -or $portable -lt 0 -or $preferred -ge $portable) { throw 'WIREID002: the persistent per-user 1-Wire identity must take precedence over portable copies.' }
if ($resolve -notmatch 'refusing to replace it' -or $resolve -notmatch 'the portable file was not modified') { throw 'WIREID003: existing secrets must not be silently regenerated or overwritten.' }
Write-Host 'WIREID: persistent identity precedence and non-destructive fallback verified.'
